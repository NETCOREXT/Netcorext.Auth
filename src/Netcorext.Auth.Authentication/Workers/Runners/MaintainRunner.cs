using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Services.Maintenance.Queries;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Mediator;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class MaintainRunner : IWorkerRunner<AuthWorker>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly ConfigSettings _config;
    private readonly ILogger<MaintainRunner> _logger;

    public MaintainRunner(IServiceProvider serviceProvider, RedisClient redis, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, ISerializer serializer, KeyLocker locker, IOptions<ConfigSettings> config, ILogger<MaintainRunner> logger)
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
        _logger.LogDebug("{Message}", nameof(MaintainRunner));

        await UpdateMaintainAsync(null, cancellationToken);

        _subscriber?.Dispose();
        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_MAINTAIN_CHANGE_EVENT], Handler);

        return;

        void Handler(string s, object o)
        {
            _ = UpdateMaintainAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateMaintainAsync(string? ids, CancellationToken cancellationToken = default)
    {
        var lockerKey = ids.IsEmpty() ? nameof(UpdateMaintainAsync) : nameof(UpdateMaintainAsync) + "/" + ids;

        try
        {
            await _locker.WaitAsync(lockerKey);

            _logger.LogInformation(nameof(UpdateMaintainAsync));

            using var scope = _serviceProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
            var result = await dispatcher.SendAsync(new GetMaintain(), cancellationToken);

            if (result.Code != Result.Success)
                return;

            if (result.Content.IsEmpty())
            {
                _cache.Remove($"{ConfigSettings.CACHE_MAINTAIN}");
                return;
            }

            _cache.Set($"{ConfigSettings.CACHE_MAINTAIN}", result.Content, _cacheEntryOptions);
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
