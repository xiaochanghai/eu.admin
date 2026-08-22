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

const requestJson = async <T>(path: string) => {
  const response = await fetch(`${apiBaseUrl()}/Agent${path}`, {
    headers: { Accept: "application/json", Authorization: `Bearer ${store.getState().user.token}` }
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
