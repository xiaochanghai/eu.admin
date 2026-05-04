using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.WxOpen.AdvancedAPIs;
using Senparc.Weixin.WxOpen.Entities;
using Senparc.Weixin.WxOpen.Entities.Request;
using Senparc.Weixin.WxOpen.MessageContexts;
using Senparc.Weixin.WxOpen.MessageHandlers;
using System.IO;
using System.Text;

namespace EU.Core.Extensions.Weixin;

/// <summary>
/// 小程序消息处理器，使用客服消息完成主要回复。
/// </summary>
public class SimpleWxOpenMessageHandler : WxOpenMessageHandler<DefaultWxOpenMessageContext>
{
    private readonly WxAccountSettingProvider _accountSettingProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WeixinReplyRuleProvider _ruleProvider;
    private readonly SenparcAccountRegistrationService _registrationService;

    public static Func<Stream, PostModel, int, IServiceProvider, SimpleWxOpenMessageHandler> GenerateMessageHandler =
        (stream, postModel, maxRecordCount, serviceProvider) =>
            new(stream, postModel, maxRecordCount, serviceProvider);

    public SimpleWxOpenMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount, IServiceProvider serviceProvider)
        : base(inputStream, postModel, maxRecordCount, serviceProvider: serviceProvider)
    {
        _accountSettingProvider = serviceProvider.GetService<WxAccountSettingProvider>();
        _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        _ruleProvider = serviceProvider.GetService<WeixinReplyRuleProvider>();
        _registrationService = serviceProvider.GetService<SenparcAccountRegistrationService>();
    }

    public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        if (!string.IsNullOrWhiteSpace(currentConfig?.AppId))
        {
            EnsureCurrentAccountRegistered(currentConfig);

            var content = requestMessage.Content?.Trim() ?? string.Empty;
            var normalized = content.ToLowerInvariant();
            var ruleProfile = _ruleProvider?.GetProfile(currentConfig.WeixinId);
            if (ruleProfile?.TextReplies != null && ruleProfile.TextReplies.TryGetValue(normalized, out var configuredReply))
            {
                await CustomApi.SendTextAsync(currentConfig.AppId, OpenId, configuredReply);
                return new SuccessResponseMessage();
            }

            if (normalized == "link")
            {
                await CustomApi.SendLinkAsync(
                    currentConfig.AppId,
                    OpenId,
                    "欢迎使用当前项目的小程序能力",
                    "这里已经迁入旧项目的小程序客服 link 指令，可继续替换为正式业务链接。",
                    "https://sdk.weixin.senparc.com/Docs/WxOpen/",
                    string.Empty);
                return new SuccessResponseMessage();
            }

            if (normalized == "客服")
            {
                await CustomApi.SendTextAsync(currentConfig.AppId, OpenId, "您即将进入客服会话。");
                return CreateResponseMessage<ResponseMessageTransfer_Customer_Service>();
            }

            var reply = normalized switch
            {
                "help" or "?" or "帮助" => BuildHelpMessage(currentConfig),
                "appid" => $"当前小程序 AppId：{currentConfig.AppId}",
                "openid" => $"当前用户 OpenId：{OpenId}",
                "card" => "旧项目 CARD 指令依赖封面素材上传，当前项目先保留文本提示。",
                _ => await BuildDefaultReplyAsync(requestMessage, currentConfig)
            };

            await CustomApi.SendTextAsync(currentConfig.AppId, OpenId, reply);
        }

        return new SuccessResponseMessage();
    }

    public override async Task<IResponseMessageBase> OnImageRequestAsync(RequestMessageImage requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        if (!string.IsNullOrWhiteSpace(currentConfig?.AppId))
        {
            EnsureCurrentAccountRegistered(currentConfig);
            await CustomApi.SendTextAsync(currentConfig.AppId, OpenId, "已收到图片消息。");
            await CustomApi.SendImageAsync(currentConfig.AppId, OpenId, requestMessage.MediaId);
        }

        return new SuccessResponseMessage();
    }

    public override async Task<IResponseMessageBase> OnEvent_UserEnterTempSessionRequestAsync(RequestMessageEvent_UserEnterTempSession requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        if (!string.IsNullOrWhiteSpace(currentConfig?.AppId))
        {
            EnsureCurrentAccountRegistered(currentConfig);
            var reply = string.IsNullOrWhiteSpace(currentConfig.SubscribeContent)
                ? "欢迎进入小程序客服会话。"
                : currentConfig.SubscribeContent;

            await CustomApi.SendTextAsync(currentConfig.AppId, OpenId, reply);
        }

        return new SuccessResponseMessage();
    }

    public override async Task<IResponseMessageBase> OnMiniProgramPageRequestAsync(RequestMessageMiniProgramPage requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        if (!string.IsNullOrWhiteSpace(currentConfig?.AppId))
        {
            EnsureCurrentAccountRegistered(currentConfig);

            var summary = $"收到小程序卡片：{requestMessage.Title}\n页面：{requestMessage.PagePath}";
            await CustomApi.SendTextAsync(currentConfig.AppId, OpenId, summary);
            if (!string.IsNullOrWhiteSpace(requestMessage.ThumbMediaId))
            {
                await CustomApi.SendImageAsync(currentConfig.AppId, OpenId, requestMessage.ThumbMediaId);
            }
        }

        return new SuccessResponseMessage();
    }

    public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
    {
        return new SuccessResponseMessage();
    }

    public override async Task<IResponseMessageBase> DefaultResponseMessageAsync(IRequestMessageBase requestMessage)
    {
        return await Task.FromResult(new SuccessResponseMessage());
    }

    private async Task<string> BuildDefaultReplyAsync(RequestMessageText requestMessage, EU.Core.Model.Entity.WxConfig currentConfig)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(currentConfig.AutoReplyContent))
        {
            builder.AppendLine(currentConfig.AutoReplyContent);
            builder.AppendLine();
        }

        builder.AppendLine($"您刚才发送了文字信息：{requestMessage.Content}");

        var messageContext = await GetCurrentMessageContext().ConfigureAwait(false);
        if (messageContext.RequestMessages.Count > 1)
        {
            builder.AppendLine();
            builder.AppendLine($"最近消息记录（{messageContext.RequestMessages.Count}/{messageContext.StorageData}）：");
            for (var i = messageContext.RequestMessages.Count - 2; i >= 0; i--)
            {
                var historyMessage = messageContext.RequestMessages[i];
                var historyContent = historyMessage switch
                {
                    RequestMessageText text => text.Content,
                    RequestMessageEvent_UserEnterTempSession => "[进入客服]",
                    _ => $"[非文字类型-{historyMessage.MsgType}]"
                };

                builder.AppendLine($"{historyMessage.CreateTime:HH:mm:ss} [{historyMessage.MsgType}] {historyContent}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("可继续测试：help、appid、openid、link、card、客服、图片消息、进入客服会话。");
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

    private void EnsureCurrentAccountRegistered(EU.Core.Model.Entity.WxConfig currentConfig)
    {
        if (currentConfig == null)
        {
            return;
        }

        var currentSetting = _accountSettingProvider?.GetWxOpenAccountSetting(currentConfig.WeixinId);
        _registrationService?.EnsureWxOpenRegistered(currentSetting, $"WxOpen:{currentConfig.WeixinId ?? currentConfig.AppId}");
    }

    private static string BuildHelpMessage(EU.Core.Model.Entity.WxConfig currentConfig)
    {
        return
            $"小程序：{currentConfig?.WeixinName ?? currentConfig?.WeixinId ?? "未配置"}\n" +
            "可用指令：help、appid、openid、link、card、客服\n" +
            "可测试消息：文本、图片、进入客服会话、小程序卡片。";
    }
}
