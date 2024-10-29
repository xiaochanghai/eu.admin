using EU.Core.Model.Models;

namespace EU.Core.Model;

public class ReturnSourceList : PdReturnDetail
{
    public int? SourceSerialNumber { get; set; }

    public string PdOrderNo { get; set; }

    /// <summary>
    /// 物料编号
    /// </summary>
    public string PdMaterialNo { get; set; }
    /// <summary>
    /// 物料名称
    /// </summary>
    public string PdMaterialName { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    public string PdSpecifications { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string PdTextureName { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string PdUnitName { get; set; }

    /// <summary>
    /// 配方
    /// </summary>
    public string PdFormula { get; set; }


    public decimal OrderQTY { get; set; }

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

    public decimal ShouldQTY { get; set; }

    public decimal ActualQTY { get; set; }

    public string LinkOrderNo { get; set; }

}