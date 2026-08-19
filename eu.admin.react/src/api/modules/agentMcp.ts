import http from "@/api";

export type McpTransportKind = "StreamableHttp" | "Sse" | "Stdio";
export type McpServerStatus = "NotSynced" | "Healthy" | "Unhealthy" | "Disabled" | "Archived";
export type McpToolRisk = "Unknown" | "ReadOnly" | "Mutating" | "HighRisk";

export interface McpToolVersion {
  Id: string;
  ServerId: string;
  Name: string;
  Description: string;
  InputSchemaJson: string;
  Risk: McpToolRisk;
  Sha256: string;
  DiscoveredAtUtc: string;
}

export interface McpServerDefinition {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  Transport: McpTransportKind;
  Endpoint: string;
  Command: string;
  Arguments: string[];
  CredentialAlias: string;
  Enabled: boolean;
  LogicalRevision: number;
  Status: McpServerStatus;
  LastError: string;
  LastSyncedAtUtc?: string | null;
  CurrentToolVersionIds: string[];
  ToolVersions: McpToolVersion[];
}

export interface McpServerInput {
  code: string;
  name: string;
  description: string;
  transport: McpTransportKind;
  endpoint: string;
  command: string;
  arguments: string[];
  credentialAlias: string;
  enabled: boolean;
}

export interface UpdateMcpServerInput extends Omit<McpServerInput, "code"> {
  expectedLogicalRevision: number;
}

const mcpUrl = (path: string) => `/Agent/api/mcp${path}`;

export const getMcpServer = async (id: string) =>
  (await http.get<McpServerDefinition>(mcpUrl(`/servers/${encodeURIComponent(id)}`))).Data;

export const createMcpServer = async (input: McpServerInput) =>
  (await http.post<McpServerDefinition>(mcpUrl("/servers"), input)).Data;

export const updateMcpServer = async (id: string, input: UpdateMcpServerInput) =>
  (await http.put<McpServerDefinition>(mcpUrl(`/servers/${encodeURIComponent(id)}`), input)).Data;

export const syncMcpServer = async (id: string, expectedLogicalRevision: number) =>
  (
    await http.post<McpServerDefinition>(mcpUrl(`/servers/${encodeURIComponent(id)}/sync`), {
      expectedLogicalRevision
    })
  ).Data;

export const setMcpServerArchived = async (id: string, expectedLogicalRevision: number, archived: boolean) =>
  (
    await http.put<McpServerDefinition>(mcpUrl(`/servers/${encodeURIComponent(id)}/archive`), {
      expectedLogicalRevision,
      archived
    })
  ).Data;

export const classifyMcpTool = async (
  serverId: string,
  toolVersionId: string,
  expectedLogicalRevision: number,
  risk: McpToolRisk
) =>
  (
    await http.put<McpServerDefinition>(
      mcpUrl(`/servers/${encodeURIComponent(serverId)}/tools/${encodeURIComponent(toolVersionId)}/risk`),
      { expectedLogicalRevision, risk }
    )
  ).Data;
