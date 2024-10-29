namespace EU.Core.Model;

public class ReissueSourceList
{
    public Guid? ID { get; set; }

    public DateTime CreatedTime { get; set; }

    public Guid? SourceOrderId { get; set; }

    public decimal OrderQTY { get; set; }

    public decimal QTY { get; set; }

    public string SourceOrderNo { get; set; }

    public Guid? MaterialId { get; set; }

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

    /// <summary>
    /// 配方
    /// </summary>
    public string Formula { get; set; }


}