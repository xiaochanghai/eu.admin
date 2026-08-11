namespace EU.Core.Agent.Api.Security;

public static class AgentAuthorizationPolicies
{
    public const string Admin = "AgentAdmin";
    public const string Debug = "AgentDebug";
    public const string Chat = "AgentChat";
    public const string AuditRead = "AgentAuditRead";
    public const string HistoryRead = "AgentHistoryRead";
    public const string ApprovalRead = "AgentApprovalRead";
    public const string ApprovalDecide = "AgentApprovalDecide";
    public const string ApprovalDecideHighRisk = "AgentApprovalDecideHighRisk";

    public const string AdminPermission = "agent.admin";
    public const string DebugPermission = "agent.debug";
    public const string ChatPermission = "agent.chat";
    public const string AuditReadPermission = "agent.audit.read";
    public const string BusinessDataReadPermission = "agent.business-data.read";
    public const string ApprovalReadPermission = "agent.approval.read";
    public const string ApprovalDecidePermission = "agent.approval.decide";
    public const string ApprovalDecideHighRiskPermission =
        "agent.approval.decide.high-risk";
}
