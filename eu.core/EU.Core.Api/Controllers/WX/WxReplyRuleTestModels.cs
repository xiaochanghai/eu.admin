namespace EU.Core.Api.Controllers;

public class WxReplyRuleTestRequest
{
    public string WeixinId { get; set; }

    public string Channel { get; set; }

    public string MessageType { get; set; } = "text";

    public string Content { get; set; }

    public string EventKey { get; set; }
}
