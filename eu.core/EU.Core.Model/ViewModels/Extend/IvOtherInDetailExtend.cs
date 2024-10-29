using EU.Core.Model.Models;

namespace EU.Core.Model;

/// <summary>
/// 其他入库单明细
/// </summary>
public class IvOtherInDetailExtend : IvOtherInDetail
{
    /// <summary>
    /// 货品名称
    /// </summary>
    public string MaterialName { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    public string StockName { get; set; }

    /// <summary>
    /// 货位名称
    /// </summary>
    public string GoodsLocationName { get; set; }

}