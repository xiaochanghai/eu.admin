import http from "@/api";

export type KnowledgeStatus = "Enabled" | "Disabled" | "Archived";

export interface KnowledgeListItem {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  Status: KnowledgeStatus;
  LogicalRevision: number;
  DocumentCount: number;
  ChunkCount: number;
  IndexedAtUtc?: string | null;
}

export type KnowledgeDetail = KnowledgeListItem;

export interface KnowledgeDocument {
  Id: string;
  FileName: string;
  MediaType: string;
  Sha256: string;
  CharacterCount: number;
  ChunkCount: number;
  ImportedAtUtc: string;
}

export interface KnowledgeChunk {
  Id: string;
  Sequence: number;
  Content: string;
  CharacterCount: number;
}

export interface KnowledgeChunkPage {
  DocumentId: string;
  FileName: string;
  Skip: number;
  Take: number;
  TotalCount: number;
  Items: KnowledgeChunk[];
}

export interface KnowledgeSearchResult {
  KnowledgeBaseId: string;
  KnowledgeBaseCode: string;
  DocumentId: string;
  FileName: string;
  ChunkId: string;
  ChunkSequence: number;
  Content: string;
  Score: number;
}

export interface UpdateKnowledgeInput {
  expectedLogicalRevision: number;
  name: string;
  description: string;
  status: KnowledgeStatus;
}

const knowledgeUrl = (path = "") => `/Agent/api/knowledge-bases${path}`;
const encoded = (value: string) => encodeURIComponent(value);

export const getKnowledgeErrorMessage = (error: unknown, fallback: string) => {
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

export const listKnowledge = async (status?: KnowledgeStatus) =>
  (await http.get<KnowledgeListItem[]>(knowledgeUrl(), status ? { status } : undefined)).Data;

export const getKnowledge = async (id: string) =>
  (await http.get<KnowledgeDetail>(knowledgeUrl(`/${encoded(id)}`))).Data;

export const createKnowledge = async (input: { code: string; name: string; description: string }) =>
  (await http.post<KnowledgeDetail>(knowledgeUrl(), input)).Data;

export const updateKnowledge = async (id: string, input: UpdateKnowledgeInput) =>
  (await http.put<KnowledgeDetail>(knowledgeUrl(`/${encoded(id)}`), input)).Data;

export const setKnowledgeArchived = async (id: string, expectedLogicalRevision: number, archived: boolean) =>
  (
    await http.put<KnowledgeDetail>(knowledgeUrl(`/${encoded(id)}/archive`), {
      expectedLogicalRevision,
      archived
    })
  ).Data;

export const listKnowledgeDocuments = async (id: string) =>
  (await http.get<KnowledgeDocument[]>(knowledgeUrl(`/${encoded(id)}/documents`))).Data;

export const listKnowledgeChunks = async (id: string, documentId: string, skip = 0, take = 10) =>
  (
    await http.get<KnowledgeChunkPage>(knowledgeUrl(`/${encoded(id)}/documents/${encoded(documentId)}/chunks`), {
      skip,
      take
    })
  ).Data;

export const importKnowledgeText = async (
  id: string,
  input: { expectedLogicalRevision: number; fileName: string; mediaType: string; content: string }
) => (await http.post<KnowledgeDetail>(knowledgeUrl(`/${encoded(id)}/documents`), input)).Data;

export const importKnowledgePdf = async (id: string, expectedLogicalRevision: number, file: File) =>
  (
    await http.postForm<KnowledgeDetail>(knowledgeUrl(`/${encoded(id)}/documents/pdf`), {
      expectedLogicalRevision,
      file
    })
  ).Data;

export const deleteKnowledgeDocument = async (
  id: string,
  documentId: string,
  expectedLogicalRevision: number
) =>
  (
    await http.delete<KnowledgeDetail>(
      knowledgeUrl(`/${encoded(id)}/documents/${encoded(documentId)}`),
      { expectedLogicalRevision }
    )
  ).Data;

export const searchKnowledge = async (id: string, query: string, take = 6) =>
  (
    await http.post<KnowledgeSearchResult[]>(knowledgeUrl(`/${encoded(id)}/search`), {
      query,
      take
    })
  ).Data;
