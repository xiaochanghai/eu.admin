#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Approvals;

/// <summary>
/// 审批完成后恢复会话的结果。
/// </summary>
/// <param name="ApprovalId">工具调用审批标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="Status">当前状态。</param>
/// <param name="Content">恢复会话后产生的内容。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record ToolApprovalConversationResumeResult(
    Guid ApprovalId,
    Guid EntryRunId,
    Guid ConversationId,
    UnifiedRunStatus Status,
    string Content,
    string ErrorCode);
