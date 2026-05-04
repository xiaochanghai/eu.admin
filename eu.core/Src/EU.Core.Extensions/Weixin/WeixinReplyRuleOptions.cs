namespace EU.Core.Extensions.Weixin;

public class WeixinReplyRuleOptions
{
    public Dictionary<string, WeixinReplyProfile> Accounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class WeixinReplyProfile
{
    public Dictionary<string, string> TextReplies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> MenuEventReplies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> MenuSelectionReplies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, WeixinNewsReply> NewsReplies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class WeixinNewsReply
{
    public string Title { get; set; }

    public string Description { get; set; }

    public string PicUrl { get; set; }

    public string Url { get; set; }
}
