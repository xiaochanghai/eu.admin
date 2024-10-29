using System.ComponentModel.DataAnnotations;
using EU.Core.Model.Models;

namespace EU.Core.Model;

public class IvTransfersDetailExtend : IvTransfersDetail
{

    public string OutStockName { get; set; }

    public string OutGoodsLocationName { get; set; }

    public string InStockName { get; set; }

    public string InGoodsLocationName { get; set; }

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
    /// 单位ID
    /// </summary>
    [Display(Name = "UnitId")]
    public Guid? UnitId { get; set; }
}
