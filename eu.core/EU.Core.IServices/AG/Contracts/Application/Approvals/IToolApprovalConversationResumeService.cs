using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.Approvals;

/// <summary>在审批完成后恢复并投影对应的会话运行。</summary>
public interface IToolApprovalConversationResumeService
{
    #region 恢复会话（ResumeAsync）
    /// <summary>校验审批归属后恢复会话；不可恢复的状态抛出审批异常。</summary>
    /// <param name="approvalId">工具审批标识。</param>
    /// <param name="requester">当前调用方身份，包含用户、租户和权限。</param>
    /// <param name="cancellationToken">取消本次恢复的令牌。</param>
    /// <returns>会话运行状态、公开输出与错误码。</returns>
    Task<ToolApprovalConversationResumeResult> ResumeAsync(Guid approvalId, AgentExecutionIdentity requester, CancellationToken cancellationToken = default);
    #endregion
}
