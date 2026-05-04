using EU.Core.Extensions.Weixin.TemplateMessage;
using Microsoft.Extensions.Options;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.Work.AdvancedAPIs;
using Senparc.Weixin.Work.Containers;
using WxOpenCustomApi = Senparc.Weixin.WxOpen.AdvancedAPIs.CustomApi;

namespace EU.Core.Extensions.Weixin;

/// <summary>
/// Common outbound message helpers migrated from the legacy Senparc integration.
/// </summary>
public class WeixinOutboundMessageService
{
    private readonly WxAccountSettingProvider _accountSettingProvider;
    private readonly SenparcAccountRegistrationService _registrationService;
    private readonly SenparcWeixinSetting _defaultSetting;

    public WeixinOutboundMessageService(
        WxAccountSettingProvider accountSettingProvider,
        SenparcAccountRegistrationService registrationService,
        IOptions<SenparcWeixinSetting> defaultSetting)
    {
        _accountSettingProvider = accountSettingProvider;
        _registrationService = registrationService;
        _defaultSetting = defaultSetting.Value;
    }

    public async Task SendMpTextAsync(string weixinId, string openId, string content)
    {
        var setting = _accountSettingProvider.GetMpAccountSetting(weixinId);
        _registrationService.EnsureMpRegistered(setting, $"MP:{weixinId ?? setting.WeixinAppId}");
        await CustomApi.SendTextAsync(setting.WeixinAppId, openId, content);
    }

    public async Task SendWxOpenTextAsync(string weixinId, string openId, string content)
    {
        var setting = _accountSettingProvider.GetWxOpenAccountSetting(weixinId);
        _registrationService.EnsureWxOpenRegistered(setting, $"WxOpen:{weixinId ?? setting.WxOpenAppId}");
        await WxOpenCustomApi.SendTextAsync(setting.WxOpenAppId, openId, content);
    }

    public async Task SendWorkTextAsync(string weixinId, string userId, string content)
    {
        var setting = _accountSettingProvider.GetWorkAccountSetting(weixinId);
        _registrationService.EnsureWorkRegistered(setting, $"Work:{weixinId ?? setting.WeixinCorpId}");

        var appKey = AccessTokenContainer.BuildingKey(setting.WeixinCorpId, setting.WeixinCorpSecret);
        await MassApi.SendTextAsync(appKey, setting.WeixinCorpAgentId, content, userId);
    }

    public async Task SendMpPaySuccessTemplateAsync(
        string weixinId,
        string openId,
        string productName,
        string notice,
        string url = null,
        string templateId = null)
    {
        var setting = _accountSettingProvider.GetMpAccountSetting(weixinId);
        _registrationService.EnsureMpRegistered(setting, $"MP:{weixinId ?? setting.WeixinAppId}");

        var data = new WeixinTemplate_PaySuccess(
            url ?? string.Empty,
            productName,
            notice,
            string.IsNullOrWhiteSpace(templateId) ? WeixinTemplate_PaySuccess.DefaultTemplateId : templateId);

        await TemplateApi.SendTemplateMessageAsync(setting.WeixinAppId, openId, data.TemplateId, data.Url, data);
    }

    public async Task SendExceptionAlertTemplateAsync(
        string openId,
        string host,
        string service,
        string status,
        string message,
        string remark,
        string url = null,
        string templateId = null)
    {
        if (string.IsNullOrWhiteSpace(openId) || string.IsNullOrWhiteSpace(_defaultSetting?.WeixinAppId))
        {
            return;
        }

        _registrationService.EnsureMpRegistered(_defaultSetting, "MP:default");

        var data = new WeixinTemplate_ExceptionAlert(
            "微信异常通知",
            host,
            service,
            status,
            message,
            remark,
            url,
            string.IsNullOrWhiteSpace(templateId) ? WeixinTemplate_ExceptionAlert.DefaultTemplateId : templateId);

        await TemplateApi.SendTemplateMessageAsync(_defaultSetting.WeixinAppId, openId, data.TemplateId, data.Url, data);
    }
}
