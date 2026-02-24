using System.Reflection;
using Netcorext.Configuration;

namespace Netcorext.Auth.Gateway.Settings;

public class ConfigSettings : Config<AppSettings>
{
    public const string CACHE_ROUTE = "Route";
    public const string CACHE_ROUTE_CHECK_KEY = "RouteCache";
    public const string QUEUES_HEALTH_CHECK_EVENT = "HealthCheckEvent";
    public const string QUEUES_ROUTE_CHANGE_EVENT = "RouteChangeEvent";

    public const int DEFAULT_WORKER_TASK_LIMIT = 5;
}

public class AppSettings
{
    public string LockPrefixKey { get; set; } = Assembly.GetEntryAssembly()!.GetName().Name!.ToLower();
    public int LockPrefixKeyExpires { get; set; } = 60 * 1000 * 10; // 10 minutes
    public int SlowConnectionLoggingThreshold { get; set; } = 100;
    public int SlowCommandLoggingThreshold { get; set; } = 100;
    public int ServiceSlowCommandLoggingThreshold { get; set; } = 150;
    public int HttpSlowCommandLoggingThreshold { get; set; } = 150;
    public int? WorkerTaskLimit { get; set; } = ConfigSettings.DEFAULT_WORKER_TASK_LIMIT;
    public int PooledConnectionLifetime { get; set; } = 5 * 60 * 1000;  // 5 minutes
    public int ConnectTimeout { get; set; } = 15 * 1000;                // 15 seconds
    public int HealthCheckInterval { get; set; } = 10 * 1000;           // 10 seconds
    public int HealthCheckTimeout { get; set; } = 5 * 1000;             // 5 seconds
    public int HealthCheckReactivationPeriod { get; set; } = 10 * 1000; // 10 seconds
    public string HealthCheckPath { get; set; } = "/Healthz";
    public int? RetryLimit { get; set; } = 3;
    public string RequestIdHeaderName { get; set; } = "X-Request-Id";
    public string[] RequestIdFromHeaderNames { get; set; } = { "X-Request-Id" };
    public bool EnableAspNetCoreLogger { get; set; }
    public Dictionary<string, int>? CheckCacheKeys { get; set; }
}
