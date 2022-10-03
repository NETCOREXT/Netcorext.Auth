using System.Text.Json;
using FreeRedis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Netcorext.Auth.API.Services.User.Commands;
using Netcorext.Auth.API.Settings;
using Netcorext.Contracts;
using Netcorext.Mediator.Pipelines;

namespace Netcorext.Auth.API.Services.User.Pipelines;

public class UserChangeNotifyPipeline : IRequestPipeline<CreateUser, Result<long?>>,
                                        IRequestPipeline<UpdateUser, Result>,
                                        IRequestPipeline<DeleteUser, Result>
{
    private readonly RedisClient _redis;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConfigSettings _config;

    public UserChangeNotifyPipeline(RedisClient redis, IOptions<ConfigSettings> config, IOptions<JsonOptions> jsonOptions)
    {
        _redis = redis;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _config = config.Value;
    }

    public async Task<Result<long?>?> InvokeAsync(CreateUser request, PipelineDelegate<Result<long?>> next, CancellationToken cancellationToken = new())
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessCreated && result.Content.HasValue)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT], result.Content.Value);

        if (result == Result.SuccessCreated && result.Content.HasValue && (request.Roles ?? Array.Empty<CreateUser.UserRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT], result.Content.Value);

        return result;
    }

    public async Task<Result?> InvokeAsync(UpdateUser request, PipelineDelegate<Result> next, CancellationToken cancellationToken = new())
    {
        var result = await next(request, cancellationToken);

        if (result == Result.SuccessNoContent)
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT], request.Id);

        if (result == Result.SuccessNoContent && (request.Roles ?? Array.Empty<UpdateUser.UserRole>()).Any())
            await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT], request.Id);

        return result;
    }

    public async Task<Result?> InvokeAsync(DeleteUser request, PipelineDelegate<Result> next, CancellationToken cancellationToken = new())
    {
        var result = await next(request, cancellationToken);

        if (result != Result.SuccessNoContent) return result;

        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_ROLE_CHANGE_EVENT], request.Id);
        await NotifyAsync(_config.Queues[ConfigSettings.QUEUES_USER_CHANGE_EVENT], request.Id);

        return result;
    }

    private Task NotifyAsync(string channelId, params long[] ids)
    {
        var value = JsonSerializer.Serialize(ids, _jsonOptions);

        _redis.Publish(channelId, value);

        return Task.CompletedTask;
    }
}