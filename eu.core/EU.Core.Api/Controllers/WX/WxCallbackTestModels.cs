namespace EU.Core.Api.Controllers;

public class WxSignatureVerifyRequest
{
    public string WeixinId { get; set; }
    public string Token { get; set; }
    public string Timestamp { get; set; }
    public string Nonce { get; set; }
    public string Signature { get; set; }
}
