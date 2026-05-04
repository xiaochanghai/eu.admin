using Senparc.Weixin.Entities.TemplateMessage;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;

namespace EU.Core.Extensions.Weixin.TemplateMessage;

/// <summary>
/// 购买成功通知模板消息（公众号）
/// </summary>
public class WeixinTemplate_PaySuccess : TemplateMessageBase
{
    /// <summary>
    /// 默认模板 ID，实际使用时需在微信公众平台后台替换为自己的模板 ID。
    /// </summary>
    public const string DefaultTemplateId = "66Gf81swxfWt_P_HkH0Bapvj1nlpiWGmEkXDeCvWcVo";

    public TemplateDataItem name { get; set; }
    public TemplateDataItem remark { get; set; }

    public WeixinTemplate_PaySuccess(string url, string productName, string notice, string templateId = DefaultTemplateId)
        : base(templateId, url, "购买成功通知")
    {
        name = new TemplateDataItem(productName);
        remark = new TemplateDataItem(notice);
    }
}
