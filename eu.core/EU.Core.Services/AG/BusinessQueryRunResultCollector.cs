using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

#nullable enable
namespace EU.Core.Services;

/// <summary>将主运行的受控 MCP 成功事件转换为可登记的业务结果，不接受模型正文作为证据。</summary>
public sealed class BusinessQueryRunResultCollector(BusinessQueryToolPolicy? policy, AgentRunContext context)
{
    private Guid? _callId;
    private Guid? _toolVersionId;
    private bool _completed;

    /// <summary>检查绑定、开始事件与回执；无关工具返回 null，非法业务证据抛出异常。</summary>
    public BusinessQueryAuthoritativeResult? Observe(AgentRunEvent value)
    {
        PublishedMcpToolReference? tool = context.Tools.FirstOrDefault(tool => tool.ToolVersionId == value.ToolVersionId);
        if (policy is null || tool is null || !policy.Matches(tool)
            || value.Kind is not (AgentRunEventKind.ToolStarted or AgentRunEventKind.ToolSucceeded))
            return null;

        if (value.RunId != context.RunId || value.ToolName != tool.ToolName || value.ToolCallId is not Guid callId || callId == Guid.Empty)
            throw InvalidEvidence();

        if (value.Kind == AgentRunEventKind.ToolStarted)
        {
            if (_callId.HasValue) throw new UnifiedEntryException(UnifiedEntryErrorCodes.BusinessQueryCallLimitExceeded,
                "The controlled business query tool may be called only once per run.");
            _callId = callId;
            _toolVersionId = tool.ToolVersionId;
            return null;
        }

        if (_completed || _callId != callId || _toolVersionId != tool.ToolVersionId
            || !BusinessQueryAuthoritativeResult.TryParse(value.Text, policy, out var result))
            throw InvalidEvidence();
        _completed = true;
        return result;
    }

    private static UnifiedEntryException InvalidEvidence() => new(UnifiedEntryErrorCodes.BusinessQueryEvidenceRequired,
        "The controlled business query did not produce valid server evidence.");
}
