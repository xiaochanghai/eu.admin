/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* WxConfig.cs
*
*功 能： N / A
* 类 名： WxConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/6/21 0:48:51  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/ 
using System.Security.Cryptography;
using System.Text;
using EU.Core.Extensions.Weixin;
using Microsoft.Extensions.Options;
using Senparc.Weixin.Entities;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 账号配置(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_WX)]
public class WxConfigController : BaseController<IWxConfigServices, WxConfig, WxConfigDto, InsertWxConfigInput, EditWxConfigInput>
{
    private readonly WxAccountSettingProvider _accountSettingProvider;
    private readonly WeixinReplyRuleProvider _replyRuleProvider;
    private readonly IConfiguration _configuration;
    private readonly SenparcWeixinSetting _senparcWeixinSetting;

    public WxConfigController(
        IWxConfigServices service,
        WxAccountSettingProvider accountSettingProvider,
        WeixinReplyRuleProvider replyRuleProvider,
        IConfiguration configuration,
        IOptions<SenparcWeixinSetting> senparcWeixinSetting) : base(service)
    {
        _accountSettingProvider = accountSettingProvider;
        _replyRuleProvider = replyRuleProvider;
        _configuration = configuration;
        _senparcWeixinSetting = senparcWeixinSetting.Value;
    }

    [HttpGet("CallbackPreview/{weixinId}")]
    public ServiceResult<object> CallbackPreview(string weixinId)
    {
        var config = _accountSettingProvider.GetByWeixinId(weixinId);
        if (config == null)
            return Failed<object>($"未找到微信配置：{weixinId}");

        var baseUrl = GetBaseUrl();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = "123456";
        var replyProfile = _replyRuleProvider.GetProfile(config.WeixinId);
        return Success<object>(new
        {
            config.WeixinId,
            config.WeixinName,
            config.InterfaceType,
            Mp = $"{baseUrl}/WeixinAsync?weixinId={config.WeixinId}",
            WxOpen = $"{baseUrl}/WxOpenAsync?weixinId={config.WeixinId}",
            Work = $"{baseUrl}/WorkAsync?weixinId={config.WeixinId}",
            TenPayV3 = new
            {
                NotifyUrl = string.IsNullOrWhiteSpace(_senparcWeixinSetting.TenPayV3_TenpayNotify)
                    ? $"{baseUrl}/TenpayApiV3/PayNotifyUrl"
                    : _senparcWeixinSetting.TenPayV3_TenpayNotify,
                WxOpenNotifyUrl = string.IsNullOrWhiteSpace(_senparcWeixinSetting.TenPayV3_WxOpenTenpayNotify)
                    ? $"{baseUrl}/TenpayApiV3/PayNotifyUrlWxOpen"
                    : _senparcWeixinSetting.TenPayV3_WxOpenTenpayNotify,
                HasAppId = !string.IsNullOrWhiteSpace(_senparcWeixinSetting.TenPayV3_AppId),
                HasMchId = !string.IsNullOrWhiteSpace(_senparcWeixinSetting.TenPayV3_MchId),
                HasApiV3Key = !string.IsNullOrWhiteSpace(_senparcWeixinSetting.TenPayV3_APIv3Key),
                HasPrivateKey = !string.IsNullOrWhiteSpace(_senparcWeixinSetting.TenPayV3_PrivateKey),
                HasSerialNumber = !string.IsNullOrWhiteSpace(_senparcWeixinSetting.TenPayV3_SerialNumber),
                Registered = HasTenPayV3Configuration(_senparcWeixinSetting)
            },
            config.Token,
            config.AppId,
            config.OriginId,
            HasAppSecret = !string.IsNullOrWhiteSpace(config.AppSecret),
            HasAesKey = !string.IsNullOrWhiteSpace(config.AESKey),
            VerifySample = new
            {
                timestamp,
                nonce,
                signature = BuildSignature(config.Token, timestamp, nonce)
            },
            TestCommands = new
            {
                Mp = new[] { "help", "appid", "openid", "welcome", "s:101", "1#2" },
                WxOpen = new[] { "help", "appid", "openid" },
                Work = new[] { "help", "agentid", "corpid" }
            },
            ReplyRules = replyProfile
        }, "回调地址预览成功");
    }

    [HttpPost("VerifySignature")]
    public ServiceResult<object> VerifySignature([FromBody] WxSignatureVerifyRequest request)
    {
        if (request == null)
            return Failed<object>("请求不能为空");

        var config = !string.IsNullOrWhiteSpace(request.WeixinId)
            ? _accountSettingProvider.GetByWeixinId(request.WeixinId)
            : null;

        var token = !string.IsNullOrWhiteSpace(request.Token) ? request.Token : config?.Token;
        if (string.IsNullOrWhiteSpace(token))
            return Failed<object>("未提供 Token，且无法从 WxConfig 中获取");

        var expectedSignature = BuildSignature(token, request.Timestamp, request.Nonce);
        return Success<object>(new
        {
            request.WeixinId,
            request.Timestamp,
            request.Nonce,
            RequestSignature = request.Signature,
            ExpectedSignature = expectedSignature,
            Matched = string.Equals(expectedSignature, request.Signature, StringComparison.OrdinalIgnoreCase)
        }, "签名校验完成");
    }

    [HttpGet("ReplyRules/{weixinId}")]
    public ServiceResult<object> ReplyRules(string weixinId)
    {
        var config = _accountSettingProvider.GetByWeixinId(weixinId);
        if (config == null)
            return Failed<object>($"未找到微信配置：{weixinId}");

        var profile = _replyRuleProvider.GetProfile(weixinId);
        return Success<object>(new
        {
            config.WeixinId,
            config.WeixinName,
            profile.TextReplies,
            profile.MenuEventReplies,
            profile.MenuSelectionReplies,
            profile.NewsReplies
        }, "回复规则读取成功");
    }

    [HttpPost("TestReply")]
    public ServiceResult<object> TestReply([FromBody] WxReplyRuleTestRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId))
            return Failed<object>("weixinId 不能为空");

        var config = _accountSettingProvider.GetByWeixinId(request.WeixinId);
        if (config == null)
            return Failed<object>($"未找到微信配置：{request.WeixinId}");

        var profile = _replyRuleProvider.GetProfile(request.WeixinId);
        var messageType = (request.MessageType ?? "text").Trim().ToLowerInvariant();
        var content = request.Content?.Trim() ?? string.Empty;
        var normalized = content.ToLowerInvariant();
        var eventKey = request.EventKey?.Trim() ?? string.Empty;

        if (messageType == "event" && profile.MenuEventReplies.TryGetValue(eventKey, out var eventReply))
        {
            return Success<object>(new
            {
                request.WeixinId,
                request.Channel,
                messageType,
                eventKey,
                Matched = true,
                ReplyType = "text",
                Reply = eventReply
            }, "规则匹配成功");
        }

        if (messageType == "text" &&
            profile.NewsReplies.TryGetValue(normalized, out var newsReply) &&
            newsReply != null &&
            (!string.IsNullOrWhiteSpace(newsReply.Title) || !string.IsNullOrWhiteSpace(newsReply.Url)))
        {
            return Success<object>(new
            {
                request.WeixinId,
                request.Channel,
                messageType,
                content,
                Matched = true,
                ReplyType = "news",
                Reply = newsReply
            }, "规则匹配成功");
        }

        if (messageType == "text")
        {
            var menuKey = content.StartsWith("s:", StringComparison.OrdinalIgnoreCase) ? content[2..] : content;
            if (profile.MenuSelectionReplies.TryGetValue(menuKey, out var menuReply))
            {
                return Success<object>(new
                {
                    request.WeixinId,
                    request.Channel,
                    messageType,
                    content,
                    Matched = true,
                    ReplyType = "text",
                    Reply = menuReply.Replace("{content}", content, StringComparison.OrdinalIgnoreCase)
                }, "规则匹配成功");
            }

            if (profile.TextReplies.TryGetValue(normalized, out var textReply))
            {
                return Success<object>(new
                {
                    request.WeixinId,
                    request.Channel,
                    messageType,
                    content,
                    Matched = true,
                    ReplyType = "text",
                    Reply = textReply
                }, "规则匹配成功");
            }
        }

        return Success<object>(new
        {
            request.WeixinId,
            request.Channel,
            messageType,
            content,
            eventKey,
            Matched = false,
            ReplyType = "fallback",
            Reply = "将进入默认回复逻辑"
        }, "未命中显式规则");
    }

    private string GetBaseUrl()
    {
        var configuredDomain = _configuration["Startup:Domain"];
        if (!string.IsNullOrWhiteSpace(configuredDomain))
            return configuredDomain.TrimEnd('/');

        return $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
    }

    private static string BuildSignature(string token, string timestamp, string nonce)
    {
        var values = new[] { token ?? string.Empty, timestamp ?? string.Empty, nonce ?? string.Empty };
        Array.Sort(values, StringComparer.Ordinal);
        var raw = string.Concat(values);
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool HasTenPayV3Configuration(SenparcWeixinSetting setting)
    {
        if (setting == null)
            return false;

        return !string.IsNullOrWhiteSpace(setting.TenPayV3_AppId)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_MchId)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_APIv3Key)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_PrivateKey)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_SerialNumber);
    }
}
