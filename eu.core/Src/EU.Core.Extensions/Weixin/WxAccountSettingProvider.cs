using EU.Core.Model;
using EU.Core.Model.Entity;
using Microsoft.Extensions.Options;
using Senparc.Weixin.Entities;
using SqlSugar;

namespace EU.Core.Extensions.Weixin;

/// <summary>
/// 微信账号配置提供器
/// </summary>
public class WxAccountSettingProvider
{
    private readonly ISqlSugarClient _db;
    private readonly SenparcWeixinSetting _defaultSetting;

    public WxAccountSettingProvider(ISqlSugarClient db, IOptions<SenparcWeixinSetting> defaultSetting)
    {
        _db = db;
        _defaultSetting = defaultSetting.Value;
    }

    public SenparcWeixinSetting GetMpAccountSetting(string weixinId)
    {
        if (string.IsNullOrWhiteSpace(weixinId))
        {
            return _defaultSetting;
        }

        var config = GetByWeixinId(weixinId);
        if (config == null)
        {
            return _defaultSetting;
        }

        return new SenparcWeixinSetting
        {
            Token = config.Token,
            EncodingAESKey = config.AESKey,
            WeixinAppId = config.AppId,
            WeixinAppSecret = config.AppSecret
        };
    }

    public SenparcWeixinSetting GetWxOpenAccountSetting(string weixinId)
    {
        if (string.IsNullOrWhiteSpace(weixinId))
        {
            return _defaultSetting;
        }

        var config = GetByWeixinId(weixinId);
        if (config == null)
        {
            return _defaultSetting;
        }

        return new SenparcWeixinSetting
        {
            WxOpenToken = config.Token,
            WxOpenEncodingAESKey = config.AESKey,
            WxOpenAppId = config.AppId,
            WxOpenAppSecret = config.AppSecret
        };
    }

    public SenparcWeixinSetting GetWorkAccountSetting(string weixinId)
    {
        if (string.IsNullOrWhiteSpace(weixinId))
        {
            return _defaultSetting;
        }

        var config = GetByWeixinId(weixinId);
        if (config == null)
        {
            return _defaultSetting;
        }

        return new SenparcWeixinSetting
        {
            WeixinCorpToken = config.Token,
            WeixinCorpEncodingAESKey = config.AESKey,
            WeixinCorpAgentId = config.AppId,
            WeixinCorpId = config.OriginId,
            WeixinCorpSecret = config.AppSecret,
            WeixinAppId = config.OriginId,
            WeixinAppSecret = config.AppSecret
        };
    }

    public WxConfig GetByAppId(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            return null;
        }

        return _db.Queryable<WxConfig>()
            .First(x => x.AppId == appId && x.IsDeleted == false);
    }

    public WxConfig GetByWeixinId(string weixinId)
    {
        if (string.IsNullOrWhiteSpace(weixinId))
        {
            return null;
        }

        return _db.Queryable<WxConfig>()
            .First(x => x.WeixinId == weixinId && x.IsDeleted == false);
    }
}
