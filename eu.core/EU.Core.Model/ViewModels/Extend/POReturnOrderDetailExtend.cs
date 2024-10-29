using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 采购退货单明细
/// </summary>
public class POReturnOrderDetailExtend : PoReturnOrderDetail
{
    /// <summary>
    /// 
    /// </summary>
    public string MaterialNo { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string MaterialName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Specifications { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public decimal MaxReturnQTY { get; set; }
}
