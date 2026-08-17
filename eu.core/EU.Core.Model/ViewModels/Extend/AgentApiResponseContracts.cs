#nullable enable

namespace EU.Core.Model.ViewModels.Extend;

/// <summary>
/// Agent API 失败响应中的稳定机器可读数据。
/// </summary>
public sealed record AgentApiErrorData(string ErrorCode, string TraceId);

/// <summary>
/// Agent API 错误码对应的业务状态和默认 HTTP 状态。
/// </summary>
/// <param name="Status">六位 Agent 业务状态码。</param>
/// <param name="HttpStatus">默认 HTTP 状态；运行记录或已开始的流式事件没有该状态。</param>
public sealed record AgentApiErrorDescriptor(int Status, int? HttpStatus);
