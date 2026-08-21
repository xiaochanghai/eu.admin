#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

namespace EU.Core.IServices.Approvals;

public sealed record ToolApprovalConversationResumeResult(
    Guid ApprovalId,
    Guid EntryRunId,
    Guid ConversationId,
    UnifiedRunStatus Status,
    string Content,
    string ErrorCode);
