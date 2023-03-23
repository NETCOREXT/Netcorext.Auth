using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Role.Commands;

public class CreateRole : IRequest<Result<IEnumerable<long>>>
{
    public Role[] Roles { get; set; } = null!;

    public class Role
    {
        public string Name { get; set; } = null!;
        public bool Disabled { get; set; }
        public RoleExtendData[]? ExtendData { get; set; }
        public RolePermission[]? Permissions { get; set; }
        public RolePermissionCondition[]? PermissionConditions { get; set; }
    }

    public class RoleExtendData
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class RolePermission
    {
        public long PermissionId { get; set; }
    }

    public class RolePermissionCondition
    {
        public long PermissionId { get; set; }
        public int Priority { get; set; }
        public string? Group { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public bool Allowed { get; set; }
    }
}