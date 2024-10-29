using System.ComponentModel.DataAnnotations;
using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 采购入库单明细
/// </summary>
public class InOrderDetailExtend : PoInOrderDetail
{
    /// <summary>
    /// 
    /// </summary>
    public int ROWNUM { get; set; }
    /// <summary>
    /// /
    /// </summary>
    public string MaterialNo { get; set; }

    /// <summary>
    /// 货品名称
    /// </summary>
    [Display(Name = "MaterialName")]
    public string MaterialName { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    [Display(Name = "Specifications")]
    public string Specifications { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    [Display(Name = "UnitId")]
    public Guid? UnitId { get; set; }
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public decimal MaxInQTY { get; set; }
}