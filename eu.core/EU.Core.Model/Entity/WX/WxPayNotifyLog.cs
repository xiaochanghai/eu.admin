namespace EU.Core.Model.Entity;

/// <summary>
/// 微信支付通知日志 (Model)
/// </summary>
[SugarTable("WxPayNotifyLog", "微信支付通知日志"), Entity(TableCnName = "微信支付通知日志", TableName = "WxPayNotifyLog")]
[SugarIndex("index_WxPayNotifyLog_Channel_NotifyId", nameof(Channel), OrderByType.Asc, nameof(NotifyId), OrderByType.Asc, true)]
[SugarIndex("index_WxPayNotifyLog_Channel_OutTradeNo", nameof(Channel), OrderByType.Asc, nameof(OutTradeNo), OrderByType.Asc)]
[SugarIndex("index_WxPayNotifyLog_Channel_TransactionId", nameof(Channel), OrderByType.Asc, nameof(TransactionId), OrderByType.Asc)]
public class WxPayNotifyLog : BasePoco
{
    [Display(Name = "Channel"), Description("渠道类型"), SugarColumn(IsNullable = true, Length = 32)]
    public string Channel { get; set; }

    [Display(Name = "NotifyId"), Description("微信通知ID"), SugarColumn(IsNullable = true, Length = 128)]
    public string NotifyId { get; set; }

    [Display(Name = "EventType"), Description("事件类型"), SugarColumn(IsNullable = true, Length = 64)]
    public string EventType { get; set; }

    [Display(Name = "OriginalType"), Description("资源类型"), SugarColumn(IsNullable = true, Length = 64)]
    public string OriginalType { get; set; }

    [Display(Name = "Serial"), Description("微信支付平台证书序列号"), SugarColumn(IsNullable = true, Length = 128)]
    public string Serial { get; set; }

    [Display(Name = "OutTradeNo"), Description("商户单号"), SugarColumn(IsNullable = true, Length = 64)]
    public string OutTradeNo { get; set; }

    [Display(Name = "TransactionId"), Description("微信支付订单号"), SugarColumn(IsNullable = true, Length = 64)]
    public string TransactionId { get; set; }

    [Display(Name = "TradeState"), Description("交易状态"), SugarColumn(IsNullable = true, Length = 32)]
    public string TradeState { get; set; }

    [Display(Name = "TradeStateDesc"), Description("交易状态描述"), SugarColumn(IsNullable = true, Length = 256)]
    public string TradeStateDesc { get; set; }

    [Display(Name = "TradeType"), Description("支付方式"), SugarColumn(IsNullable = true, Length = 32)]
    public string TradeType { get; set; }

    [Display(Name = "AppId"), Description("AppId"), SugarColumn(IsNullable = true, Length = 64)]
    public string AppId { get; set; }

    [Display(Name = "MchId"), Description("商户号"), SugarColumn(IsNullable = true, Length = 64)]
    public string MchId { get; set; }

    [Display(Name = "OpenId"), Description("支付用户OpenId"), SugarColumn(IsNullable = true, Length = 128)]
    public string OpenId { get; set; }

    [Display(Name = "AmountFen"), Description("订单总金额（分）"), SugarColumn(IsNullable = true)]
    public int? AmountFen { get; set; }

    [Display(Name = "PayerAmountFen"), Description("用户实际支付金额（分）"), SugarColumn(IsNullable = true)]
    public int? PayerAmountFen { get; set; }

    [Display(Name = "SuccessTime"), Description("支付成功时间"), SugarColumn(IsNullable = true)]
    public DateTime? SuccessTime { get; set; }

    [Display(Name = "Attach"), Description("附加数据"), SugarColumn(IsNullable = true, Length = 512)]
    public string Attach { get; set; }

    [Display(Name = "NotifyCount"), Description("通知次数"), SugarColumn(IsNullable = true, DefaultValue = "1")]
    public int? NotifyCount { get; set; } = 1;

    [Display(Name = "FirstNotifyTime"), Description("首次通知时间"), SugarColumn(IsNullable = true)]
    public DateTime? FirstNotifyTime { get; set; }

    [Display(Name = "LastNotifyTime"), Description("最近通知时间"), SugarColumn(IsNullable = true)]
    public DateTime? LastNotifyTime { get; set; }

    [Display(Name = "RawBody"), Description("原始回调报文"), SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string RawBody { get; set; }

    [Display(Name = "ResourceJson"), Description("解密后的资源报文"), SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
    public string ResourceJson { get; set; }

    [Display(Name = "Remark"), Description("处理备注"), SugarColumn(IsNullable = true, Length = 2000)]
    public string Remark { get; set; }
}
