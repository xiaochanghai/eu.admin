namespace EU.Core.Model.ViewModels;

public class SdOrderWaitShip
{
    public Guid ID { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string OrderNo { get; set; }
    public DateTime? CreatedTime { get; set; }
    public int? SerialNumber { get; set; }
    public Guid? MaterialId { get; set; }
    public string MaterialNo { get; set; }
    public string Specifications { get; set; }
    public string UnitName { get; set; }
    public decimal? OrderQTY { get; set; }
    public decimal? ShipQTYVV { get; set; }
    public decimal? QTY { get; set; }
    public string CustomerMaterialCode { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; }
}
