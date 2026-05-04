using System.Collections.Concurrent;
using Senparc.Weixin.Entities;

namespace EU.Core.Extensions.Weixin;

/// <summary>
/// Registers dynamic WxConfig accounts into Senparc token containers on demand.
/// </summary>
public class SenparcAccountRegistrationService
{
    private readonly ConcurrentDictionary<string, byte> _registeredKeys = new(StringComparer.OrdinalIgnoreCase);

    public void EnsureMpRegistered(SenparcWeixinSetting setting, string accountName)
    {
        if (setting == null ||
            string.IsNullOrWhiteSpace(setting.WeixinAppId) ||
            string.IsNullOrWhiteSpace(setting.WeixinAppSecret))
        {
            return;
        }

        var key = $"mp:{setting.WeixinAppId}";
        if (_registeredKeys.TryAdd(key, 0))
        {
            Senparc.Weixin.MP.Containers.AccessTokenContainer.Register(setting.WeixinAppId, setting.WeixinAppSecret, accountName);
            Senparc.Weixin.MP.Containers.JsApiTicketContainer.Register(setting.WeixinAppId, setting.WeixinAppSecret, accountName);
            Senparc.Weixin.MP.Containers.OAuthAccessTokenContainer.Register(setting.WeixinAppId, setting.WeixinAppSecret, null, accountName);
        }
    }

    public void EnsureWxOpenRegistered(SenparcWeixinSetting setting, string accountName)
    {
        if (setting == null ||
            string.IsNullOrWhiteSpace(setting.WxOpenAppId) ||
            string.IsNullOrWhiteSpace(setting.WxOpenAppSecret))
        {
            return;
        }

        var key = $"wxopen:{setting.WxOpenAppId}";
        if (_registeredKeys.TryAdd(key, 0))
        {
            Senparc.Weixin.WxOpen.Containers.AccessTokenContainer.Register(setting.WxOpenAppId, setting.WxOpenAppSecret, accountName);
        }
    }

    public void EnsureWorkRegistered(SenparcWeixinSetting setting, string accountName)
    {
        if (setting == null ||
            string.IsNullOrWhiteSpace(setting.WeixinCorpId) ||
            string.IsNullOrWhiteSpace(setting.WeixinCorpSecret))
        {
            return;
        }

        var key = $"work:{setting.WeixinCorpId}:{setting.WeixinCorpSecret}";
        if (_registeredKeys.TryAdd(key, 0))
        {
            Senparc.Weixin.Work.Containers.AccessTokenContainer.Register(setting.WeixinCorpId, setting.WeixinCorpSecret, accountName);
            Senparc.Weixin.Work.Containers.JsApiTicketContainer.Register(setting.WeixinCorpId, setting.WeixinCorpSecret, accountName);
        }
    }

}
