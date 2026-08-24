import { AbstractChatProvider, XRequest } from "@ant-design/x-sdk";
import type { TransformMessage, XRequestOptions } from "@ant-design/x-sdk";
import { store } from "@/redux";

export interface UnifiedChatRunEvent {
  runId: string;
  conversationId: string;
  sequence: number;
  kind: string;
  occurredAtUtc: string;
  correlationId: string;
  parentRunId?: string | null;
  depth: number;
  payloadJson: string;
  route: string;
}

export interface StreamChatRunOptions {
  input: string;
  conversationId?: string;
  signal: AbortSignal;
  onOpen?: (metadata: { runId?: string; conversationId?: string }) => void;
  onEvent: (event: UnifiedChatRunEvent) => void;
}

export interface UnifiedChatSdkMessage {
  role: "user" | "assistant";
  content: string;
}

export interface UnifiedChatSdkRequest {
  input?: string;
  messages?: UnifiedChatSdkMessage[];
  conversationId?: string;
}

type UnifiedChatSdkFrame = { event?: string; data?: string };

export interface UnifiedChatConversation {
  id: string;
  title: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UnifiedChatMessage {
  id: string;
  conversationId: string;
  role: string | number;
  content: string;
  createdAtUtc: string;
}

export interface UnifiedChatConversationDetail {
  conversation: UnifiedChatConversation;
  messages: UnifiedChatMessage[];
}

export type AgentTaskStatus =
  | "Pending"
  | "Running"
  | "WaitingForApproval"
  | "WaitingForUser"
  | "Completed"
  | "Failed"
  | "Cancelled";

export type AgentTaskAttemptStatus = "Running" | "Completed" | "Failed" | "Cancelled" | "Paused";

export interface AgentTask {
  id: string;
  title: string;
  conversationId?: string;
  currentRunId?: string;
  status: AgentTaskStatus;
  attemptCount: number;
  maximumAttempts: number;
  logicalRevision: number;
  availableAtUtc: string;
  lastErrorCode: string;
  lastErrorMessage: string;
}

export interface AgentTaskAttempt {
  id: string;
  attemptNumber: number;
  runId?: string;
  status: AgentTaskAttemptStatus;
  workerId: string;
  startedAtUtc: string;
  finishedAtUtc?: string;
  errorCode: string;
}

export interface AgentTaskEvent {
  id: string;
  attemptNumber?: number;
  runId?: string;
  kind: string;
  status: AgentTaskStatus;
  workerId: string;
  occurredAtUtc: string;
}

export interface AgentTaskDetail {
  task: AgentTask;
  attempts: AgentTaskAttempt[];
  events: AgentTaskEvent[];
}

export interface CreateAgentTaskRequest {
  title: string;
  description?: string;
  input: string;
  conversationId?: string;
  idempotencyKey: string;
  priority?: number;
  maximumAttempts?: number;
}

interface UnifiedChatConversationDto {
  Id: string;
  Title: string;
  CreatedAtUtc: string;
  UpdatedAtUtc: string;
}

interface UnifiedChatMessageDto {
  Id: string;
  ConversationId: string;
  Role: string | number;
  Content: string;
  CreatedAtUtc: string;
}

interface UnifiedChatConversationDetailDto {
  Conversation: UnifiedChatConversationDto;
  Messages: UnifiedChatMessageDto[];
}

interface UnifiedChatRunDto {
  Id: string;
}

interface UnifiedChatRunStateDto {
  Id: string;
  Status: string | number;
  Output?: string | null;
  ErrorCode?: string | null;
}

interface AgentTaskDto {
  Id: string;
  Title: string;
  ConversationId?: string | null;
  CurrentRunId?: string | null;
  Status: string | number;
  AttemptCount: number;
  MaximumAttempts: number;
  LogicalRevision: number;
  AvailableAtUtc: string;
  LastErrorCode?: string | null;
  LastErrorMessage?: string | null;
}

interface AgentTaskAttemptDto {
  Id: string;
  AttemptNumber: number;
  RunId?: string | null;
  Status: string | number;
  WorkerId?: string | null;
  StartedAtUtc: string;
  FinishedAtUtc?: string | null;
  ErrorCode?: string | null;
}

interface AgentTaskEventDto {
  Id: string;
  AttemptNumber?: number | null;
  RunId?: string | null;
  Kind: string;
  Status: string | number;
  WorkerId?: string | null;
  OccurredAtUtc: string;
}

interface AgentTaskDetailDto {
  Task: AgentTaskDto;
  Attempts: AgentTaskAttemptDto[];
  Events: AgentTaskEventDto[];
}

interface UnifiedChatRunDetailsDto {
  AgentRuns?: Array<{
    EntryRunId: string;
    Kind: string | number;
    AgentVersionId: string;
    Depth: number;
    StartedAtUtc: string;
    Output?: string | null;
    ErrorCode?: string | null;
  }>;
  Orchestrations?: Array<{
    EntryRunId: string;
    OrchestrationVersionId: string;
    Depth: number;
    StartedAtUtc: string;
    Output?: string | null;
    ErrorCode?: string | null;
  }>;
  ToolCalls?: Array<{
    EntryRunId: string;
    ToolVersionId: string;
    Depth: number;
    Status: string | number;
    StartedAtUtc: string;
    ArgumentsJson?: string | null;
    ResultContent?: string | null;
    ErrorCode?: string | null;
  }>;
}

export interface UnifiedChatRunState {
  id: string;
  status: string;
  output: string;
  errorCode: string;
}

const unifiedRunStatusNames = ["Pending", "Running", "WaitingForApproval", "Completed", "Failed", "Cancelled", "Blocked"];
const normalizeRunStatus = (status: string | number) =>
  typeof status === "number" ? unifiedRunStatusNames[status] || String(status) : status;

const agentTaskStatusNames: AgentTaskStatus[] = [
  "Pending",
  "Running",
  "WaitingForApproval",
  "WaitingForUser",
  "Completed",
  "Failed",
  "Cancelled"
];
const agentTaskAttemptStatusNames: AgentTaskAttemptStatus[] = ["Running", "Completed", "Failed", "Cancelled", "Paused"];
const mapAgentTask = (value: AgentTaskDto): AgentTask => ({
  id: value.Id,
  title: value.Title,
  conversationId: value.ConversationId || undefined,
  currentRunId: value.CurrentRunId || undefined,
  status:
    typeof value.Status === "number"
      ? agentTaskStatusNames[value.Status] || "Failed"
      : (value.Status as AgentTaskStatus),
  attemptCount: value.AttemptCount,
  maximumAttempts: value.MaximumAttempts,
  logicalRevision: value.LogicalRevision,
  availableAtUtc: value.AvailableAtUtc,
  lastErrorCode: value.LastErrorCode || "",
  lastErrorMessage: value.LastErrorMessage || ""
});

interface UnifiedChatRunEventDto {
  EntryRunId: string;
  Sequence: number;
  CorrelationId: string;
  Kind: string;
  OccurredAtUtc: string;
  ParentRunId?: string | null;
  Depth: number;
  PayloadJson: string;
}

const apiBaseUrl = () => {
  const value = (import.meta.env.VITE_API_URL as string | undefined) || "";
  return value === "/" ? "" : value.replace(/\/$/, "");
};

const readErrorMessage = async (response: Response) => {
  try {
    const body = (await response.json()) as { Message?: string; message?: string };
    return body.Message || body.message || `请求失败（${response.status}）`;
  } catch {
    return `请求失败（${response.status}）`;
  }
};

const requestJson = async <T>(path: string, init?: RequestInit) => {
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json");
  headers.set("Authorization", `Bearer ${store.getState().user.token}`);
  const response = await fetch(`${apiBaseUrl()}/Agent${path}`, {
    ...init,
    headers
  });
  if (!response.ok) throw new Error(await readErrorMessage(response));
  const body = (await response.json()) as { Success?: boolean; Message?: string; Data?: T };
  if (!body.Success) throw new Error(body.Message || "请求失败。");
  return body.Data as T;
};

export const listUnifiedChatConversations = (take = 40) =>
  requestJson<UnifiedChatConversationDto[]>(`/api/chat/conversations?take=${encodeURIComponent(take)}`).then(values =>
    values.map(value => ({
      id: value.Id,
      title: value.Title,
      createdAtUtc: value.CreatedAtUtc,
      updatedAtUtc: value.UpdatedAtUtc
    }))
  );

export const getUnifiedChatConversation = (conversationId: string, take = 160) =>
  requestJson<UnifiedChatConversationDetailDto>(
    `/api/chat/conversations/${encodeURIComponent(conversationId)}?take=${encodeURIComponent(take)}`
  ).then(value => ({
    conversation: {
      id: value.Conversation.Id,
      title: value.Conversation.Title,
      createdAtUtc: value.Conversation.CreatedAtUtc,
      updatedAtUtc: value.Conversation.UpdatedAtUtc
    },
    messages: value.Messages.map(message => ({
      id: message.Id,
      conversationId: message.ConversationId,
      role: message.Role,
      content: message.Content,
      createdAtUtc: message.CreatedAtUtc
    }))
  }));

export const listUnifiedChatRuns = (conversationId: string, take = 20) =>
  requestJson<UnifiedChatRunDto[]>(
    `/api/chat/conversations/${encodeURIComponent(conversationId)}/runs?take=${encodeURIComponent(take)}`
  ).then(values => values.map(value => value.Id));

export const getUnifiedChatRun = (runId: string) =>
  requestJson<UnifiedChatRunStateDto>(`/api/chat/runs/${encodeURIComponent(runId)}`).then(value => ({
    id: value.Id,
    status: normalizeRunStatus(value.Status),
    output: value.Output || "",
    errorCode: value.ErrorCode || ""
  }));

export const getUnifiedChatRunDetailEvents = (runId: string) =>
  requestJson<UnifiedChatRunDetailsDto>(`/api/chat/runs/${encodeURIComponent(runId)}/details`).then(value => {
    const events: UnifiedChatRunEvent[] = [];
    for (const run of value.AgentRuns || []) {
      events.push({
        runId: run.EntryRunId,
        conversationId: "",
        sequence: 0,
        kind: run.Kind === 0 || String(run.Kind).toLowerCase() === "main" ? "main-agent-started" : "child-agent-started",
        occurredAtUtc: run.StartedAtUtc,
        correlationId: "",
        depth: run.Depth,
        payloadJson: JSON.stringify({ agentVersionId: run.AgentVersionId, text: run.Output || "", errorCode: run.ErrorCode || "" }),
        route: ""
      });
    }
    for (const tool of value.ToolCalls || []) {
      const status = normalizeRunStatus(tool.Status).toLowerCase();
      events.push({
        runId: tool.EntryRunId,
        conversationId: "",
        sequence: 0,
        kind: status === "completed" ? "tool-succeeded" : status === "blocked" ? "tool-blocked" : "tool-failed",
        occurredAtUtc: tool.StartedAtUtc,
        correlationId: "",
        depth: tool.Depth,
        payloadJson: JSON.stringify({
          toolName: tool.ToolVersionId,
          argumentsJson: tool.ArgumentsJson || "",
          text: tool.ResultContent || "",
          errorCode: tool.ErrorCode || ""
        }),
        route: ""
      });
    }
    for (const orchestration of value.Orchestrations || []) {
      events.push({
        runId: orchestration.EntryRunId,
        conversationId: "",
        sequence: 0,
        kind: "orchestration-started",
        occurredAtUtc: orchestration.StartedAtUtc,
        correlationId: "",
        depth: orchestration.Depth,
        payloadJson: JSON.stringify({
          orchestrationVersionId: orchestration.OrchestrationVersionId,
          text: orchestration.Output || "",
          errorCode: orchestration.ErrorCode || ""
        }),
        route: ""
      });
    }
    return events
      .sort((left, right) => Date.parse(left.occurredAtUtc) - Date.parse(right.occurredAtUtc))
      .map((event, index) => ({ ...event, sequence: index + 1 }));
  });

export const listUnifiedChatRunEvents = (runId: string, take = 160) =>
  requestJson<UnifiedChatRunEventDto[]>(`/api/chat/runs/${encodeURIComponent(runId)}/events?take=${encodeURIComponent(take)}`).then(
    values =>
      values.map(value => ({
        runId: value.EntryRunId,
        conversationId: "",
        sequence: value.Sequence,
        kind: value.Kind,
        occurredAtUtc: value.OccurredAtUtc,
        correlationId: value.CorrelationId,
        parentRunId: value.ParentRunId,
        depth: value.Depth,
        payloadJson: value.PayloadJson,
        route: ""
      }))
  );

export const cancelUnifiedChatRun = (runId: string) =>
  requestJson<{ RunId: string }>(`/api/chat/runs/${encodeURIComponent(runId)}/cancel`, { method: "POST" });

export const listAgentTasks = (take = 100) =>
  requestJson<AgentTaskDto[]>(`/api/agent-tasks?take=${encodeURIComponent(take)}`).then(values => values.map(mapAgentTask));

export const getAgentTaskDetail = (taskId: string) =>
  requestJson<AgentTaskDetailDto>(`/api/agent-tasks/${encodeURIComponent(taskId)}`).then(value => ({
    task: mapAgentTask(value.Task),
    attempts: value.Attempts.map(attempt => ({
      id: attempt.Id,
      attemptNumber: attempt.AttemptNumber,
      runId: attempt.RunId || undefined,
      status:
        typeof attempt.Status === "number"
          ? agentTaskAttemptStatusNames[attempt.Status] || "Failed"
          : (attempt.Status as AgentTaskAttemptStatus),
      workerId: attempt.WorkerId || "",
      startedAtUtc: attempt.StartedAtUtc,
      finishedAtUtc: attempt.FinishedAtUtc || undefined,
      errorCode: attempt.ErrorCode || ""
    })),
    events: value.Events.map(event => ({
      id: event.Id,
      attemptNumber: event.AttemptNumber ?? undefined,
      runId: event.RunId || undefined,
      kind: event.Kind,
      status:
        typeof event.Status === "number"
          ? agentTaskStatusNames[event.Status] || "Failed"
          : (event.Status as AgentTaskStatus),
      workerId: event.WorkerId || "",
      occurredAtUtc: event.OccurredAtUtc
    }))
  }));

export const createAgentTask = (request: CreateAgentTaskRequest) =>
  requestJson<AgentTaskDto>("/api/agent-tasks", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      title: request.title,
      description: request.description || "",
      input: request.input,
      sourceType: "chat",
      sourceId: "unified-chat",
      idempotencyKey: request.idempotencyKey,
      conversationId: request.conversationId,
      priority: request.priority ?? 0,
      maximumAttempts: request.maximumAttempts ?? 3
    })
  }).then(mapAgentTask);

export const cancelAgentTask = (taskId: string) =>
  requestJson<AgentTaskDto>(`/api/agent-tasks/${encodeURIComponent(taskId)}/cancel`, { method: "POST" }).then(mapAgentTask);

export const resumeAgentTaskWithUserInput = (taskId: string, expectedLogicalRevision: number, input: string) =>
  requestJson<AgentTaskDto>(`/api/agent-tasks/${encodeURIComponent(taskId)}/user-input`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ expectedLogicalRevision, input })
  }).then(mapAgentTask);

const parseFrame = (frame: string): UnifiedChatRunEvent | null => {
  const fields = frame.split(/\r?\n/);
  const eventName = fields.find(line => line.startsWith("event:"))?.slice(6).trim();
  const data = fields.filter(line => line.startsWith("data:")).map(line => line.slice(5).trimStart()).join("\n");
  if (!eventName || !data) return null;
  try {
    const event = JSON.parse(data) as UnifiedChatRunEvent;
    return { ...event, kind: event.kind || eventName };
  } catch {
    return null;
  }
};

const parseUnifiedChatEvent = (frame: UnifiedChatSdkFrame): UnifiedChatRunEvent | null => {
  if (!frame.data) return null;
  try {
    const event = JSON.parse(frame.data) as Record<string, unknown>;
    return {
      runId: String(event.runId ?? event.RunId ?? ""),
      conversationId: String(event.conversationId ?? event.ConversationId ?? ""),
      sequence: Number(event.sequence ?? event.Sequence ?? 0),
      kind: String(event.kind ?? event.Kind ?? frame.event ?? "message"),
      occurredAtUtc: String(event.occurredAtUtc ?? event.OccurredAtUtc ?? ""),
      correlationId: String(event.correlationId ?? event.CorrelationId ?? ""),
      parentRunId: (event.parentRunId ?? event.ParentRunId ?? null) as string | null,
      depth: Number(event.depth ?? event.Depth ?? 0),
      payloadJson: String(event.payloadJson ?? event.PayloadJson ?? ""),
      route: String(event.route ?? event.Route ?? "")
    };
  } catch {
    return null;
  }
};

export class UnifiedChatProvider extends AbstractChatProvider<
  UnifiedChatSdkMessage,
  UnifiedChatSdkRequest,
  UnifiedChatSdkFrame
> {
  constructor(private readonly onEvent: (event: UnifiedChatRunEvent) => void) {
    super({
      request: XRequest<UnifiedChatSdkRequest, UnifiedChatSdkFrame, UnifiedChatSdkMessage>(
        `${apiBaseUrl()}/Agent/api/chat/runs`,
        {
          manual: true,
          method: "POST",
          headers: {
            Accept: "text/event-stream",
            "Content-Type": "application/json",
            Authorization: `Bearer ${store.getState().user.token}`
          }
        }
      )
    });
  }

  transformParams(
    requestParams: Partial<UnifiedChatSdkRequest>,
    options: XRequestOptions<UnifiedChatSdkRequest, UnifiedChatSdkFrame, UnifiedChatSdkMessage>
  ): UnifiedChatSdkRequest {
    const lastMessage = requestParams.messages?.at(-1);
    return {
      input: lastMessage?.content || requestParams.input || options.params?.input || "",
      conversationId: requestParams.conversationId
    };
  }

  transformLocalMessage(requestParams: Partial<UnifiedChatSdkRequest>): UnifiedChatSdkMessage {
    return requestParams.messages?.at(-1) || { role: "user", content: requestParams.input || "" };
  }

  transformMessage(info: TransformMessage<UnifiedChatSdkMessage, UnifiedChatSdkFrame>): UnifiedChatSdkMessage {
    const event = parseUnifiedChatEvent(info.chunk);
    if (!event) return info.originMessage || { role: "assistant", content: "" };
    this.onEvent(event);
    try {
      const payload = JSON.parse(event.payloadJson) as { eventKind?: string; text?: string };
      if (event.kind === "message" && event.depth === 0 && payload.eventKind === "Delta") {
        return {
          role: "assistant",
          content: info.status === "updating" ? `${info.originMessage?.content || ""}${payload.text || ""}` : payload.text || ""
        };
      }
    } catch {
      // The event remains available to the inspector even when its payload is malformed.
    }
    return info.originMessage || { role: "assistant", content: "" };
  }
}

export const streamUnifiedChatRun = async ({ input, conversationId, signal, onOpen, onEvent }: StreamChatRunOptions) => {
  const response = await fetch(`${apiBaseUrl()}/Agent/api/chat/runs`, {
    method: "POST",
    headers: {
      Accept: "text/event-stream",
      "Content-Type": "application/json",
      Authorization: `Bearer ${store.getState().user.token}`
    },
    body: JSON.stringify({ input, conversationId }),
    signal
  });
  if (!response.ok) throw new Error(await readErrorMessage(response));
  if (!response.body) throw new Error("服务端未返回流式响应。");
  onOpen?.({
    runId: response.headers.get("X-Agent-Run-ID") || undefined,
    conversationId: response.headers.get("X-Agent-Conversation-ID") || undefined
  });

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";
  try {
    while (true) {
      const { done, value } = await reader.read();
      buffer += decoder.decode(value, { stream: !done });
      let boundary = buffer.search(/\r?\n\r?\n/);
      while (boundary >= 0) {
        const frame = buffer.slice(0, boundary);
        buffer = buffer.slice(boundary).replace(/^\r?\n\r?\n/, "");
        const event = parseFrame(frame);
        if (event) onEvent(event);
        boundary = buffer.search(/\r?\n\r?\n/);
      }
      if (done) break;
    }
  } finally {
    reader.releaseLock();
  }
};
