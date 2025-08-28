using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Route.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;
using Yarp.ReverseProxy.Configuration;

namespace Netcorext.Auth.Authentication.Workers;

internal class RouteRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly IConfiguration _configuration;
    private readonly InMemoryConfigProvider _memoryConfigProvider;
    private readonly ConfigSettings _config;
    private readonly ILogger<RouteRunner> _logger;

    public RouteRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, ISerializer serializer, KeyLocker locker, IProxyConfigProvider proxyConfigProvider, IOptions<ConfigSettings> config, IConfiguration configuration, ILogger<RouteRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _cacheEntryOptions = cacheEntryOptions;
        _serializer = serializer;
        _locker = locker;
        _configuration = configuration;
        _memoryConfigProvider = (InMemoryConfigProvider)proxyConfigProvider;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(RouteRunner));

        await UpdateRouteAsync(null, cancellationToken);

        _subscriber?.Dispose();
        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_ROUTE_CHANGE_EVENT], Handler);

        return;

        void Handler(string s, object o)
        {
            _ = UpdateRouteAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateRouteAsync(string? ids, CancellationToken cancellationToken = default)
    {
        var lockerKey = ids.IsEmpty() ? nameof(UpdateRouteAsync) : nameof(UpdateRouteAsync) + "/" + ids;

        try
        {
            await _locker.WaitAsync(lockerKey);

            _logger.LogInformation(nameof(UpdateRouteAsync));

            var gatewayConfig = _configuration.GetSection("ReverseProxy");

            if (gatewayConfig.Exists())
            {
                _cache.Set(ConfigSettings.CACHE_ROUTE_CHECK_KEY, 9999);

                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids.IsEmpty() ? null : _serializer.Deserialize<long[]>(ids);

            var cacheRouteGroups = _cache.Get<Dictionary<long, Services.Route.Queries.Models.RouteGroup>>(ConfigSettings.CACHE_ROUTE) ?? new Dictionary<long, Services.Route.Queries.Models.RouteGroup>();

            if (reqIds.IsEmpty())
                cacheRouteGroups.Clear();
            else
                reqIds.ForEach(t => cacheRouteGroups.Remove(t));

            var result = await dispatcher.SendAsync(new GetRoute
                                                    {
                                                        GroupIds = reqIds.IsEmpty() ? null : reqIds
                                                    }, cancellationToken);

            if (result.Code == Result.Success && !result.Content.IsEmpty())
            {
                result.Content.ForEach(t =>
                                       {
                                           if (cacheRouteGroups.TryAdd(t.Id, t))
                                               return;

                                           cacheRouteGroups[t.Id] = t;
                                       });
            }

            var clusters = cacheRouteGroups.Values
                                           .Select(t => new ClusterConfig
                                                        {
                                                            ClusterId = $"{t.Id}-{t.Name}",
                                                            HttpRequest = t.ForwarderRequestConfig,
                                                            Destinations = new Dictionary<string, DestinationConfig>
                                                                           {
                                                                               {
                                                                                   $"{t.Name}-{t.BaseUrl}",
                                                                                   new DestinationConfig
                                                                                   {
                                                                                       Address = t.BaseUrl
                                                                                   }
                                                                               }
                                                                           },
                                                            HealthCheck = new HealthCheckConfig
                                                                          {
                                                                              Active = new ActiveHealthCheckConfig
                                                                                       {
                                                                                           Enabled = true,
                                                                                           Interval = TimeSpan.FromMilliseconds(_config.AppSettings.HealthCheckInterval),
                                                                                           Timeout = TimeSpan.FromMilliseconds(_config.AppSettings.HealthCheckTimeout),
                                                                                           Path = _config.AppSettings.HealthCheckPath
                                                                                       },
                                                                              Passive = new PassiveHealthCheckConfig
                                                                                        {
                                                                                            Enabled = true,
                                                                                            ReactivationPeriod = TimeSpan.FromSeconds(_config.AppSettings.HealthCheckReactivationPeriod)
                                                                                        }
                                                                          }
                                                        })
                                           .ToArray();

            var routes = cacheRouteGroups.Values
                                         .SelectMany(t => t.Routes
                                                           .Select(t2 => new { t2.Protocol, t2.HttpMethod, t2.RelativePath })
                                                           .Distinct()
                                                           .GroupBy(t2 => new { t2.Protocol, t2.RelativePath }, t2 => t2.HttpMethod)
                                                           .Select(t2 => new RouteConfig
                                                                         {
                                                                             ClusterId = $"{t.Id}-{t.Name}",
                                                                             RouteId = $"{t2.Key.Protocol} - {t.BaseUrl}/{t2.Key.RelativePath}",
                                                                             Match = new RouteMatch
                                                                                     {
                                                                                         Methods = t2.ToArray(),
                                                                                         Path = t2.Key.RelativePath
                                                                                     }
                                                                         }))
                                         .ToArray();


            _memoryConfigProvider.Update(routes, clusters);

            _cache.Set(ConfigSettings.CACHE_ROUTE, cacheRouteGroups, _cacheEntryOptions);
            _cache.Set(ConfigSettings.CACHE_ROUTE_CHECK_KEY, cacheRouteGroups.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
        finally
        {
            _locker.Release(lockerKey);
        }
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
    }
}
