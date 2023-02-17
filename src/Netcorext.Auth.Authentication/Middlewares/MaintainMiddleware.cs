using System.Security.Claims;
using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Extensions;
using Netcorext.Auth.Authentication.Services.Maintenance.Commands;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Middlewares;

internal class MaintainMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RedisClient _redis;
    private readonly ConfigSettings _config;

    public MaintainMiddleware(RequestDelegate next, IMemoryCache cache, RedisClient redis, IOptions<ConfigSettings> config)
    {
        _next = next;
        _cache = cache;
        _redis = redis;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context, IDispatcher dispatcher)
    {
        if (!_cache.TryGetValue<Maintain>(ConfigSettings.CACHE_MAINTAIN_KEY, out var maintain))
        {
            var key = _config.Caches[ConfigSettings.CACHE_MAINTAIN_KEY].Key;

            maintain = await _redis.GetAsync<Maintain>(key) ?? new Maintain();
        }

        if (!maintain.Enabled)
        {
            await _next(context);

            return;
        }

        if (_config.AppSettings.InternalHost?.Any(t => t.Equals(context.Request.Host.Host, StringComparison.CurrentCultureIgnoreCase)) ?? false)
        {
            await _next(context);

            return;
        }

        var claimName = context.User.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Name)?.Value;

        if (long.TryParse(claimName, out var id) && (_config.AppSettings.Owner?.Any(t => t == id) ?? false))
        {
            await _next(context);

            return;
        }

        var role = context.User.Claims.FirstOrDefault(t => t.Type == ClaimTypes.Role)?.Value;

        var roles = (role?.Split() ?? Array.Empty<string>()).Select(long.Parse);

        if (maintain.ExcludeRoles?.Any() == true && roles.Any(t => maintain.ExcludeRoles.Contains(t)))
        {
            await _next(context);

            return;
        }

        await context.ServiceUnavailableAsync(_config.AppSettings.UseNativeStatus, message: maintain.Message);
    }
}