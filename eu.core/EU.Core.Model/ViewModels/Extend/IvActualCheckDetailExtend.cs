using EU.Core.Model.Models;

namespace EU.Core.Model;

public class IvActualCheckDetailExtend : IvActualCheckDetail
{
    public string StockName { get; set; }
    public string GoodsLocationName { get; set; }
    public string MaterialNo { get; set; }
    public string MaterialName { get; set; }
}