using EU.Core.Common;
using EU.Core.Extensions.Weixin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.TenPay.V3;
using Senparc.Weixin.Work.AdvancedAPIs;
using Senparc.Weixin.Work.Containers;
using Senparc.Weixin.WxOpen.AdvancedAPIs.Sns;
using MpJSSDKHelper = Senparc.Weixin.MP.Helpers.JSSDKHelper;
using WorkJSSDKHelper = Senparc.Weixin.Work.Helpers.JSSDKHelper;

namespace EU.Core.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = Grouping.GroupName_WX)]
public class WeixinPortalController : ControllerBase
{
    private readonly WxAccountSettingProvider _accountSettingProvider;
    private readonly SenparcAccountRegistrationService _registrationService;
    private readonly SenparcWeixinSetting _senparcWeixinSetting;
    private readonly IConfiguration _configuration;
    private readonly ISmUsersServices _smUsersServices;
    private readonly WeixinUserBindingService _bindingService;

    public WeixinPortalController(
        WxAccountSettingProvider accountSettingProvider,
        SenparcAccountRegistrationService registrationService,
        IOptions<SenparcWeixinSetting> senparcWeixinSetting,
        IConfiguration configuration,
        ISmUsersServices smUsersServices,
        WeixinUserBindingService bindingService)
    {
        _accountSettingProvider = accountSettingProvider;
        _registrationService = registrationService;
        _senparcWeixinSetting = senparcWeixinSetting.Value;
        _configuration = configuration;
        _smUsersServices = smUsersServices;
        _bindingService = bindingService;
    }

    [HttpPost("MpOAuthAuthorize")]
    [AllowAnonymous]
    public IActionResult MpOAuthAuthorize([FromBody] WeixinOAuthAuthorizeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.RedirectUrl))
        {
            return BadRequest(new
            {
                message = "weixinId 和 redirectUrl 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetMpAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinAppId) || string.IsNullOrWhiteSpace(setting.WeixinAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的公众号配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureMpRegistered(setting, $"MP:{request.WeixinId}");

        var scope = ParseScope(request.Scope);
        var authorizeUrl = OAuthApi.GetAuthorizeUrl(
            setting.WeixinAppId,
            request.RedirectUrl,
            request.State ?? string.Empty,
            scope,
            "code",
            true,
            request.ForcePopup);

        return Ok(new
        {
            request.WeixinId,
            Scope = scope.ToString(),
            AuthorizeUrl = authorizeUrl
        });
    }

    [HttpGet("MpOAuthCallback")]
    [AllowAnonymous]
    public async Task<IActionResult> MpOAuthCallback(
        [FromQuery] string weixinId,
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string scope = "snsapi_base")
    {
        if (string.IsNullOrWhiteSpace(weixinId) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new
            {
                message = "weixinId 和 code 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetMpAccountSetting(weixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinAppId) || string.IsNullOrWhiteSpace(setting.WeixinAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的公众号配置：{weixinId}"
            });
        }

        _registrationService.EnsureMpRegistered(setting, $"MP:{weixinId}");

        var oauthResult = await OAuthApi.GetAccessTokenAsync(
            setting.WeixinAppId,
            setting.WeixinAppSecret,
            code,
            "authorization_code");

        object userInfo = null;
        if (ParseScope(scope) == OAuthScope.snsapi_userinfo)
        {
            var info = await OAuthApi.GetUserInfoAsync(oauthResult.access_token, oauthResult.openid, Language.zh_CN);
            userInfo = new
            {
                info.openid,
                info.nickname,
                info.sex,
                info.country,
                info.province,
                info.city,
                info.headimgurl,
                info.unionid
            };
        }

        return Ok(new
        {
            WeixinId = weixinId,
            state,
            Scope = ParseScope(scope).ToString(),
            oauthResult.openid,
            oauthResult.unionid,
            oauthResult.access_token,
            oauthResult.refresh_token,
            oauthResult.expires_in,
            UserInfo = userInfo
        });
    }

    [HttpPost("MpOAuthLogin")]
    [AllowAnonymous]
    public async Task<IActionResult> MpOAuthLogin([FromBody] WeixinMpOAuthLoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new
            {
                message = "weixinId 和 code 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetMpAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinAppId) || string.IsNullOrWhiteSpace(setting.WeixinAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的公众号配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureMpRegistered(setting, $"MP:{request.WeixinId}");

        var oauthResult = await OAuthApi.GetAccessTokenAsync(
            setting.WeixinAppId,
            setting.WeixinAppSecret,
            request.Code,
            "authorization_code");
        var binding = await _bindingService.FindBindingAsync("mp", request.WeixinId, oauthResult.openid, oauthResult.unionid);
        if (binding?.UserId == null || binding.UserId == Guid.Empty)
        {
            return NotFound(new
            {
                request.WeixinId,
                Channel = "mp",
                oauthResult.openid,
                oauthResult.unionid,
                Bound = false,
                message = "当前公众号账号未绑定系统用户"
            });
        }

        await _bindingService.TouchLoginAsync(binding.ID);
        var loginResult = await _smUsersServices.LoginByUserIdAsync(binding.UserId.Value, "Mp");
        if (!loginResult.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                request.WeixinId,
                Channel = "mp",
                oauthResult.openid,
                oauthResult.unionid,
                Bound = true,
                loginResult.Message
            });
        }

        return Ok(new
        {
            request.WeixinId,
            Channel = "mp",
            oauthResult.openid,
            oauthResult.unionid,
            Bound = true,
            Login = loginResult.Data
        });
    }

    [Authorize]
    [HttpPost("MpBindCurrentUser")]
    public async Task<IActionResult> MpBindCurrentUser([FromBody] WeixinMpOAuthLoginRequest request)
    {
        if (App.User?.ID == null || App.User.ID == Guid.Empty)
        {
            return Unauthorized(new
            {
                message = "当前用户未登录"
            });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new
            {
                message = "weixinId 和 code 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetMpAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinAppId) || string.IsNullOrWhiteSpace(setting.WeixinAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的公众号配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureMpRegistered(setting, $"MP:{request.WeixinId}");

        var oauthResult = await OAuthApi.GetAccessTokenAsync(
            setting.WeixinAppId,
            setting.WeixinAppSecret,
            request.Code,
            "authorization_code");
        var binding = await _bindingService.BindCurrentUserAsync(
            "mp",
            request.WeixinId,
            setting.WeixinAppId,
            oauthResult.openid,
            oauthResult.unionid,
            null,
            "公众号登录绑定");

        return Ok(new
        {
            request.WeixinId,
            Channel = "mp",
            UserId = binding.UserId,
            binding.OpenId,
            binding.UnionId,
            binding.BindTime
        });
    }

    [HttpPost("MpJsSdkConfig")]
    [AllowAnonymous]
    public async Task<IActionResult> MpJsSdkConfig([FromBody] WeixinJsSdkRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new
            {
                message = "weixinId 和 url 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetMpAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinAppId) || string.IsNullOrWhiteSpace(setting.WeixinAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的公众号配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureMpRegistered(setting, $"MP:{request.WeixinId}");

        var package = await MpJSSDKHelper.GetJsSdkUiPackageAsync(setting.WeixinAppId, setting.WeixinAppSecret, request.Url);
        return Ok(new
        {
            request.WeixinId,
            request.Url,
            package.AppId,
            package.Timestamp,
            package.NonceStr,
            package.Signature
        });
    }

    [HttpPost("WxOpenCode2Session")]
    [AllowAnonymous]
    public async Task<IActionResult> WxOpenCode2Session([FromBody] WeixinWxOpenCode2SessionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.JsCode))
        {
            return BadRequest(new
            {
                message = "weixinId 和 jsCode 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetWxOpenAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WxOpenAppId) || string.IsNullOrWhiteSpace(setting.WxOpenAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的小程序配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureWxOpenRegistered(setting, $"WxOpen:{request.WeixinId}");

        var sessionResult = await SnsApi.JsCode2JsonAsync(
            setting.WxOpenAppId,
            setting.WxOpenAppSecret,
            request.JsCode,
            "authorization_code",
            10000);

        return Ok(new
        {
            request.WeixinId,
            sessionResult.openid,
            sessionResult.unionid,
            sessionResult.session_key,
            sessionResult.errcode,
            sessionResult.errmsg
        });
    }

    [HttpPost("WxOpenLogin")]
    [AllowAnonymous]
    public async Task<IActionResult> WxOpenLogin([FromBody] WeixinWxOpenCode2SessionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.JsCode))
        {
            return BadRequest(new
            {
                message = "weixinId 和 jsCode 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetWxOpenAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WxOpenAppId) || string.IsNullOrWhiteSpace(setting.WxOpenAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的小程序配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureWxOpenRegistered(setting, $"WxOpen:{request.WeixinId}");

        var sessionResult = await SnsApi.JsCode2JsonAsync(
            setting.WxOpenAppId,
            setting.WxOpenAppSecret,
            request.JsCode,
            "authorization_code",
            10000);
        var binding = await _bindingService.FindBindingAsync("wxopen", request.WeixinId, sessionResult.openid, sessionResult.unionid);
        if (binding?.UserId == null || binding.UserId == Guid.Empty)
        {
            return NotFound(new
            {
                request.WeixinId,
                Channel = "wxopen",
                sessionResult.openid,
                sessionResult.unionid,
                Bound = false,
                message = "当前小程序账号未绑定系统用户"
            });
        }

        await _bindingService.TouchLoginAsync(binding.ID);
        var loginResult = await _smUsersServices.LoginByUserIdAsync(binding.UserId.Value, "WxOpen");
        if (!loginResult.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                request.WeixinId,
                Channel = "wxopen",
                sessionResult.openid,
                sessionResult.unionid,
                Bound = true,
                loginResult.Message
            });
        }

        return Ok(new
        {
            request.WeixinId,
            Channel = "wxopen",
            sessionResult.openid,
            sessionResult.unionid,
            Bound = true,
            Login = loginResult.Data
        });
    }

    [Authorize]
    [HttpPost("WxOpenBindCurrentUser")]
    public async Task<IActionResult> WxOpenBindCurrentUser([FromBody] WeixinWxOpenCode2SessionRequest request)
    {
        if (App.User?.ID == null || App.User.ID == Guid.Empty)
        {
            return Unauthorized(new
            {
                message = "当前用户未登录"
            });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.JsCode))
        {
            return BadRequest(new
            {
                message = "weixinId 和 jsCode 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetWxOpenAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WxOpenAppId) || string.IsNullOrWhiteSpace(setting.WxOpenAppSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的小程序配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureWxOpenRegistered(setting, $"WxOpen:{request.WeixinId}");

        var sessionResult = await SnsApi.JsCode2JsonAsync(
            setting.WxOpenAppId,
            setting.WxOpenAppSecret,
            request.JsCode,
            "authorization_code",
            10000);
        var binding = await _bindingService.BindCurrentUserAsync(
            "wxopen",
            request.WeixinId,
            setting.WxOpenAppId,
            sessionResult.openid,
            sessionResult.unionid,
            null,
            "小程序登录绑定");

        return Ok(new
        {
            request.WeixinId,
            Channel = "wxopen",
            UserId = binding.UserId,
            binding.OpenId,
            binding.UnionId,
            binding.BindTime
        });
    }

    [HttpPost("WorkOAuthAuthorize")]
    [AllowAnonymous]
    public IActionResult WorkOAuthAuthorize([FromBody] WeixinWorkOAuthAuthorizeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.RedirectUrl))
        {
            return BadRequest(new
            {
                message = "weixinId 和 redirectUrl 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetWorkAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinCorpId))
        {
            return BadRequest(new
            {
                message = $"未找到可用的企业微信配置：{request.WeixinId}"
            });
        }

        var agentId = string.IsNullOrWhiteSpace(request.AgentId)
            ? setting.WeixinCorpAgentId
            : request.AgentId.Trim();
        var authorizeUrl = OAuth2Api.GetCode(
            setting.WeixinCorpId,
            request.RedirectUrl,
            request.State ?? string.Empty,
            agentId,
            string.IsNullOrWhiteSpace(request.ResponseType) ? "code" : request.ResponseType.Trim(),
            ParseWorkScope(request.Scope));

        return Ok(new
        {
            request.WeixinId,
            Scope = ParseWorkScope(request.Scope),
            AgentId = agentId,
            AuthorizeUrl = authorizeUrl
        });
    }

    [HttpGet("WorkOAuthCallback")]
    [AllowAnonymous]
    public async Task<IActionResult> WorkOAuthCallback(
        [FromQuery] string weixinId,
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string scope = "snsapi_base",
        [FromQuery] string agentId = null)
    {
        if (string.IsNullOrWhiteSpace(weixinId) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new
            {
                message = "weixinId 和 code 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetWorkAccountSetting(weixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinCorpId) ||
            string.IsNullOrWhiteSpace(setting.WeixinCorpSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的企业微信配置：{weixinId}"
            });
        }

        _registrationService.EnsureWorkRegistered(setting, $"Work:{weixinId}");

        var currentAgentId = string.IsNullOrWhiteSpace(agentId)
            ? setting.WeixinCorpAgentId
            : agentId.Trim();
        var accessToken = await AccessTokenContainer.GetTokenAsync(setting.WeixinCorpId, setting.WeixinCorpSecret, false);
        var oauthResult = await OAuth2Api.GetUserIdAsync(accessToken, code);

        object userDetail = null;
        if (!string.IsNullOrWhiteSpace(oauthResult.user_ticket))
        {
            var detail = await OAuth2Api.GetUserDetailAsync(accessToken, oauthResult.user_ticket);
            userDetail = new
            {
                detail.userid,
                detail.name,
                detail.position,
                detail.mobile,
                detail.email,
                detail.avatar,
                detail.gender,
                detail.department
            };
        }

        return Ok(new
        {
            WeixinId = weixinId,
            state,
            Scope = ParseWorkScope(scope),
            AgentId = currentAgentId,
            oauthResult.UserId,
            oauthResult.OpenId,
            oauthResult.CorpId,
            oauthResult.DeviceId,
            oauthResult.user_ticket,
            oauthResult.expires_in,
            oauthResult.external_userid,
            UserDetail = userDetail
        });
    }

    [HttpPost("WorkJsSdkConfig")]
    [AllowAnonymous]
    public async Task<IActionResult> WorkJsSdkConfig([FromBody] WeixinWorkJsSdkRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new
            {
                message = "weixinId 和 url 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetWorkAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinCorpId) ||
            string.IsNullOrWhiteSpace(setting.WeixinCorpSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的企业微信配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureWorkRegistered(setting, $"Work:{request.WeixinId}");

        var jsApiTicket = await JsApiTicketContainer.GetTicketAsync(
            setting.WeixinCorpId,
            setting.WeixinCorpSecret,
            request.IsAgentConfig,
            request.GetNewTicket);
        var package = await WorkJSSDKHelper.GetJsApiUiPackageAsync(
            setting.WeixinCorpId,
            setting.WeixinCorpSecret,
            request.Url,
            jsApiTicket,
            request.IsAgentConfig);

        return Ok(new
        {
            request.WeixinId,
            request.Url,
            request.IsAgentConfig,
            AgentId = setting.WeixinCorpAgentId,
            package.AppId,
            package.Timestamp,
            package.NonceStr,
            package.Signature
        });
    }

    [HttpPost("WorkOAuthLogin")]
    [AllowAnonymous]
    public async Task<IActionResult> WorkOAuthLogin([FromBody] WeixinWorkOAuthLoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WeixinId) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new
            {
                message = "weixinId 和 code 不能为空"
            });
        }

        var setting = _accountSettingProvider.GetWorkAccountSetting(request.WeixinId);
        if (string.IsNullOrWhiteSpace(setting?.WeixinCorpId) ||
            string.IsNullOrWhiteSpace(setting.WeixinCorpSecret))
        {
            return BadRequest(new
            {
                message = $"未找到可用的企业微信配置：{request.WeixinId}"
            });
        }

        _registrationService.EnsureWorkRegistered(setting, $"Work:{request.WeixinId}");

        var accessToken = await AccessTokenContainer.GetTokenAsync(setting.WeixinCorpId, setting.WeixinCorpSecret, false);
        var oauthResult = await OAuth2Api.GetUserIdAsync(accessToken, request.Code);

        string mobile = null;
        object userDetail = null;
        if (!string.IsNullOrWhiteSpace(oauthResult.user_ticket))
        {
            var detail = await OAuth2Api.GetUserDetailAsync(accessToken, oauthResult.user_ticket);
            mobile = detail.mobile;
            userDetail = new
            {
                detail.userid,
                detail.name,
                detail.position,
                detail.mobile,
                detail.email,
                detail.avatar,
                detail.gender,
                detail.department
            };
        }

        var loginResult = await _smUsersServices.LoginByWeComIdentityAsync(
            oauthResult.UserId,
            mobile,
            "WeCom");
        if (!loginResult.Success)
        {
            return NotFound(new
            {
                request.WeixinId,
                oauthResult.UserId,
                oauthResult.OpenId,
                Mobile = mobile,
                loginResult.Message,
                UserDetail = userDetail
            });
        }

        return Ok(new
        {
            request.WeixinId,
            oauthResult.UserId,
            oauthResult.OpenId,
            Mobile = mobile,
            UserDetail = userDetail,
            Login = loginResult.Data
        });
    }

    [HttpPost("CreateMpJsApiOrder")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateMpJsApiOrder([FromBody] WeixinJsApiPayRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.OpenId) ||
            string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.OutTradeNo) ||
            request.AmountFen <= 0)
        {
            return BadRequest(new
            {
                message = "OpenId、Description、OutTradeNo 必填，AmountFen 必须大于 0"
            });
        }

        if (!HasTenPayV3Configuration(_senparcWeixinSetting))
        {
            return BadRequest(new
            {
                message = "SenparcWeixinSetting 未完成 TenPay V3 配置"
            });
        }

        var notifyUrl = string.IsNullOrWhiteSpace(request.NotifyUrl)
            ? BuildDefaultNotifyUrl()
            : request.NotifyUrl.Trim();

        var unifiedorderRequest = new TenPayV3UnifiedorderRequestData
        {
            AppId = _senparcWeixinSetting.TenPayV3_AppId,
            MchId = _senparcWeixinSetting.TenPayV3_MchId,
            Body = request.Description,
            OutTradeNo = request.OutTradeNo,
            TotalFee = request.AmountFen,
            SpbillCreateIP = GetClientIp(),
            NotifyUrl = notifyUrl,
            TradeType = Senparc.Weixin.TenPay.TenPayV3Type.JSAPI,
            OpenId = request.OpenId,
            Attach = request.Attach,
            TimeStart = DateTime.Now.ToString("yyyyMMddHHmmss"),
            TimeExpire = DateTime.Now.AddMinutes(30).ToString("yyyyMMddHHmmss")
        };

        var result = await TenPayV3.UnifiedorderAsync(unifiedorderRequest);
        if (result == null || result.return_code != "SUCCESS" || result.result_code != "SUCCESS" || string.IsNullOrWhiteSpace(result.prepay_id))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "微信支付统一下单失败",
                result?.return_code,
                result?.return_msg,
                result?.result_code,
                result?.err_code,
                result?.err_code_des
            });
        }

        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonceStr = Guid.NewGuid().ToString("N");
        var packageValue = $"prepay_id={result.prepay_id}";
        var signType = "MD5";
        var paySign = TenPayV3.GetJsPaySign(
            _senparcWeixinSetting.TenPayV3_AppId,
            timeStamp,
            nonceStr,
            packageValue,
            signType,
            _senparcWeixinSetting.TenPayV3_Key);

        return Ok(new
        {
            request.WeixinId,
            request.OpenId,
            request.OutTradeNo,
            request.AmountFen,
            prepayId = result.prepay_id,
            appId = _senparcWeixinSetting.TenPayV3_AppId,
            timeStamp,
            nonceStr,
            packageValue,
            signType,
            paySign,
            notifyUrl
        });
    }

    private string BuildDefaultNotifyUrl()
    {
        var configured = _senparcWeixinSetting.TenPayV3_TenpayNotify;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return $"{GetBaseUrl()}/TenpayApiV3/PayNotifyUrl";
    }

    private string GetBaseUrl()
    {
        var configuredDomain = _configuration["Startup:Domain"];
        if (!string.IsNullOrWhiteSpace(configuredDomain))
        {
            return configuredDomain.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
    }

    private string GetClientIp()
    {
        var xForwardedFor = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xForwardedFor))
        {
            return xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "127.0.0.1";
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    private static OAuthScope ParseScope(string scope)
    {
        return string.Equals(scope, "snsapi_userinfo", StringComparison.OrdinalIgnoreCase)
            ? OAuthScope.snsapi_userinfo
            : OAuthScope.snsapi_base;
    }

    private static string ParseWorkScope(string scope)
    {
        return string.Equals(scope, "snsapi_privateinfo", StringComparison.OrdinalIgnoreCase)
            ? "snsapi_privateinfo"
            : "snsapi_base";
    }

    private static bool HasTenPayV3Configuration(SenparcWeixinSetting setting)
    {
        if (setting == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(setting.TenPayV3_AppId)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_MchId)
            && !string.IsNullOrWhiteSpace(setting.TenPayV3_Key);
    }
}
