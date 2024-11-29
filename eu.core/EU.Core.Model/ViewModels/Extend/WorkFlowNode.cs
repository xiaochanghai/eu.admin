namespace EU.Core.Model;

#region 自定义功能

public class WorkFlowNode
{
    public string id { get; set; }

    public string nodeType { get; set; }

    public string name { get; set; }

    public WorkFlowNode childNode { get; set; }
    public List<WorkFlowNode> conditionNodeList { get; set; }
}
#endregion