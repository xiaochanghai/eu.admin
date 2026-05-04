namespace EU.Core.Api.Controllers;

public class WeixinOAuthAuthorizeRequest
{
    public string WeixinId { get; set; }

    public string RedirectUrl { get; set; }

    public string State { get; set; }

    public string Scope { get; set; }

    public bool ForcePopup { get; set; }
}

public class WeixinJsSdkRequest
{
    public string WeixinId { get; set; }

    public string Url { get; set; }
}

public class WeixinJsApiPayRequest
{
    public string WeixinId { get; set; }

    public string OpenId { get; set; }

    public string Description { get; set; }

    public string OutTradeNo { get; set; }

    public int AmountFen { get; set; }

    public string Attach { get; set; }

    public string NotifyUrl { get; set; }
}

public class WeixinWxOpenCode2SessionRequest
{
    public string WeixinId { get; set; }

    public string JsCode { get; set; }
}

public class WeixinWorkOAuthAuthorizeRequest
{
    public string WeixinId { get; set; }

    public string RedirectUrl { get; set; }

    public string State { get; set; }

    public string Scope { get; set; }

    public string AgentId { get; set; }

    public string ResponseType { get; set; }
}

public class WeixinWorkJsSdkRequest
{
    public string WeixinId { get; set; }

    public string Url { get; set; }

    public bool IsAgentConfig { get; set; }

    public bool GetNewTicket { get; set; }
}

public class WeixinWorkOAuthLoginRequest
{
    public string WeixinId { get; set; }

    public string Code { get; set; }

    public string AgentId { get; set; }
}

public class WeixinMpOAuthLoginRequest
{
    public string WeixinId { get; set; }

    public string Code { get; set; }
}
