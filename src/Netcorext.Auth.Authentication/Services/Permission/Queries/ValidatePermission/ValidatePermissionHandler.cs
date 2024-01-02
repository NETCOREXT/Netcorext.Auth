using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Auth.Enums;
using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class ValidatePermissionHandler : IRequestHandler<ValidatePermission, Result>
{
    private readonly DatabaseContext _context;
    private readonly IMemoryCache _cache;
    private readonly ConfigSettings _config;

    public ValidatePermissionHandler(DatabaseContextAdapter context, IMemoryCache cache, IOptions<ConfigSettings> config)
    {
        _context = context.Slave;
        _cache = cache;
        _config = config.Value;
    }

    public async Task<Result> Handle(ValidatePermission request, CancellationToken cancellationToken = default)
    {
        var cachePermissionRule = _cache.Get<Dictionary<long, Models.PermissionRule>>(ConfigSettings.CACHE_PERMISSION_RULE) ?? new Dictionary<long, Models.PermissionRule>();
        var cacheRolePermission = _cache.Get<Dictionary<string, Models.RolePermission>>(ConfigSettings.CACHE_ROLE_PERMISSION) ?? new Dictionary<string, Models.RolePermission>();
        var cacheRolePermissionCondition = _cache.Get<Dictionary<long, Models.RolePermissionCondition>>(ConfigSettings.CACHE_ROLE_PERMISSION_CONDITION) ?? new Dictionary<long, Models.RolePermissionCondition>();
        var cacheUserPermissionCondition = _cache.Get<Dictionary<long, Models.UserPermissionCondition>>(ConfigSettings.CACHE_USER_PERMISSION_CONDITION) ?? new Dictionary<long, Models.UserPermissionCondition>();

        if (!cachePermissionRule.Any())
            return Result.Forbidden;

        var roleIds = Array.Empty<long>();

        if (request.RoleId != null && request.RoleId.Any())
            roleIds = request.RoleId;

        if (request.UserId.HasValue)
        {
            if (_config.AppSettings.Owner?.Any(t => t == request.UserId) ?? false)
                return Result.Success;

            var dsUser = _context.Set<Domain.Entities.User>();

            var user = await dsUser.Where(t => t.Id == request.UserId.Value)
                                   .Select(t => new
                                                {
                                                    t.Id,
                                                    t.Disabled,
                                                    Roles = t.Roles
                                                             .Where(t2 => t2.ExpireDate == null || t2.ExpireDate > DateTimeOffset.UtcNow)
                                                             .Select(t2 => t2.RoleId)
                                                })
                                   .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
                return Result.Forbidden;

            if (user.Disabled)
                return Result.AccountIsDisabled;

            if (!user.Roles.Any())
                return Result.Forbidden;

            roleIds = roleIds.Any()
                          ? user.Roles
                                .Join(roleIds, o => o, i => i, (o, _) => o)
                                .ToArray()
                          : user.Roles.ToArray();
        }

        if (request.RoleExtendData != null && request.RoleExtendData.Any())
        {
            Expression<Func<Domain.Entities.Role, bool>> predicateRole = p => false;

            var extendData = request.RoleExtendData.GroupBy(t => t.Key, (k, values) =>
                                                                            new
                                                                            {
                                                                                Key = k.ToUpper(),
                                                                                Values = values.Select(t => t.Value.ToUpper())
                                                                            });

            predicateRole = extendData.Aggregate(predicateRole, (current, item) => current.Or(t => t.ExtendData.Any(t2 => t2.Key == item.Key && item.Values.Contains(t2.Value))));

            var dsRole = _context.Set<Domain.Entities.Role>();

            var roleIdFilter = await dsRole.Where(predicateRole)
                                           .Select(t => t.Id)
                                           .ToArrayAsync(cancellationToken);

            roleIds = roleIds.Where(t => roleIdFilter.Contains(t)).ToArray();
        }

        roleIds = roleIds.Distinct().ToArray();

        if (!roleIds.Any()) return Result.Forbidden;

        Expression<Func<KeyValuePair<long, Models.PermissionRule>, bool>> predicatePermissionRule = t => t.Value.FunctionId == request.FunctionId.ToUpper();

        Expression<Func<KeyValuePair<string, Models.RolePermission>, bool>> predicateRolePermission = t => roleIds.Contains(t.Value.RoleId);
        Expression<Func<KeyValuePair<long, Models.RolePermissionCondition>, bool>> predicateRolePermissionCondition = t => roleIds.Contains(t.Value.RoleId);
        Expression<Func<KeyValuePair<long, Models.UserPermissionCondition>, bool>> predicateUserPermissionCondition = t => t.Value.UserId == request.UserId && (t.Value.ExpireDate == null || t.Value.ExpireDate > DateTimeOffset.UtcNow);

        predicateRolePermissionCondition = request.Group.IsEmpty()
                                               ? predicateRolePermissionCondition.And(t => t.Value.Group.IsEmpty())
                                               : predicateRolePermissionCondition.And(t => t.Value.Group.IsEmpty() || t.Value.Group == request.Group);

        predicateUserPermissionCondition = request.Group.IsEmpty()
                                               ? predicateUserPermissionCondition.And(t => t.Value.Group.IsEmpty())
                                               : predicateUserPermissionCondition.And(t => t.Value.Group.IsEmpty() || t.Value.Group == request.Group);

        var rolePermission = cacheRolePermission.Where(predicateRolePermission.Compile())
                                                .Select(t => t.Value.PermissionId);

        var roleConditions = cacheRolePermissionCondition.Where(predicateRolePermissionCondition.Compile())
                                                         .Select(t => new Models.PermissionCondition

                                                                      {
                                                                          PermissionId = t.Value.PermissionId,
                                                                          Group = t.Value.Group,
                                                                          Key = t.Value.Key,
                                                                          Value = t.Value.Value
                                                                      });

        var userConditions = cacheUserPermissionCondition.Where(predicateUserPermissionCondition.Compile())
                                                         .Select(t => new Models.PermissionCondition

                                                                      {
                                                                          PermissionId = t.Value.PermissionId,
                                                                          Group = t.Value.Group,
                                                                          Key = t.Value.Key,
                                                                          Value = t.Value.Value
                                                                      });

        var conditions = roleConditions.Union(userConditions)
                                       .Distinct()
                                       .ToArray();

        if (!request.PermissionConditions.IsEmpty())
        {
            var reqConditions = request.PermissionConditions
                                       .GroupBy(t => t.Key.ToUpper(), t => t.Value.ToUpper(), (key, values) => new
                                                                                                               {
                                                                                                                   Key = key.ToUpper(),
                                                                                                                   Values = values.Select(t => t.ToUpper())
                                                                                                               })
                                       .ToArray();

            Expression<Func<Models.PermissionCondition, bool>> predicateCondition = t => false;

            foreach (var i in reqConditions)
            {
                if (conditions.All(t => t.Key != i.Key)) continue;

                Expression<Func<Models.PermissionCondition, bool>> predicateKey = t => t.Key == i.Key && (i.Values.Contains(t.Value) || t.Value == "*");

                predicateCondition = predicateCondition.Or(predicateKey);
            }

            if (reqConditions.Any())
            {
                conditions = conditions.Where(predicateCondition.Compile()).ToArray();
            }
        }

        var permissions = request.PermissionConditions.IsEmpty()
                              ? rolePermission
                              : conditions.Select(t => t.PermissionId)
                                          .Union(rolePermission)
                                          .Distinct();

        predicatePermissionRule = predicatePermissionRule.And(t => permissions.Contains(t.Value.PermissionId));

        var rules = cachePermissionRule.Where(predicatePermissionRule.Compile())
                                       .Select(t => t.Value)
                                       .ToArray();

        if (!rules.Any())
            return Result.Forbidden;


        var validatorRules = rules.Select(t => new
                                               {
                                                   t.FunctionId,
                                                   t.Priority,
                                                   Readable = t.PermissionType.HasFlag(PermissionType.Read) ? (bool?)t.Allowed : null,
                                                   Writable = t.PermissionType.HasFlag(PermissionType.Write) ? (bool?)t.Allowed : null,
                                                   Deletable = t.PermissionType.HasFlag(PermissionType.Read) ? (bool?)t.Allowed : null
                                               })
                                  .GroupBy(t => new { t.FunctionId, t.Priority }, t => new { t.Readable, t.Writable, t.Deletable })
                                  .Select(t =>
                                          {
                                              // 先將同權重允許/不允許的權限最大化
                                              var p = t.Aggregate((c, n) =>
                                                                  {
                                                                      var readable = c.Readable.HasValue
                                                                                         ? c.Readable | (n.Readable ?? c.Readable)
                                                                                         : n.Readable;

                                                                      var writable = c.Writable.HasValue
                                                                                         ? c.Writable | (n.Writable ?? c.Writable)
                                                                                         : n.Writable;

                                                                      var deletable = c.Deletable.HasValue
                                                                                          ? c.Deletable | (n.Deletable ?? c.Deletable)
                                                                                          : n.Deletable;

                                                                      return new
                                                                             {
                                                                                 Readable = readable,
                                                                                 Writable = writable,
                                                                                 Deletable = deletable
                                                                             };
                                                                  });

                                              return new
                                                     {
                                                         t.Key.FunctionId,
                                                         t.Key.Priority,
                                                         p.Readable,
                                                         p.Writable,
                                                         p.Deletable
                                                     };
                                          })
                                  .OrderBy(t => t.FunctionId).ThenBy(t => t.Priority)
                                  .GroupBy(t => t.FunctionId,
                                           t => new { t.Readable, t.Writable, t.Deletable })
                                  .Select(t =>
                                          {
                                              // 最終以優先度高的權限為主
                                              var p = t.Aggregate((c, n) =>
                                                                  {
                                                                      var readable = n.Readable ?? c.Readable;

                                                                      var writable = n.Writable ?? c.Writable;

                                                                      var deletable = n.Deletable ?? c.Deletable;

                                                                      return new
                                                                             {
                                                                                 Readable = readable,
                                                                                 Writable = writable,
                                                                                 Deletable = deletable
                                                                             };
                                                                  });

                                              return new
                                                     {
                                                         FunctionId = t.Key,
                                                         PermissionType = PermissionType.None
                                                                        | (p.Readable.HasValue && p.Readable.Value ? PermissionType.Read : PermissionType.None)
                                                                        | (p.Writable.HasValue && p.Writable.Value ? PermissionType.Write : PermissionType.None)
                                                                        | (p.Deletable.HasValue && p.Deletable.Value ? PermissionType.Delete : PermissionType.None)
                                                     };
                                          })
                                  .ToArray();

        if (validatorRules.Any(t => (t.PermissionType & request.PermissionType) == request.PermissionType))
            return Result.Success;

        return Result.Forbidden;
    }
}
