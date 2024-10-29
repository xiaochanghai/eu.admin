using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 采购单明细
/// </summary>
public class POOrderDetailExtend : PoOrderDetail
{

    /// <summary>
    /// 物料名称
    /// </summary>
    public string MaterialNo { get; set; }
    /// <summary>
    /// 物料名称
    /// </summary>
    public string MaterialName { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    public string Specifications { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Description { get; set; }


    public string UnitName { get; set; }


    public decimal QTY { get; set; }
    public decimal MaxPurchaseQTY { get; set; }

}