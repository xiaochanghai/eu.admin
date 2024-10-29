using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 应收预付单明细
/// </summary>
public class ApPrepaidDetailExtend : ApPrepaidDetail
{
    /// <summary>
    /// 预付金额
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// 预付比例
    /// </summary>
    public decimal? Percent { get; set; }
    
    /// <summary>
    /// 预付金额
    /// </summary>
    public decimal? MaxPaymentAmount { get; set; }

    /// <summary>
    /// 含税金额
    /// </summary>
    public decimal? TaxIncludedAmount { get; set; }

}
