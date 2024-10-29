using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 销售收款核销明细
/// </summary>
public class ArSalesCollectionWriteOffExtend : ArSalesCollectionWriteOff
{
    /// <summary>
    /// 收款金额
    /// </summary>
    public decimal? MaxReceivableAmount { get; set; }

}
