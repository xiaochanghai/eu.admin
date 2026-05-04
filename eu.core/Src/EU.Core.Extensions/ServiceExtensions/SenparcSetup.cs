using EU.Core.Extensions.Weixin;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Cache.CsRedis;
using Senparc.NeuChar.MessageHandlers;
using Senparc.Weixin;
using Senparc.Weixin.AspNet;
using Senparc.Weixin.Cache.CsRedis;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.MessageHandlers.Middleware;
using Senparc.Weixin.TenPay;
using Senparc.Weixin.Work.MessageHandlers.Middleware;
using Senparc.Weixin.WxOpen.MessageHandlers.Middleware;
using Senparc.Weixin.RegisterServices;
using CO2NETCsRedisRegister = Senparc.CO2NET.Cache.CsRedis.Register;

namespace EU.Core.Extensions;

/// <summary>
/// Senparc 微信框架注册
/// </summary>
public static class SenparcSetup
{
    public static void AddSenparcSetup(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // 必须先显式绑定配置，否则 AddSenparcGlobalServices 内部反射会报 NullReferenceException
        services.Configure<SenparcSetting>(configuration.GetSection("SenparcSetting"));
        services.Configure<SenparcWeixinSetting>(configuration.GetSection("SenparcWeixinSetting"));

        services.AddSingleton<WxAccountSettingProvider>();
        services.AddSingleton<SenparcAccountRegistrationService>();
        services.AddSingleton<TenPayV3NotifyService>();
        services.AddSingleton<WeixinPayNotifyPersistenceService>();
        services.AddSingleton<WeixinOutboundMessageService>();
        services.AddSingleton<WeixinUserBindingService>();
        services.Configure<WeixinReplyRuleOptions>(configuration.GetSection("WeixinReplyRules"));
        services.AddSingleton<WeixinReplyRuleProvider>();
        services.AddMvc();
        services.AddSenparcGlobalServices(configuration);
        services.AddSenparcWeixinServices(configuration);
    }

    public static void UseSenparcSetup(this WebApplication app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        var senparcSetting = app.Services.GetRequiredService<IOptions<SenparcSetting>>();
        var senparcWeixinSetting = app.Services.GetRequiredService<IOptions<SenparcWeixinSetting>>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var accountSettingProvider = app.Services.GetRequiredService<WxAccountSettingProvider>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SenparcSetup");

        // 确保 CsRedis 程序集被加载（.NET 懒加载可能导致 UseSenparcGlobal 内部反射找不到类型）
        _ = typeof(CO2NETCsRedisRegister);

        app.UseSenparcGlobal(app.Environment, senparcSetting.Value, globalRegister =>
        {
            // DefaultCacheNamespace 已在 appsettings.json 的 SenparcSetting 中配置，
            // ChangeDefaultCacheNamespace 内部通过反射调用，在某些环境下会报 NullReferenceException
            // globalRegister.ChangeDefaultCacheNamespace("EUCoreSenparcCache");

            if (TryGetRedisConfiguration(senparcSetting.Value, configuration, out var redisConfiguration))
            {
                TryEnableGlobalRedisCache(redisConfiguration, logger);
            }
        }, true)
        .UseSenparcWeixin(senparcWeixinSetting.Value, (weixinRegister, _) =>
        {
            if (HasTenPayV3Configuration(senparcWeixinSetting.Value))
            {
                weixinRegister.RegisterTenpayV3(senparcWeixinSetting.Value, "EU.Core 微信支付(V3)");
            }
        });

        app.UseMessageHandlerForMp("/WeixinAsync", SimpleMpMessageHandler.GenerateMessageHandler, options =>
        {
            options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;
            options.AccountSettingFunc = context =>
            {
                var weixinId = context.Request.Query["userName"].ToString();
                if (string.IsNullOrWhiteSpace(weixinId))
                {
                    weixinId = context.Request.Query["weixinId"].ToString();
                }

                return accountSettingProvider.GetMpAccountSetting(weixinId);
            };
        });

        app.UseMessageHandlerForWxOpen("/WxOpenAsync", SimpleWxOpenMessageHandler.GenerateMessageHandler, options =>
        {
            options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;
            options.AccountSettingFunc = context =>
            {
                var weixinId = context.Request.Query["userName"].ToString();
                if (string.IsNullOrWhiteSpace(weixinId))
                {
                    weixinId = context.Request.Query["weixinId"].ToString();
                }

                return accountSettingProvider.GetWxOpenAccountSetting(weixinId);
            };
        });

        app.UseMessageHandlerForWork("/WorkAsync", SimpleWorkMessageHandler.GenerateMessageHandler, options =>
        {
            options.AccountSettingFunc = context =>
            {
                var weixinId = context.Request.Query["userName"].ToString();
                if (string.IsNullOrWhiteSpace(weixinId))
                {
                    weixinId = context.Request.Query["weixinId"].ToString();
                }

                return accountSettingProvider.GetWorkAccountSetting(weixinId);
            };
        });
    }

    private static bool TryGetRedisConfiguration(SenparcSetting senparcSetting, IConfiguration configuration, out string redisConfiguration)
    {
        redisConfiguration = senparcSetting?.Cache_Redis_Configuration;
        if (string.IsNullOrWhiteSpace(redisConfiguration))
        {
            redisConfiguration = configuration["Redis:ConnectionString"];
        }

        return !string.IsNullOrWhiteSpace(redisConfiguration);
    }

    private static bool HasTenPayV3Configuration(SenparcWeixinSetting setting)
    {
        if (setting == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(setting.TenPayV3_AppId)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_MchId)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_APIv3Key)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_PrivateKey)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_SerialNumber);
    }

    private static void TryEnableGlobalRedisCache(string redisConfiguration, ILogger logger)
    {
        try
        {
            CO2NETCsRedisRegister.SetConfigurationOption(redisConfiguration);
            CO2NETCsRedisRegister.UseKeyValueRedisNow();
            logger.LogInformation("Senparc global Redis cache enabled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enable Senparc Redis cache. Fallback to in-memory cache.");
        }
    }
}
