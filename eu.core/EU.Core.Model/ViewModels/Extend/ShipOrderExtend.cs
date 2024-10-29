using EU.Core.Model.Models;

namespace EU.Core.Model;

public class ShipOrderExtend
{
    public Guid? ID { get; set; }
    public Guid? OrderId { get; set; }

    public string OrderNo { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int SerialNumber { get; set; }

    public Guid? MaterialId { get; set; }
    public string MaterialNo { get; set; }

    public string MaterialName { get; set; }

    public string MaterialSpecifications { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitName { get; set; }
    public decimal QTY { get; set; }
    public decimal WaitQTY { get; set; }
    public decimal ShipQTY { get; set; }

    public decimal OutQTY { get; set; }
    /// <summary>
    /// 单价
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 未税金额
    /// </summary>
    public decimal NoTaxAmount { get; set; }

    /// <summary>
    /// 税额
    /// </summary>
    public decimal TaxAmount { get; set; }
    /// <summary>
    /// 含税金额
    /// </summary>
    public decimal TaxIncludedAmount { get; set; }

    public string CustomerMaterialCode { get; set; }
    public DateTime? DeliveryrDate { get; set; }

    public Guid? ShipOrderId { get; set; }
    public Guid? ShipOrderDetailId { get; set; }
    public Guid? StockId { get; set; }
    public Guid? GoodsLocationId { get; set; }

    public string StockName { get; set; }
    public string GoodsLocationName { get; set; }
    public int DecimalPlaces { get; set; }
    public decimal Step { get; set; }
    public decimal Min { get; set; }

    public string OrderSource { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesOrderDetailId { get; set; }
    public decimal InventoryQTY { get; set; }

    public List<BdStock> StockList { get; set; }
    public List<BdGoodsLocation> GoodsLocationList { get; set; }
}
