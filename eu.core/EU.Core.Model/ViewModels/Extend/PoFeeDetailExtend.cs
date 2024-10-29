using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 采购费用单明细
/// </summary>
public class PoFeeDetailExtend : PoFeeDetail
{
    /// <summary>
    /// 物料编号
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
    /// 单位
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

}