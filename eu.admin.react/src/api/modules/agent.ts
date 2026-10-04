import http from "@/api";
import { store } from "@/redux";

export type AgentRuntimeStatus = "Enabled" | "Disabled" | "Archived";
export type AgentOutputMode = "Text" | "Structured";

export interface AgentVersion {
  Id: string;
  Label: string;
  IsDraft: boolean;
  Instructions: string;
  ModelProfileId: string;
  OutputMode: AgentOutputMode;
  OutputJsonSchema?: string | null;
  OutputSchemaSha256?: string | null;
  SkillVersionIds: string[];
  ToolVersionIds: string[];
  KnowledgeBaseIds: string[];
  ChildAgentIds: string[];
  OrchestrationIds: string[];
}

export interface AgentDefinition {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  RuntimeStatus: AgentRuntimeStatus;
  LogicalRevision: number;
  Draft: AgentVersion;
  PublishedVersions: AgentVersion[];
  DeploymentTarget: string;
  Host: string;
}

export interface AgentListItem {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  RuntimeStatus: AgentRuntimeStatus;
  LogicalRevision: number;
  CurrentPublishedLabel?: string | null;
}

export interface AgentRunAuditRecord {
  RunId: string;
  AgentVersionId: string;
  Status: string;
  StartedAtUtc: string;
  FinishedAtUtc?: string | null;
  OutputCharacters: number;
  ToolCallCount: number;
  ToolCalls: AgentToolCallAuditRecord[];
  ErrorCode: string;
  ModelProfileId?: string | null;
  ModelUsage?: AgentModelUsage | null;
}

export interface AgentToolCallAuditRecord {
  ToolVersionId: string;
  ToolName: string;
  Risk: string;
  Status: string;
  StartedAtUtc: string;
  FinishedAtUtc: string;
  ErrorCode: string;
}

export interface AgentModelUsage {
  InputTokens: number | null;
  OutputTokens: number | null;
  TotalTokens: number | null;
  Status: "Unknown" | "Partial" | "Reported";
  ModelDurationMilliseconds: number | null;
  TimeToFirstTextMilliseconds: number | null;
}

export interface AgentRunEvent {
  runId?: string;
  occurredAtUtc?: string;
  text?: string;
  toolVersionId?: string | null;
  toolName?: string;
  toolCallId?: string | null;
  argumentsJson?: string;
  errorCode?: string;
  knowledgeBaseCount?: number;
  knowledgeHitCount?: number;
}

export interface AgentUserTokenQuotaPeriod {
  Kind: "Daily" | "Monthly";
  StartUtc: string;
  EndUtc: string;
  LimitTokens: string;
  UsedTokens: string;
  ReservedTokens: string;
  RemainingTokens: string | null;
  HasUnknownUsage: boolean;
  CanReserve: boolean;
}

export interface AgentUserTokenQuotaBalance {
  Enabled: boolean;
  TimeZoneId: string;
  EvaluatedAtUtc: string;
  RequestReservationTokens: string | null;
  CanReserve: boolean | null;
  Periods: AgentUserTokenQuotaPeriod[];
}

const isQuotaTokenCount = (value: unknown): value is string =>
  typeof value === "string" && /^(0|[1-9][0-9]{0,18})$/.test(value) && BigInt(value) <= 9223372036854775807n;
const isQuotaDate = (value: unknown): value is string => typeof value === "string" && Number.isFinite(Date.parse(value));
const isQuotaPeriod = (value: unknown): value is AgentUserTokenQuotaPeriod =>
  !!value && typeof value === "object"
  && "Kind" in value && (value.Kind === "Daily" || value.Kind === "Monthly")
  && "StartUtc" in value && isQuotaDate(value.StartUtc) && "EndUtc" in value && isQuotaDate(value.EndUtc)
  && "LimitTokens" in value && isQuotaTokenCount(value.LimitTokens)
  && "UsedTokens" in value && isQuotaTokenCount(value.UsedTokens)
  && "ReservedTokens" in value && isQuotaTokenCount(value.ReservedTokens)
  && "RemainingTokens" in value && (value.RemainingTokens === null || isQuotaTokenCount(value.RemainingTokens))
  && "HasUnknownUsage" in value && typeof value.HasUnknownUsage === "boolean"
  && "CanReserve" in value && typeof value.CanReserve === "boolean";

export const isAgentUserTokenQuotaBalance = (value: unknown): value is AgentUserTokenQuotaBalance => {
  if (!value || typeof value !== "object" || !("Enabled" in value) || typeof value.Enabled !== "boolean"
    || !("TimeZoneId" in value) || typeof value.TimeZoneId !== "string" || value.TimeZoneId.length > 128
    || !("EvaluatedAtUtc" in value) || !isQuotaDate(value.EvaluatedAtUtc)
    || !("RequestReservationTokens" in value) || !("CanReserve" in value) || !("Periods" in value) || !Array.isArray(value.Periods)) return false;
  if (!value.Enabled) return value.RequestReservationTokens === null && value.CanReserve === null && value.Periods.length === 0;
  if (!isQuotaTokenCount(value.RequestReservationTokens) || BigInt(value.RequestReservationTokens) < 1n
    || typeof value.CanReserve !== "boolean" || value.Periods.length < 1 || value.Periods.length > 2) return false;
  const reservation = BigInt(value.RequestReservationTokens);
  const evaluated = Date.parse(value.EvaluatedAtUtc);
  const kinds = new Set<string>();
  const periods: unknown[] = value.Periods;
  for (const period of periods) {
    if (!isQuotaPeriod(period) || kinds.has(period.Kind) || Date.parse(period.StartUtc) > evaluated || Date.parse(period.EndUtc) <= evaluated
      || BigInt(period.LimitTokens) < reservation) return false;
    kinds.add(period.Kind);
    if (period.HasUnknownUsage) {
      if (period.RemainingTokens !== null || period.CanReserve) return false;
    } else {
      if (!isQuotaTokenCount(period.RemainingTokens)) return false;
      const available = BigInt(period.LimitTokens) - BigInt(period.UsedTokens) - BigInt(period.ReservedTokens);
      const remaining = available > 0n ? available : 0n;
      if (BigInt(period.RemainingTokens) !== remaining || period.CanReserve !== (remaining >= reservation)) return false;
    }
  }
  return value.CanReserve === periods.every(period => isQuotaPeriod(period) && period.CanReserve);
};

export interface AgentQuotaPolicy {
  UseDefaults: boolean;
  DailyTotalTokens: string | null;
  MonthlyTotalTokens: string | null;
  RequestReservationTokens: string | null;
  Revision: string;
}

export interface AgentQuotaPolicyInput {
  OperationId: string;
  ExpectedRevision: string;
  UseDefaults: boolean;
  DailyTotalTokens: string | null;
  MonthlyTotalTokens: string | null;
  RequestReservationTokens: string | null;
  Reason: string;
}

export interface AgentQuotaPending {
  Id: string;
  RunId: string;
  ConsumerUserId: string;
  State: 0 | 1 | 3;
  ReservedTokens: string;
  SettledAtUtc: string | null;
}

export interface AgentQuotaReconcileInput {
  OperationId: string;
  ActualTokens: string;
  Confirmed: boolean;
  Reason: string;
  EvidenceReference: string;
}

export interface AgentQuotaReconciliation {
  OperationId: string;
  ReservationId: string;
  ActualTokens: string;
  AppliedAtUtc: string;
}

export const isQuotaId = (value: unknown): value is string => typeof value === "string"
  && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
  && value !== "00000000-0000-0000-0000-000000000000";

// 所有额度输入保持字符串；绝不通过 InputNumber / Number 把 long 转成浮点数。
export const parseQuotaTokenInput = (value: string, allowZero = false): string | null => {
  const text = value.trim();
  if (text === "") return null;
  if (!isQuotaTokenCount(text) || (!allowZero && BigInt(text) === 0n)) throw new Error("Token 数必须是范围内的整数");
  return text;
};

export const isAgentQuotaPolicy = (value: unknown): value is AgentQuotaPolicy => {
  if (!value || typeof value !== "object" || !("UseDefaults" in value) || typeof value.UseDefaults !== "boolean"
    || !("Revision" in value) || !isQuotaTokenCount(value.Revision)
    || !("DailyTotalTokens" in value) || !("MonthlyTotalTokens" in value) || !("RequestReservationTokens" in value)) return false;
  if (value.UseDefaults) return value.DailyTotalTokens === null && value.MonthlyTotalTokens === null && value.RequestReservationTokens === null;
  if (!isQuotaTokenCount(value.RequestReservationTokens) || BigInt(value.RequestReservationTokens) < 1n) return false;
  const reservation = BigInt(value.RequestReservationTokens);
  const limits = [value.DailyTotalTokens, value.MonthlyTotalTokens];
  return limits.some(limit => limit !== null) && limits.every(limit => limit === null
    || (isQuotaTokenCount(limit) && BigInt(limit) >= reservation));
};

export const isAgentQuotaPending = (value: unknown): value is AgentQuotaPending => !!value && typeof value === "object"
  && "Id" in value && isQuotaId(value.Id) && "RunId" in value && isQuotaId(value.RunId)
  && "ConsumerUserId" in value && isQuotaId(value.ConsumerUserId)
  && "State" in value && (value.State === 0 || value.State === 1 || value.State === 3)
  && "ReservedTokens" in value && isQuotaTokenCount(value.ReservedTokens) && BigInt(value.ReservedTokens) > 0n
  && "SettledAtUtc" in value && (value.SettledAtUtc === null || isQuotaDate(value.SettledAtUtc));

export const isAgentQuotaReconciliation = (value: unknown): value is AgentQuotaReconciliation => !!value && typeof value === "object"
  && "OperationId" in value && isQuotaId(value.OperationId) && "ReservationId" in value && isQuotaId(value.ReservationId)
  && "ActualTokens" in value && isQuotaTokenCount(value.ActualTokens) && "AppliedAtUtc" in value && isQuotaDate(value.AppliedAtUtc);

const isAgentRunEvent = (value: unknown): value is AgentRunEvent => {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  const fields = value as Record<string, unknown>;
  const textFields = ["runId", "occurredAtUtc", "text", "toolVersionId", "toolName", "toolCallId", "argumentsJson", "errorCode"];
  const countFields = ["knowledgeBaseCount", "knowledgeHitCount"];
  return (
    textFields.every(key => fields[key] === undefined || typeof fields[key] === "string"
      || (fields[key] === null && (key === "toolVersionId" || key === "toolCallId")))
    && countFields.every(key => fields[key] === undefined
      || (typeof fields[key] === "number" && Number.isSafeInteger(fields[key]) && fields[key] >= 0))
  );
};

export interface PublishedSkillReference {
  SkillId: string;
  VersionId: string;
  SkillCode: string;
  SkillName: string;
  VersionLabel: string;
  ManifestSha256: string;
}

export interface PublishedToolReference {
  ServerId: string;
  ServerCode: string;
  ServerName: string;
  ToolVersionId: string;
  ToolName: string;
  Description: string;
  Risk: string;
  Sha256: string;
}

export interface KnowledgeReference {
  KnowledgeBaseId: string;
  Code: string;
  Name: string;
  LogicalRevision: number;
}

export interface OrchestrationReference {
  Id: string;
  Code: string;
  Name: string;
  Status: string;
  CurrentPublishedLabel?: string | null;
}

export interface MainAgentAssignment {
  AgentId: string;
  AgentVersionId: string;
  LogicalRevision: number;
  UpdatedAtUtc: string;
}

export interface AgentCapabilities {
  ModelProfileIds: string[];
  Features: {
    ModelJudge: boolean;
  };
}

export interface SaveAgentDraftInput {
  expectedLogicalRevision: number;
  name: string;
  description: string;
  instructions: string;
  modelProfileId: string;
  outputMode: AgentOutputMode;
  outputJsonSchema: string | null;
  skillVersionIds: string[];
  toolVersionIds: string[];
  knowledgeBaseIds: string[];
  childAgentIds: string[];
  orchestrationIds: string[];
}

interface AgentServiceResponse {
  Status: number;
  Success: boolean;
  Message?: string | null;
  Data: unknown;
}

export class AgentExportError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "AgentExportError";
  }
}

const throwAgentExportFailure = async (blob: Blob) => {
  let payload: unknown;
  try {
    payload = JSON.parse(await blob.text());
  } catch {
    return;
  }
  if (
    typeof payload === "object" &&
    payload !== null &&
    "Status" in payload &&
    "Success" in payload &&
    "Data" in payload
  ) {
    const result = payload as AgentServiceResponse;
    if (!result.Success || result.Status !== 200) {
      throw new AgentExportError(result.Message || "Agent 导出失败");
    }
  }
};

const agentUrl = (path: string) => `/Agent${path}`;

export const getAgent = async (id: string) =>
  (await http.get<AgentDefinition>(agentUrl(`/api/agents/${encodeURIComponent(id)}`))).Data;

export const createAgent = async (input: { code: string; name: string; description: string }) =>
  (await http.post<AgentDefinition>(agentUrl("/api/agents"), input)).Data;

export const saveAgentDraft = async (id: string, input: SaveAgentDraftInput) =>
  (await http.put<AgentDefinition>(agentUrl(`/api/agents/${encodeURIComponent(id)}/draft`), input)).Data;

export const publishAgent = async (id: string, expectedLogicalRevision: number) =>
  (await http.post<AgentDefinition>(agentUrl(`/api/agents/${encodeURIComponent(id)}/publish`), {
    expectedLogicalRevision
  })).Data;

export const setAgentStatus = async (
  id: string,
  runtimeStatus: AgentRuntimeStatus,
  expectedLogicalRevision: number
) =>
  (await http.put<AgentDefinition>(agentUrl(`/api/agents/${encodeURIComponent(id)}/status`), {
    runtimeStatus,
    expectedLogicalRevision
  })).Data;

export const listAgents = async (status?: AgentRuntimeStatus) =>
  (await http.get<AgentListItem[]>(agentUrl("/api/agents"), status ? { status } : undefined)).Data;

export const getAgentCapabilities = async () =>
  (await http.get<AgentCapabilities>(agentUrl("/api/platform/capabilities"))).Data;

export const listPublishedSkills = async () =>
  (await http.get<PublishedSkillReference[]>(agentUrl("/api/skill-versions"))).Data;

export const listPublishedTools = async () =>
  (await http.get<PublishedToolReference[]>(agentUrl("/api/mcp/tool-versions"))).Data;

export const listKnowledgeReferences = async () =>
  (await http.get<KnowledgeReference[]>(agentUrl("/api/knowledge-base-references"))).Data;

export const listOrchestrations = async () =>
  (await http.get<OrchestrationReference[]>(agentUrl("/api/orchestrations"))).Data;

export const getMainAgent = async () =>
  (await http.get<MainAgentAssignment>(agentUrl("/api/platform/main-agent"))).Data;

export const setMainAgent = async (agentId: string, expectedLogicalRevision: number | null) =>
  (await http.put<MainAgentAssignment>(agentUrl("/api/platform/main-agent"), {
    agentId,
    expectedLogicalRevision
  })).Data;

export const exportAgent = async (id: string) => {
  const response = await http.service.get<Blob>(agentUrl(`/api/agents/${encodeURIComponent(id)}/export`), {
    responseType: "blob"
  });
  const blob = response as unknown as Blob;
  await throwAgentExportFailure(blob);
  return blob;
};

export const importAgent = async (content: string) =>
  (await http.post<AgentDefinition>(agentUrl("/api/agents/import"), content, {
    headers: { "Content-Type": "application/json" }
  })).Data;

export const listAgentRuns = async (id: string, take = 10, signal?: AbortSignal) =>
  (await http.get<AgentRunAuditRecord[]>(agentUrl(`/api/agents/${encodeURIComponent(id)}/runs`), { take }, { signal })).Data;

export const getCurrentUserTokenQuota = async (signal?: AbortSignal) => {
  const value: unknown = (await http.get<AgentUserTokenQuotaBalance>(agentUrl("/api/agent-usage/quota"), undefined, { signal })).Data;
  if (!isAgentUserTokenQuotaBalance(value)) throw new Error("租户额度响应格式无效");
  return value;
};

export const getAgentQuotaManagementAccess = async (signal?: AbortSignal): Promise<boolean> => {
  const value: unknown = (await http.get<unknown>(agentUrl("/api/agent-usage/management/access"), undefined, { signal })).Data;
  if (!value || typeof value !== "object" || !("CanManage" in value) || typeof value.CanManage !== "boolean") throw new Error("额度管理权限响应无效");
  return value.CanManage;
};

const quotaManagementUrl = agentUrl("/api/agent-usage/management");

export const getAgentQuotaPolicy = async (signal?: AbortSignal) => {
  const value: unknown = (await http.get<unknown>(`${quotaManagementUrl}/policy`, undefined, { signal })).Data;
  if (!isAgentQuotaPolicy(value)) throw new Error("租户额度设置响应无效");
  return value;
};

export const saveAgentQuotaPolicy = async (input: AgentQuotaPolicyInput, signal?: AbortSignal) => {
  const value: unknown = (await http.put<unknown>(`${quotaManagementUrl}/policy`, input, { signal })).Data;
  if (!isAgentQuotaPolicy(value)) throw new Error("额度操作响应无效，请用相同操作标识核实结果");
  return value;
};

export const getAgentQuotaPending = async (signal?: AbortSignal) => {
  const value: unknown = (await http.get<unknown>(`${quotaManagementUrl}/pending`, undefined, { signal })).Data;
  if (!Array.isArray(value) || value.length > 100 || !value.every(isAgentQuotaPending)
    || new Set(value.map(item => item.Id.toLowerCase())).size !== value.length) throw new Error("待对账响应无效");
  return value;
};

export const reconcileAgentQuota = async (reservationId: string, input: AgentQuotaReconcileInput, signal?: AbortSignal) => {
  if (!isQuotaId(reservationId)) throw new Error("预占标识无效");
  const value: unknown = (await http.post<unknown>(`${quotaManagementUrl}/reservations/${encodeURIComponent(reservationId)}/reconcile`, input, { signal })).Data;
  if (!isAgentQuotaReconciliation(value) || value.OperationId.toLowerCase() !== input.OperationId.toLowerCase()
    || value.ReservationId.toLowerCase() !== reservationId.toLowerCase() || value.ActualTokens !== input.ActualTokens)
    throw new Error("对账响应无效，请用相同操作标识核实结果");
  return value;
};

export const runAgent = async (
  id: string,
  input: string,
  onEvent: (name: string, event: AgentRunEvent) => void,
  signal: AbortSignal
) => {
  const baseUrl = ((import.meta.env.VITE_API_URL as string | undefined) || "").replace(/\/$/, "");
  const response = await fetch(`${baseUrl}${agentUrl(`/api/agents/${encodeURIComponent(id)}/runs`)}`, {
    method: "POST",
    headers: { Accept: "text/event-stream", "Content-Type": "application/json", Authorization: `Bearer ${store.getState().user.token}` },
    body: JSON.stringify({ input }),
    signal
  });
  if (!response.ok) throw new Error(`运行失败（${response.status}）`);
  if (!response.headers.get("Content-Type")?.toLowerCase().startsWith("text/event-stream")) {
    throw new Error("服务端未返回运行事件流，请刷新运行历史确认状态");
  }
  if (!response.body) throw new Error("运行流不可用");
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";
  try {
    while (true) {
      const { done, value } = await reader.read();
      buffer += decoder.decode(value || new Uint8Array(), { stream: !done });
      const frames = buffer.split("\n\n");
      buffer = frames.pop() || "";
      for (const frame of frames) {
        const name = frame.match(/^event:\s*(.+)$/m)?.[1] || "message";
        const data = frame.match(/^data:\s*(.+)$/m)?.[1];
        if (!data) continue;
        const event: unknown = JSON.parse(data);
        if (!isAgentRunEvent(event)) throw new Error("运行事件格式无效");
        onEvent(name, event);
      }
      if (done) break;
    }
  } finally {
    try {
      await reader.cancel();
    } catch {
      // Cancellation may race the fetch abort; the reader still must release its lock.
    } finally {
      reader.releaseLock();
    }
  }
};
