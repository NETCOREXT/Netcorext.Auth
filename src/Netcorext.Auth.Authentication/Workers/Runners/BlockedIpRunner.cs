using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Blocked.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Extensions.Linq;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class BlockedIpRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly ConfigSettings _config;
    private readonly ILogger<BlockedIpRunner> _logger;
    private IDisposable? _subscriber;

    public BlockedIpRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, ISerializer serializer, KeyLocker locker, IOptions<ConfigSettings> config, ILogger<BlockedIpRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _redis = redis;
        _cache = cache;
        _cacheEntryOptions = cacheEntryOptions;
        _serializer = serializer;
        _locker = locker;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(BlockedIpRunner));

        await UpdateBlockedIpAsync(null, cancellationToken);

        _subscriber?.Dispose();
        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_BLOCKED_IP_CHANGE_EVENT], Handler);

        return;

        void Handler(string s, object o)
        {
            _ = UpdateBlockedIpAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateBlockedIpAsync(string? ids, CancellationToken cancellationToken = default)
    {
        var lockerKey = ids.IsEmpty() ? nameof(UpdateBlockedIpAsync) : nameof(UpdateBlockedIpAsync) + "/" + ids;

        try
        {
            await _locker.WaitAsync(lockerKey);

            _logger.LogInformation(nameof(UpdateBlockedIpAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            var reqIds = ids.IsEmpty() ? null : _serializer.Deserialize<long[]>(ids);

            var cacheBlockedIp = _cache.Get<Dictionary<long, Services.Blocked.Queries.Models.BlockedIp>>(ConfigSettings.CACHE_BLOCKED_IP) ?? new Dictionary<long, Services.Blocked.Queries.Models.BlockedIp>();

            if (reqIds.IsEmpty())
                cacheBlockedIp.Clear();
            else
                reqIds.ForEach(t => cacheBlockedIp.Remove(t));

            var result = await dispatcher.SendAsync(new GetBlockedIp
                                                    {
                                                        Ids = reqIds.IsEmpty() ? null : reqIds
                                                    }, cancellationToken);

            if (result.Code == Result.Success && !result.Content.IsEmpty())
            {
                result.Content.ForEach(t =>
                                       {
                                           if (cacheBlockedIp.TryAdd(t.Id, t))
                                               return;

                                           cacheBlockedIp[t.Id] = t;
                                       });
            }

            _cache.Set(ConfigSettings.CACHE_BLOCKED_IP, cacheBlockedIp, _cacheEntryOptions);
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
