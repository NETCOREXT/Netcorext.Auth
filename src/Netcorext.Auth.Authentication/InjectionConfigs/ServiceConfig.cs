using Netcorext.Auth.Authentication.Settings;
using Netcorext.Configuration.Extensions;

namespace Netcorext.Auth.Authentication.InjectionConfigs;

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
