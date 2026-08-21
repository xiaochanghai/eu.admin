using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.Agent.Application.Runtime;
using EU.Core.Agent.Application.UnifiedEntry;

namespace EU.Core.Agent.Application.Approvals;

public sealed record ToolApprovalConversationResumeResult(
    Guid ApprovalId,
    Guid EntryRunId,
    Guid ConversationId,
    UnifiedRunStatus Status,
    string Content,
    string ErrorCode);
