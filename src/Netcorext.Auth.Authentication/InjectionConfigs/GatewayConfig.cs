using Netcorext.Auth.Authentication.Settings;
using Netcorext.Extensions.Commons;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

[Injection]
public class GatewayConfig
{
    public GatewayConfig(IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.Get<ConfigSettings>()!;

        var gatewayConfig = configuration.GetSection("ReverseProxy");

        var proxyBuilder = services.AddReverseProxy()
                                   .ConfigureHttpClient((_, handler) =>
                                                        {
                                                            handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(cfg.AppSettings.PooledConnectionLifetime);
                                                            handler.ConnectTimeout = TimeSpan.FromMilliseconds(cfg.AppSettings.ConnectTimeout);
                                                        });

        if (gatewayConfig.Exists())
            proxyBuilder.LoadFromConfig(gatewayConfig);
        else
            proxyBuilder.LoadFromMemory(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());

        proxyBuilder.AddTransforms(builder =>
                                   {
                                       builder.AddXForwarded(ForwardedTransformActions.Off);
                                       builder.AddXForwardedFor(action: ForwardedTransformActions.Append);
                                       builder.AddRequestTransform(ctx =>
                                                                   {
                                                                       if (cfg.AppSettings.RequestHeaderRemovePrefixes.IsEmpty())
                                                                           return ValueTask.CompletedTask;

                                                                       foreach (var header in ctx.HttpContext.Request.Headers)
                                                                       {
                                                                           if (!cfg.AppSettings.RequestHeaderRemovePrefixes.Any(t => header.Key.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
                                                                               continue;

                                                                           ctx.ProxyRequest.Headers.Remove(header.Key);
                                                                           ctx.ProxyRequest.Content?.Headers.Remove(header.Key);
                                                                       }

                                                                       return ValueTask.CompletedTask;
                                                                   });
                                       builder.AddResponseTransform(ctx =>
                                                                    {
                                                                        if (ctx.ProxyResponse == null || !ctx.ProxyResponse.Headers.TryGetValues(cfg.AppSettings.RequestIdHeaderName, out var requestIds))
                                                                            return ValueTask.CompletedTask;

                                                                        var requestIdHeader = string.Join(',', requestIds);

                                                                        if (string.IsNullOrWhiteSpace(requestIdHeader))
                                                                            return ValueTask.CompletedTask;

                                                                        ctx.HttpContext.Response.Headers[cfg.AppSettings.RequestIdHeaderName] = requestIdHeader;

                                                                        return ValueTask.CompletedTask;
                                                                    });
                                   });
    }
}
