using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Weixin.Entities;
using Senparc.Weixin.Work.AdvancedAPIs;
using Senparc.Weixin.Work.Containers;
using Senparc.Weixin.Work.Entities;
using Senparc.Weixin.Work.Entities.Request;
using Senparc.Weixin.Work.MessageContexts;
using Senparc.Weixin.Work.MessageHandlers;
using System.IO;
using System.Text;

namespace EU.Core.Extensions.Weixin;

/// <summary>
/// 企业微信消息处理器。
/// </summary>
public class SimpleWorkMessageHandler : WorkMessageHandler<DefaultWorkMessageContext>
{
    private readonly WxAccountSettingProvider _accountSettingProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WeixinReplyRuleProvider _ruleProvider;
    private readonly SenparcAccountRegistrationService _registrationService;

    public static Func<Stream, PostModel, int, IServiceProvider, SimpleWorkMessageHandler> GenerateMessageHandler =
        (stream, postModel, maxRecordCount, serviceProvider) =>
            new(stream, postModel, maxRecordCount, serviceProvider);

    public SimpleWorkMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount, IServiceProvider serviceProvider)
        : base(inputStream, postModel, maxRecordCount, serviceProvider: serviceProvider)
    {
        _accountSettingProvider = serviceProvider.GetService<WxAccountSettingProvider>();
        _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        _ruleProvider = serviceProvider.GetService<WeixinReplyRuleProvider>();
        _registrationService = serviceProvider.GetService<SenparcAccountRegistrationService>();
    }

    public override IWorkResponseMessageBase OnTextRequest(RequestMessageText requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        var currentSetting = currentConfig == null ? null : _accountSettingProvider?.GetWorkAccountSetting(currentConfig.WeixinId);
        _registrationService?.EnsureWorkRegistered(currentSetting, $"Work:{currentConfig?.WeixinId ?? currentConfig?.OriginId}");

        var content = requestMessage.Content?.Trim() ?? string.Empty;
        var normalized = content.ToLowerInvariant();
        var ruleProfile = _ruleProvider?.GetProfile(currentConfig?.WeixinId);
        var response = CreateResponseMessage<ResponseMessageText>();

        if (ruleProfile?.TextReplies != null && ruleProfile.TextReplies.TryGetValue(normalized, out var configuredReply))
        {
            response.Content = configuredReply;
            return response;
        }

        response.Content = normalized switch
        {
            "help" or "?" or "帮助" => BuildHelpMessage(currentConfig),
            "agentid" => $"当前企业微信 AgentId：{currentConfig?.AppId ?? "未配置"}",
            "corpid" => $"当前企业微信 CorpId：{currentConfig?.OriginId ?? "未配置"}",
            _ => BuildDefaultReply(content, currentConfig)
        };

        if (currentSetting != null &&
            !string.IsNullOrWhiteSpace(currentSetting.WeixinCorpId) &&
            !string.IsNullOrWhiteSpace(currentSetting.WeixinCorpSecret) &&
            !string.IsNullOrWhiteSpace(currentSetting.WeixinCorpAgentId))
        {
            var appKey = AccessTokenContainer.BuildingKey(currentSetting.WeixinCorpId, currentSetting.WeixinCorpSecret);
            MassApi.SendText(appKey, currentSetting.WeixinCorpAgentId, $"企业微信已收到消息：{content}", OpenId);
        }

        return response;
    }

    public override IWorkResponseMessageBase OnImageRequest(RequestMessageImage requestMessage)
    {
        var response = CreateResponseMessage<ResponseMessageText>();
        response.Content = $"已收到企业微信图片消息，MediaId：{requestMessage.MediaId}";
        return response;
    }

    public override IWorkResponseMessageBase OnEvent_PicPhotoOrAlbumRequest(RequestMessageEvent_Pic_Photo_Or_Album requestMessage)
    {
        var response = CreateResponseMessage<ResponseMessageText>();
        response.Content = "已收到拍照或相册事件。";
        return response;
    }

    public override IWorkResponseMessageBase OnEvent_LocationRequest(RequestMessageEvent_Location requestMessage)
    {
        var response = CreateResponseMessage<ResponseMessageText>();
        response.Content = $"收到位置：{requestMessage.Latitude},{requestMessage.Longitude}";
        return response;
    }

    public override IWorkResponseMessageBase OnEvent_EnterAgentRequest(RequestMessageEvent_Enter_Agent requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        var response = CreateResponseMessage<ResponseMessageText>();
        response.Content = string.IsNullOrWhiteSpace(currentConfig?.SubscribeContent)
            ? "欢迎进入企业微信应用。"
            : currentConfig.SubscribeContent;
        return response;
    }

    public override IWorkResponseMessageBase DefaultResponseMessage(IWorkRequestMessageBase requestMessage)
    {
        var response = CreateResponseMessage<ResponseMessageText>();
        response.Content = "企业微信消息已接收。";
        return response;
    }

    private string BuildDefaultReply(string content, EU.Core.Model.Entity.WxConfig currentConfig)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(currentConfig?.AutoReplyContent))
        {
            builder.AppendLine(currentConfig.AutoReplyContent);
            builder.AppendLine();
        }

        builder.AppendLine($"已收到企业微信消息：{content}");
        builder.AppendLine("可继续测试：help、agentid、corpid、图片、位置、进入应用。");
        return builder.ToString().TrimEnd();
    }

    private EU.Core.Model.Entity.WxConfig GetCurrentConfig()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        var weixinId = httpContext?.Request.Query["userName"].ToString();
        if (string.IsNullOrWhiteSpace(weixinId))
        {
            weixinId = httpContext?.Request.Query["weixinId"].ToString();
        }

        return _accountSettingProvider?.GetByWeixinId(weixinId);
    }

    private static string BuildHelpMessage(EU.Core.Model.Entity.WxConfig currentConfig)
    {
        return
            $"企业微信：{currentConfig?.WeixinName ?? currentConfig?.WeixinId ?? "未配置"}\n" +
            "可用指令：help、agentid、corpid\n" +
            "可测试消息：文本、图片、位置、拍照/相册、进入应用。";
    }
}
