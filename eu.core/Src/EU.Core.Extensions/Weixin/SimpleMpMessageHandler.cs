using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.Entities.Request;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EU.Core.Extensions.Weixin;

/// <summary>
/// 公众号消息处理器，兼容当前项目的 WxConfig 动态配置。
/// </summary>
public class SimpleMpMessageHandler : MessageHandler<DefaultMpMessageContext>
{
    private readonly WxAccountSettingProvider _accountSettingProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WeixinReplyRuleProvider _ruleProvider;

    public static Func<Stream, PostModel, int, IServiceProvider, SimpleMpMessageHandler> GenerateMessageHandler =
        (stream, postModel, maxRecordCount, serviceProvider) =>
            new(stream, postModel, maxRecordCount, serviceProvider);

    public SimpleMpMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount, IServiceProvider serviceProvider)
        : base(inputStream, postModel, maxRecordCount, false, serviceProvider: serviceProvider)
    {
        _accountSettingProvider = serviceProvider.GetService<WxAccountSettingProvider>();
        _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        _ruleProvider = serviceProvider.GetService<WeixinReplyRuleProvider>();
    }

    public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        var ruleProfile = _ruleProvider?.GetProfile(currentConfig?.WeixinId);
        var content = requestMessage.Content?.Trim() ?? string.Empty;
        var normalized = content.ToLowerInvariant();

        if (TryBuildNewsReply(normalized, ruleProfile, out var newsReply))
        {
            return CreateNewsResponse(newsReply);
        }

        if (TryBuildMenuSelectionReply(content, ruleProfile, out var menuReply))
        {
            return CreateTextResponse(menuReply);
        }

        if (Regex.IsMatch(content, @"^\d+#\d+$"))
        {
            return CreateTextResponse($"您输入了：{content}，格式匹配成功。");
        }

        if (ruleProfile?.TextReplies != null && ruleProfile.TextReplies.TryGetValue(normalized, out var configuredReply))
        {
            return CreateTextResponse(configuredReply);
        }

        var reply = normalized switch
        {
            "help" or "?" or "菜单" or "帮助" => BuildHelpMessage(currentConfig),
            "appid" => $"当前公众号 AppId：{currentConfig?.AppId ?? "未配置"}",
            "openid" => $"当前用户 OpenId：{requestMessage.FromUserName}",
            "welcome" or "subscribe" => string.IsNullOrWhiteSpace(currentConfig?.SubscribeContent) ? "感谢关注。" : currentConfig.SubscribeContent,
            "jssdk" => "JSSDK 调试页请按当前系统域名自行配置，当前项目已完成基础 Senparc 接入。",
            "订阅" => "一次订阅消息能力需要结合具体模板和业务参数实现，当前已保留接口接入基础。",
            "审批" => "企业微信审批示例需要结合企业微信应用页面，这里保留为业务扩展入口。",
            _ => await BuildDefaultTextReplyAsync(requestMessage, currentConfig)
        };

        return CreateTextResponse(reply);
    }

    public override async Task<IResponseMessageBase> OnImageRequestAsync(RequestMessageImage requestMessage)
    {
        var requestCount = (await GetCurrentMessageContext()).RequestMessages.Count;
        if (requestCount % 2 == 0)
        {
            var news = CreateResponseMessage<ResponseMessageNews>();
            news.Articles.Add(new Article
            {
                Title = "您刚才发送了图片消息",
                Description = "当前项目已接收到图片，并按旧项目风格返回图文消息。",
                PicUrl = requestMessage.PicUrl,
                Url = requestMessage.PicUrl
            });
            news.Articles.Add(new Article
            {
                Title = "调试说明",
                Description = "如需改为素材回传或落库存储，可以继续扩展这里。",
                PicUrl = requestMessage.PicUrl,
                Url = requestMessage.PicUrl
            });
            return news;
        }

        var image = CreateResponseMessage<ResponseMessageImage>();
        image.Image.MediaId = requestMessage.MediaId;
        return image;
    }

    public override async Task<IResponseMessageBase> OnLocationRequestAsync(RequestMessageLocation requestMessage)
    {
        return CreateTextResponse($"收到位置：{requestMessage.Label} ({requestMessage.Location_X},{requestMessage.Location_Y})");
    }

    public override async Task<IResponseMessageBase> OnVoiceRequestAsync(RequestMessageVoice requestMessage)
    {
        return CreateTextResponse($"收到语音消息，MediaId：{requestMessage.MediaId}");
    }

    public override async Task<IResponseMessageBase> OnVideoRequestAsync(RequestMessageVideo requestMessage)
    {
        return CreateTextResponse($"收到视频消息，MediaId：{requestMessage.MediaId}");
    }

    public override async Task<IResponseMessageBase> OnLinkRequestAsync(RequestMessageLink requestMessage)
    {
        var content = $"收到链接消息：\n标题：{requestMessage.Title}\n描述：{requestMessage.Description}\n链接：{requestMessage.Url}";
        return CreateTextResponse(content);
    }

    public override IResponseMessageBase OnEvent_ClickRequest(RequestMessageEvent_Click requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        var ruleProfile = _ruleProvider?.GetProfile(currentConfig?.WeixinId);
        if (ruleProfile?.MenuEventReplies != null &&
            ruleProfile.MenuEventReplies.TryGetValue(requestMessage.EventKey ?? string.Empty, out var configuredReply))
        {
            return CreateTextResponse(configuredReply);
        }

        var content = requestMessage.EventKey switch
        {
            "OneClick" => "您点击了底部按钮。",
            "SubClickRoot_Text" => "您点击了子菜单按钮。",
            "SubClickRoot_News" => "您点击了图文子菜单，当前项目已接收到菜单点击事件。",
            "SendMenu" => "菜单评价功能已接通，您可以继续发送 s:101 / s:102 / s:103 测试选择菜单结果。",
            _ => $"收到菜单点击事件：{requestMessage.EventKey}"
        };

        return CreateTextResponse(content);
    }

    public override IResponseMessageBase OnEvent_SubscribeRequest(RequestMessageEvent_Subscribe requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        return CreateTextResponse(string.IsNullOrWhiteSpace(currentConfig?.SubscribeContent) ? "感谢关注。" : currentConfig.SubscribeContent);
    }

    public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
    {
        var currentConfig = GetCurrentConfig();
        var content = string.IsNullOrWhiteSpace(currentConfig?.AutoReplyContent)
            ? $"暂未处理消息类型：{requestMessage.MsgType}"
            : currentConfig.AutoReplyContent;
        return CreateTextResponse(content);
    }

    private async Task<string> BuildDefaultTextReplyAsync(RequestMessageText requestMessage, EU.Core.Model.Entity.WxConfig currentConfig)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(currentConfig?.AutoReplyContent))
        {
            builder.AppendLine(currentConfig.AutoReplyContent);
            builder.AppendLine();
        }

        builder.AppendLine($"您刚才发送了文字信息：{requestMessage.Content}");

        var messageContext = await GetCurrentMessageContext();
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
                    IRequestMessageEventKey eventKey => $"[事件-{eventKey.EventKey}]",
                    _ => $"[非文字类型-{historyMessage.MsgType}]"
                };

                builder.AppendLine($"{historyMessage.CreateTime:HH:mm:ss} [{historyMessage.MsgType}] {historyContent}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("可继续测试：help、appid、openid、welcome、jssdk、订阅、审批、s:101。");
        return builder.ToString().TrimEnd();
    }

    private bool TryBuildMenuSelectionReply(string content, WeixinReplyProfile ruleProfile, out string reply)
    {
        var menuKey = content.StartsWith("s:", StringComparison.OrdinalIgnoreCase) ? content[2..] : content;
        if (ruleProfile?.MenuSelectionReplies != null &&
            ruleProfile.MenuSelectionReplies.TryGetValue(menuKey, out var configuredReply) &&
            !string.IsNullOrWhiteSpace(configuredReply))
        {
            reply = configuredReply.Replace("{content}", content, StringComparison.OrdinalIgnoreCase);
            return true;
        }

        reply = menuKey switch
        {
            "101" => $"感谢您的评价（{content}）！我们会持续优化服务。",
            "102" => $"感谢您的评价（{content}）！欢迎继续给出更多建议。",
            "103" => $"感谢您的评价（{content}）！如有问题建议继续反馈详细场景。",
            "110" or "111" => "这里只是演示，同时支持多个选择菜单 key。",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(reply);
    }

    private bool TryBuildNewsReply(string normalizedContent, WeixinReplyProfile ruleProfile, out WeixinNewsReply reply)
    {
        reply = null;
        if (ruleProfile?.NewsReplies == null)
        {
            return false;
        }

        if (!ruleProfile.NewsReplies.TryGetValue(normalizedContent, out var configuredReply) || configuredReply == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(configuredReply.Title) && string.IsNullOrWhiteSpace(configuredReply.Url))
        {
            return false;
        }

        reply = configuredReply;
        return true;
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

    private ResponseMessageText CreateTextResponse(string content)
    {
        var response = CreateResponseMessage<ResponseMessageText>();
        response.Content = content;
        return response;
    }

    private ResponseMessageNews CreateNewsResponse(WeixinNewsReply reply)
    {
        var response = CreateResponseMessage<ResponseMessageNews>();
        response.Articles.Add(new Article
        {
            Title = reply.Title ?? "图文消息",
            Description = reply.Description ?? string.Empty,
            PicUrl = reply.PicUrl ?? string.Empty,
            Url = reply.Url ?? string.Empty
        });
        return response;
    }

    private static string BuildHelpMessage(EU.Core.Model.Entity.WxConfig currentConfig)
    {
        return
            $"公众号：{currentConfig?.WeixinName ?? currentConfig?.WeixinId ?? "未配置"}\n" +
            "可用指令：help、appid、openid、welcome、jssdk、订阅、审批\n" +
            "可测试消息：文本、图片、位置、语音、视频、链接、菜单 key(s:101/s:102/s:103)。";
    }
}
