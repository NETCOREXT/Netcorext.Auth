using FreeRedis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Netcorext.Auth.Authentication.Settings;
using Netcorext.Contracts;
using Netcorext.Extensions.Commons;
using Netcorext.Serialization;
using Netcorext.Worker;

namespace Netcorext.Auth.Authentication.Workers;

internal class TokenRunner : IWorkerRunner<AuthWorker>
{
    private readonly RedisClient _redis;
    private IDisposable? _subscriber;
    private readonly IMemoryCache _cache;
    private readonly ISerializer _serializer;
    private readonly KeyLocker _locker;
    private readonly ConfigSettings _config;
    private readonly ILogger<TokenRunner> _logger;

    public TokenRunner(RedisClient redis, IMemoryCache cache, ISerializer serializer, KeyLocker locker, IOptions<ConfigSettings> config, ILogger<TokenRunner> logger)
    {
        _redis = redis;
        _cache = cache;
        _serializer = serializer;
        _locker = locker;
        _config = config.Value;
        _logger = logger;
    }

    public Task InvokeAsync(AuthWorker worker, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{Message}", nameof(TokenRunner));

        _subscriber?.Dispose();
        _subscriber = _redis.Subscribe(_config.Queues[ConfigSettings.QUEUES_TOKEN_REVOKE_EVENT], Handler);

        return Task.CompletedTask;

        void Handler(string s, object o)
        {
            _ = UpdateTokenAsync(o.ToString(), cancellationToken);
        }
    }

    private async Task UpdateTokenAsync(string? ids, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ids)) return;

        var lockerKey = ids.IsEmpty() ? nameof(UpdateTokenAsync) : nameof(UpdateTokenAsync) + "/" + ids;

        try
        {
            await _locker.WaitAsync(lockerKey);

            _logger.LogInformation(nameof(UpdateTokenAsync));

            var tokens = _serializer.Deserialize<string[]>(ids);

            if (tokens.IsEmpty()) return;

            foreach (var token in tokens)
            {
                _cache.Set(token, Result.UnauthorizedAndCannotRefreshToken, DateTimeOffset.UtcNow.AddMilliseconds(_config.AppSettings.CacheTokenExpires));
            }
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
