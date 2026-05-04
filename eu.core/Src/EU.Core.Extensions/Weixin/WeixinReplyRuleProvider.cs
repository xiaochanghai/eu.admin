using Microsoft.Extensions.Options;

namespace EU.Core.Extensions.Weixin;

public class WeixinReplyRuleProvider
{
    private readonly WeixinReplyRuleOptions _options;

    public WeixinReplyRuleProvider(IOptions<WeixinReplyRuleOptions> options)
    {
        _options = options.Value ?? new WeixinReplyRuleOptions();
    }

    public WeixinReplyProfile GetProfile(string weixinId)
    {
        if (!string.IsNullOrWhiteSpace(weixinId) &&
            _options.Accounts.TryGetValue(weixinId, out var profile))
        {
            return profile ?? new WeixinReplyProfile();
        }

        if (_options.Accounts.TryGetValue("default", out var defaultProfile))
        {
            return defaultProfile ?? new WeixinReplyProfile();
        }

        return new WeixinReplyProfile();
    }
}
