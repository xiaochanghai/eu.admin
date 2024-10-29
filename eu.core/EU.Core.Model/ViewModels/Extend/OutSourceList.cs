using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 
/// </summary>
public class OutSourceList : PdOutDetail
{
    public decimal OrderQTY { get; set; }

    public string SourceOrderNo { get; set; }

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
    /// 
    /// </summary>
    public string TextureName { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// 配方
    /// </summary>
    public string Formula { get; set; }

    public string ShouldMaterialNo { get; set; }
    public string ShouldMaterialName { get; set; }
    public string ShouldSpecifications { get; set; }
    public string ShouldUnitName { get; set; }

}