import http from "@/api";

export type OrchestrationStatus = "Enabled" | "Disabled" | "Archived";
export type OrchestrationNodeInputMode = "InitialInput" | "PreviousOutput" | "Template";
export type OrchestrationEdgeCondition = "Always" | "Succeeded" | "Failed" | "OutputContains";
export type OrchestrationRunStatus = "Running" | "Completed" | "Failed" | "Cancelled";
export type OrchestrationNodeRunStatus = "Pending" | "Running" | "Completed" | "Failed" | "Cancelled";

export interface OrchestrationNode {
  Id: string;
  Name: string;
  AgentId: string;
  InputMode: OrchestrationNodeInputMode;
  InputTemplate: string;
  MaximumRetries: number;
  TimeoutSeconds: number;
}

export interface OrchestrationEdge {
  FromNodeId: string;
  ToNodeId: string;
  Condition: OrchestrationEdgeCondition;
  ConditionValue: string;
  Order: number;
}

export interface OrchestrationVersion {
  Id: string;
  Label: string;
  IsDraft: boolean;
  StartNodeId: string;
  Nodes: OrchestrationNode[];
  Edges: OrchestrationEdge[];
}

export interface OrchestrationDefinition {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  Status: OrchestrationStatus;
  LogicalRevision: number;
  Draft: OrchestrationVersion;
  PublishedVersions: OrchestrationVersion[];
}

export interface OrchestrationListItem {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  Status: OrchestrationStatus;
  LogicalRevision: number;
  DraftNodeCount: number;
  CurrentPublishedLabel?: string | null;
}

export interface SaveOrchestrationInput {
  expectedLogicalRevision: number;
  name: string;
  description: string;
  status: OrchestrationStatus;
  startNodeId: string;
  nodes: Array<{
    id: string;
    name: string;
    agentId: string;
    inputMode: OrchestrationNodeInputMode;
    inputTemplate: string;
    maximumRetries: number;
    timeoutSeconds: number;
  }>;
  edges: Array<{
    fromNodeId: string;
    toNodeId: string;
    condition: OrchestrationEdgeCondition;
    conditionValue: string;
    order: number;
  }>;
}

export interface OrchestrationNodeRunRecord {
  NodeId: string;
  NodeName: string;
  AgentId: string;
  AgentVersionId: string;
  Status: OrchestrationNodeRunStatus;
  Attempts: number;
  StartedAtUtc?: string | null;
  FinishedAtUtc?: string | null;
  OutputCharacters: number;
  InputSha256: string;
  ErrorCode: string;
}

export interface OrchestrationRunRecord {
  Id: string;
  OrchestrationId: string;
  OrchestrationVersionId: string;
  OrchestrationCode: string;
  Status: OrchestrationRunStatus;
  StartedAtUtc: string;
  FinishedAtUtc?: string | null;
  InputSha256: string;
  ErrorCode: string;
  Nodes: OrchestrationNodeRunRecord[];
}

export interface OrchestrationToolCallRecord {
  ToolCallId: string;
  AgentRunId: string;
  ToolVersionId: string;
  ToolName: string;
  Status: string;
  ArgumentsJson: string;
  ResultContent: string;
  ResultSha256: string;
  ResultCharacters: number;
  StartedAtUtc: string;
  FinishedAtUtc?: string | null;
  ErrorCode: string;
}

export interface OrchestrationNodeAttemptRecord {
  NodeId: string;
  Attempt: number;
  AgentRunId: string;
  Input: string;
  InputSha256: string;
  Output: string;
  OutputSha256: string;
  Status: OrchestrationNodeRunStatus;
  StartedAtUtc: string;
  FinishedAtUtc?: string | null;
  ErrorCode: string;
  ToolCalls: OrchestrationToolCallRecord[];
}

export interface OrchestrationRunDetails {
  RunId: string;
  OrchestrationId: string;
  Input: string;
  Output: string;
  Attempts: OrchestrationNodeAttemptRecord[];
}

const orchestrationUrl = (path = "") => `/Agent/api/orchestrations${path}`;
const encoded = (value: string) => encodeURIComponent(value);

export const getOrchestrationErrorMessage = (error: unknown, fallback: string) => {
  if (typeof error === "object" && error !== null) {
    const value = error as {
      Message?: string;
      message?: string;
      response?: { data?: { Message?: string; message?: string } };
    };
    return value.response?.data?.Message || value.response?.data?.message || value.Message || value.message || fallback;
  }
  return fallback;
};

export const listOrchestrationDefinitions = async (status?: OrchestrationStatus) =>
  (await http.get<OrchestrationListItem[]>(orchestrationUrl(), status ? { status } : undefined)).Data;

export const getOrchestration = async (id: string) =>
  (await http.get<OrchestrationDefinition>(orchestrationUrl(`/${encoded(id)}`))).Data;

export const createOrchestration = async (input: { code: string; name: string; description: string }) =>
  (await http.post<OrchestrationDefinition>(orchestrationUrl(), input)).Data;

export const saveOrchestrationDraft = async (id: string, input: SaveOrchestrationInput) =>
  (await http.put<OrchestrationDefinition>(orchestrationUrl(`/${encoded(id)}/draft`), input)).Data;

export const publishOrchestration = async (id: string, expectedLogicalRevision: number) =>
  (
    await http.post<OrchestrationDefinition>(orchestrationUrl(`/${encoded(id)}/publish`), {
      expectedLogicalRevision
    })
  ).Data;

export const setOrchestrationArchived = async (id: string, expectedLogicalRevision: number, archived: boolean) =>
  (
    await http.put<OrchestrationDefinition>(orchestrationUrl(`/${encoded(id)}/archive`), {
      expectedLogicalRevision,
      archived
    })
  ).Data;

export const startOrchestrationRun = async (id: string, input: string) =>
  (await http.post<OrchestrationRunRecord>(orchestrationUrl(`/${encoded(id)}/runs`), { input })).Data;

export const getOrchestrationRun = async (id: string, runId: string) =>
  (await http.get<OrchestrationRunRecord>(orchestrationUrl(`/${encoded(id)}/runs/${encoded(runId)}`))).Data;

export const getOrchestrationRunDetails = async (id: string, runId: string) =>
  (
    await http.get<OrchestrationRunDetails>(
      orchestrationUrl(`/${encoded(id)}/runs/${encoded(runId)}/details`)
    )
  ).Data;

export const cancelOrchestrationRun = async (id: string, runId: string) =>
  (await http.post<{ RunId: string }>(orchestrationUrl(`/${encoded(id)}/runs/${encoded(runId)}/cancel`))).Data;
