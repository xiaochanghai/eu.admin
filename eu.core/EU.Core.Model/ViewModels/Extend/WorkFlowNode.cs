namespace EU.Core.Model;

#region 自定义功能

public class WorkFlowNode
{
    public string id { get; set; }

    public string nodeType { get; set; }

    public string name { get; set; }

    public WorkFlowNode childNode { get; set; }
    public ApproverSettings approverSettings { get; set; }
    public List<WorkFlowNode> conditionNodeList { get; set; }
}

public class ApproverSettings
{
    public List<AuditList> auditList { get; set; }

}
public class AuditList
{
    public string userType { get; set; }
    public Guid objectId { get; set; }
    public string label { get; set; }
}
#endregion