import http from "@/api";

const agentUrl = (path: string) => `/Agent${path}`;
const encoded = (value: string) => encodeURIComponent(value);

type UnknownRecord = Record<string, unknown>;

const asRecord = (value: unknown): UnknownRecord | null =>
  typeof value === "object" && value !== null ? (value as UnknownRecord) : null;

const parseRecord = (value: unknown): UnknownRecord | null => {
  const record = asRecord(value);
  if (record || typeof value !== "string") return record;
  try {
    return asRecord(JSON.parse(value));
  } catch {
    return null;
  }
};

export const getAgentSkillErrorMessage = (error: unknown, fallback: string) => {
  const root = asRecord(error);
  const response = asRecord(root?.response);
  const responseData = parseRecord(response?.data);
  const envelope = responseData ?? root;
  const data = asRecord(envelope?.Data ?? envelope?.data);
  const serverMessage = envelope?.Message ?? envelope?.message;
  const errorCode = data?.ErrorCode ?? data?.errorCode;
  const messageText = typeof serverMessage === "string" && serverMessage.trim() ? serverMessage : fallback;
  return typeof errorCode === "string" && errorCode ? `${messageText} · ${errorCode}` : messageText;
};

export interface SkillFileEntry {
  Path: string;
  Size: number;
}

export interface SkillFileHash extends SkillFileEntry {
  Sha256: string;
}

export interface SkillPublishedVersion {
  Id: string;
  Label: string;
  ManifestSha256: string;
  PublishedAtUtc: string;
  Files: SkillFileHash[];
  BoundAgents: Array<{ Id: string; Code: string; Name: string }>;
}

export interface SkillDefinition {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  Category: string;
  Status: "Active" | "Archived";
  DraftRevision: number;
  PublishedVersions: SkillPublishedVersion[];
}

export const getSkill = async (id: string) => (await http.get<SkillDefinition>(agentUrl(`/api/skills/${encoded(id)}`))).Data;

export const createSkill = async (input: { code: string; name: string; description: string; category: string }) =>
  (await http.post<SkillDefinition>(agentUrl("/api/skills"), input)).Data;

export const updateSkill = async (
  id: string,
  input: { expectedDraftRevision: number; name: string; description: string; category: string }
) => (await http.put<SkillDefinition>(agentUrl(`/api/skills/${encoded(id)}`), input)).Data;

export const listSkillFiles = async (id: string) =>
  (await http.get<SkillFileEntry[]>(agentUrl(`/api/skills/${encoded(id)}/files`))).Data;

export const readSkillFile = async (id: string, path: string) =>
  (await http.service.get<string>(agentUrl(`/api/skills/${encoded(id)}/files/content`), {
    params: { path },
    responseType: "text"
  })) as unknown as string;

export const saveSkillFile = async (id: string, input: { expectedDraftRevision: number; path: string; content: string }) =>
  (await http.put<SkillDefinition>(agentUrl(`/api/skills/${encoded(id)}/files/content`), input)).Data;

export const deleteSkillFile = async (id: string, input: { expectedDraftRevision: number; path: string }) =>
  (await http.delete<SkillDefinition>(agentUrl(`/api/skills/${encoded(id)}/files/content`), input)).Data;

export const publishSkill = async (id: string, expectedDraftRevision: number, versionLabel: string) =>
  (
    await http.post<SkillDefinition>(agentUrl(`/api/skills/${encoded(id)}/publish`), {
      expectedDraftRevision,
      versionLabel
    })
  ).Data;

export const archiveSkill = async (id: string, expectedDraftRevision: number, archived: boolean) =>
  (
    await http.put<SkillDefinition>(agentUrl(`/api/skills/${encoded(id)}/archive`), {
      expectedDraftRevision,
      archived
    })
  ).Data;
