using EU.Core.Model.Models;

namespace EU.Core.Model;

public class OrderSourceList : PdOrder
{
    public int SerialNumber { get; set; }

    public decimal MaxQTY { get; set; }
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
    /// 配方
    /// </summary>
    public string Formula { get; set; }


}
