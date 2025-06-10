using Microsoft.Extensions.Options;
using Netcorext.Auth.Gateway.Settings;
using Netcorext.Configuration.Extensions;

namespace Netcorext.Auth.Gateway.InjectionConfigs;

[Injection]
public class ServiceConfig
{
    public ServiceConfig(IServiceCollection services, IConfiguration configuration)
    {
        var cfg = configuration.Get<ConfigSettings>()!;

        services.AddMediator()
                .AddRedisQueuing((_, options) =>
                                 {
                                     options.ConnectionString = cfg.Connections.Redis.GetDefault().Connection;
                                 })
                .AddLoggingPipeline()
                .AddPerformancePipeline((_, options) =>
                                        {
                                            options.SlowCommandTimes = cfg.AppSettings.ServiceSlowCommandLoggingThreshold;
                                        })
                .AddValidatorPipeline();
    }
}
