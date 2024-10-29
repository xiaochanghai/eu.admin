using EU.Core.Model.Models;

namespace EU.Core.Model;



/// <summary>
/// 采购到货通知单明细
/// </summary>
public class ArrivalOrderDetailExtend : PoArrivalOrderDetail
{
    /// <summary>
    /// 货品名称
    /// </summary>
    public string MaterialName { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    public string Specifications { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// 单位ID
    /// </summary>
    public Guid? UnitId { get; set; }

    public decimal MaxArrivalQTY { get; set; }

}