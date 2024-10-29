using EU.Core.Model.Models;

namespace EU.Core.Model;

public class CompleteSourceList : PdCompleteDetail
{

    public string OrderNo { get; set; }

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

    public decimal PdQTY { get; set; }

    public decimal ActualQTY { get; set; }

    public string LinkOrderNo { get; set; }

}