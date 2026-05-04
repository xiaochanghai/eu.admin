using Senparc.Weixin.Entities.TemplateMessage;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;

namespace EU.Core.Extensions.Weixin.TemplateMessage.WxOpen;

/// <summary>
/// 购买成功通知模板消息（小程序）
/// 模板字段：keyword1(购买地点)、keyword2(购买时间)、keyword3(物品名称)、keyword4(交易单号)、keyword5(购买价格)、keyword6(售后电话)
/// 注：2020-01-10 起新发布的小程序将不能使用模板消息，建议迁移至订阅消息。
/// </summary>
public class WxOpenTemplateMessage_PaySuccessNotice : TemplateMessageBase
{
    /// <summary>
    /// 默认模板 ID，实际使用时需在微信公众平台后台替换为自己的模板 ID。
    /// </summary>
    public const string DefaultTemplateId = "Ap1S3tRvsB8BXsWkiILLz93nhe7S8IgAipZDfygy9Bg";

    public TemplateDataItem keyword1 { get; set; }
    public TemplateDataItem keyword2 { get; set; }
    public TemplateDataItem keyword3 { get; set; }
    public TemplateDataItem keyword4 { get; set; }
    public TemplateDataItem keyword5 { get; set; }
    public TemplateDataItem keyword6 { get; set; }

    public WxOpenTemplateMessage_PaySuccessNotice(
        string payAddress,
        DateTimeOffset payTime,
        string productName,
        string orderNumber,
        decimal orderPrice,
        string hotLine,
        string url,
        string templateId = DefaultTemplateId)
        : base(templateId, url, "购买成功通知")
    {
        keyword1 = new TemplateDataItem(payAddress);
        keyword2 = new TemplateDataItem(payTime.LocalDateTime.ToString());
        keyword3 = new TemplateDataItem(productName);
        keyword4 = new TemplateDataItem(orderNumber);
        keyword5 = new TemplateDataItem(orderPrice.ToString("C"));
        keyword6 = new TemplateDataItem(hotLine);
    }
}
