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
  Id: string;
  Status: string;
  StartedAtUtc: string;
  FinishedAtUtc?: string | null;
  ToolCallCount: number;
  ErrorCode: string;
}

export interface AgentRunEvent {
  runId?: string;
  occurredAtUtc?: string;
  text?: string;
  toolVersionId?: string;
  toolName?: string;
  toolCallId?: string;
  argumentsJson?: string;
  errorCode?: string;
  knowledgeBaseCount?: number;
  knowledgeHitCount?: number;
}

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

export const listAgentRuns = async (id: string, take = 10) =>
  (await http.get<AgentRunAuditRecord[]>(agentUrl(`/api/agents/${encodeURIComponent(id)}/runs`), { take })).Data;

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
  if (!response.body) throw new Error("运行流不可用");
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";
  while (true) {
    const { done, value } = await reader.read();
    buffer += decoder.decode(value || new Uint8Array(), { stream: !done });
    const frames = buffer.split("\n\n");
    buffer = frames.pop() || "";
    for (const frame of frames) {
      const name = frame.match(/^event:\s*(.+)$/m)?.[1] || "message";
      const data = frame.match(/^data:\s*(.+)$/m)?.[1];
      if (!data) continue;
      try { onEvent(name, JSON.parse(data) as AgentRunEvent); } catch { /* Skip malformed SSE frames. */ }
    }
    if (done) break;
  }
};
