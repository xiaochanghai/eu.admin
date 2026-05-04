using Senparc.CO2NET;
using Senparc.Weixin.Entities.TemplateMessage;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;

namespace EU.Core.Extensions.Weixin.TemplateMessage;

/// <summary>
/// 系统异常告警通知模板消息
/// 模板字段：first、keyword1(Time)、keyword2(Host)、keyword3(Service)、keyword4(Status)、keyword5(Message)、remark
/// </summary>
public class WeixinTemplate_ExceptionAlert : TemplateMessageBase
{
    /// <summary>
    /// 默认模板 ID，实际使用时需在微信公众平台后台替换为自己的模板 ID。
    /// </summary>
    public const string DefaultTemplateId = "ur6TqESOo-32FEUk4qJxeWZZVt4KEOPjqbAFDGWw6gg";

    public TemplateDataItem first { get; set; }
    public TemplateDataItem keyword1 { get; set; }
    public TemplateDataItem keyword2 { get; set; }
    public TemplateDataItem keyword3 { get; set; }
    public TemplateDataItem keyword4 { get; set; }
    public TemplateDataItem keyword5 { get; set; }
    public TemplateDataItem remark { get; set; }

    public WeixinTemplate_ExceptionAlert(
        string firstMessage,
        string host,
        string service,
        string status,
        string message,
        string remarkMessage,
        string url = null,
        string templateId = DefaultTemplateId)
        : base(templateId, url, "系统异常告警通知")
    {
        first = new TemplateDataItem(firstMessage);
        keyword1 = new TemplateDataItem(SystemTime.Now.LocalDateTime.ToString());
        keyword2 = new TemplateDataItem(host);
        keyword3 = new TemplateDataItem(service);
        keyword4 = new TemplateDataItem(status);

        if (message.StartsWith("Padding is invalid"))
        {
            keyword5 = new TemplateDataItem(message, "#00dd00");
            remark = new TemplateDataItem("获取当前请求路径时发生异常，详见日志。");
        }
        else
        {
            keyword5 = new TemplateDataItem(message);
            remark = new TemplateDataItem(remarkMessage);
        }
    }
}
