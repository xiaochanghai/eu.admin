using System.ComponentModel.DataAnnotations;
using EU.Core.Model.Models;

namespace EU.Core.Model;

public class OrderDetailExtend : SdOrderDetail
{
    /// <summary>
    /// 材质编号
    /// </summary>
    [Display(Name = "MaterialNo")]
    public string MaterialNo { get; set; }

    /// <summary>
    /// 类型名称
    /// </summary>
    [Display(Name = "类型名称")]
    public string UnitName { get; set; }

    /// <summary>
    /// 订单占用数量
    /// </summary>
    [Display(Name = "OccupyQTY")]
    public decimal OccupyQTY { get; set; }

}