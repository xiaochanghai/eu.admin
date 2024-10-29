using EU.Core.Model.Models;

namespace EU.Core.Model;

public class AnalysisList : PdRequireAnalysis
{
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
    /// 来源，销售单、生产计划单
    /// </summary>
    public string Specifications { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// 订单交期
    /// </summary>
    public string DeliveryrDate { get; set; }

    /// <summary>
    /// 配方
    /// </summary>
    public string Formula { get; set; }

    /// <summary>
    /// 客户
    /// </summary>
    public string CustomerName { get; set; }

}