namespace EU.Core.Model;

public class OrderExtend
{
    public Guid? ID { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string OrderNo { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int SerialNumber { get; set; }

    public Guid? MaterialId { get; set; }
    public string MaterialNo { get; set; }

    public string MaterialName { get; set; }

    public string MaterialSpecifications { get; set; }
    public string UnitNames { get; set; }
    public decimal OrderQTY { get; set; }
    public decimal QTY { get; set; }
    public string CustomerMaterialCode { get; set; }
    public DateTime? DeliveryrDate { get; set; }

    public Guid? ShipOrderId { get; set; }
    public decimal ShipQTY { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? StockId { get; set; }
    public Guid? GoodsLocationId { get; set; }
    public int DecimalPlaces { get; set; }
    public decimal Step { get; set; }
    public decimal Min { get; set; }
}