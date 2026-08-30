import {
  createApiError,
  parseServiceResponse,
  requestJson as serviceRequest
} from "./http.js";
import { authorizedFetch } from "./auth.js";

const base = "/api/agents";

function lowerDto(value) {
  if (Array.isArray(value)) return value.map(lowerDto);
  if (!value || typeof value !== "object") return value;
  return Object.fromEntries(Object.entries(value).map(([key, item]) => [
    key ? `${key[0].toLowerCase()}${key.slice(1)}` : key,
    lowerDto(item)
  ]));
}

export const platformCapabilitiesPresentation = value => lowerDto(value);
export const chatConversationPresentation = value => lowerDto(value);
export const chatMessagePresentation = value => lowerDto(value);
export const chatRunPresentation = value => lowerDto(value);
export const chatRunDetailsPresentation = value => lowerDto(value);
export const chatRunEventPresentation = value => lowerDto(value);
export const agentRunAuditPresentation = value => lowerDto(value);

function chatConversationDetailsPresentation(value) {
  return {
    conversation: chatConversationPresentation(value?.Conversation),
    messages: (value?.Messages ?? []).map(chatMessagePresentation)
  };
}

export function createSseParser(onEvent) {
  const decoder = new TextDecoder("utf-8");
  let buffer = "";
  let eventName = "";
  let eventId = "";
  let dataLines = [];
  let finished = false;

  function dispatch() {
    if (!dataLines.length) {
      eventName = "";
      return;
    }
    const data = dataLines.join("\n");
    const name = eventName || "message";
    eventName = "";
    dataLines = [];
    onEvent(name, JSON.parse(data), eventId);
  }

  function consumeLine(rawLine) {
    const line = rawLine.endsWith("\r") ? rawLine.slice(0, -1) : rawLine;
    if (!line) {
      dispatch();
      return;
    }
    if (line.startsWith(":")) return;
    const separator = line.indexOf(":");
    const field = separator < 0 ? line : line.slice(0, separator);
    let value = separator < 0 ? "" : line.slice(separator + 1);
    if (value.startsWith(" ")) value = value.slice(1);
    if (field === "event") eventName = value;
    if (field === "id" && !value.includes("\0")) eventId = value;
    if (field === "data") dataLines.push(value);
  }

  function parseAvailableLines() {
    let boundary;
    while ((boundary = buffer.indexOf("\n")) >= 0) {
      consumeLine(buffer.slice(0, boundary));
      buffer = buffer.slice(boundary + 1);
    }
  }

  return {
    push(chunk) {
      if (finished) throw new Error("The SSE parser has already finished.");
      buffer += decoder.decode(chunk, { stream: true });
      parseAvailableLines();
    },
    finish() {
      if (finished) return;
      finished = true;
      buffer += decoder.decode();
      parseAvailableLines();
      if (buffer.length) {
        consumeLine(buffer);
        buffer = "";
      }
      dispatch();
    }
  };
}

export async function streamChatRun({ input, conversationId, onOpen, onEvent, signal }) {
  const response = await authorizedFetch("/api/chat/runs", {
    method: "POST",
    headers: { Accept: "text/event-stream", "Content-Type": "application/json" },
    body: JSON.stringify({ input, ...(conversationId ? { conversationId } : {}) }),
    signal
  });
  if (!response.ok) {
    throw await createApiError(response, `运行失败 (${response.status})`);
  }
  onOpen?.({
    runId: response.headers.get("X-Agent-Run-ID"),
    conversationId: response.headers.get("X-Agent-Conversation-ID")
  });
  if (!response.body) throw new Error("运行响应不支持流式读取。");

  const parser = createSseParser(onEvent);
  const reader = response.body.getReader();
  let completed = false;
  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      parser.push(value);
    }
    parser.finish();
    completed = true;
  } finally {
    if (!completed) {
      try { await reader.cancel(); } catch { /* The connection is already closed. */ }
    }
    reader.releaseLock();
  }
}

export const agentApi = {
  capabilities: async () => platformCapabilitiesPresentation(
    await serviceRequest("/api/platform/capabilities")),
  list: ({ search = "", status = "" } = {}) => {
    const query = new URLSearchParams();
    if (search) query.set("search", search);
    if (status) query.set("status", status);
    const suffix = query.toString();
    return serviceRequest(`${base}${suffix ? `?${suffix}` : ""}`);
  },
  get: id => serviceRequest(`${base}/${encodeURIComponent(id)}`),
  create: body => serviceRequest(base, { method: "POST", body: JSON.stringify(body) }),
  saveDraft: (id, body) => serviceRequest(`${base}/${encodeURIComponent(id)}/draft`, { method: "PUT", body: JSON.stringify(body) }),
  publish: (id, expectedLogicalRevision) => serviceRequest(`${base}/${encodeURIComponent(id)}/publish`, {
    method: "POST", body: JSON.stringify({ expectedLogicalRevision })
  }),
  setStatus: (id, runtimeStatus, expectedLogicalRevision) => serviceRequest(`${base}/${encodeURIComponent(id)}/status`, {
    method: "PUT", body: JSON.stringify({ runtimeStatus, expectedLogicalRevision })
  }),
  exportPackage: async id => {
    const response = await authorizedFetch(`${base}/${encodeURIComponent(id)}/export`, { headers: { Accept: "application/json" } });
    const blob = await response.blob();
    let payload = null;
    try { payload = JSON.parse(await blob.text()); } catch { /* The export may be a non-JSON transport error. */ }
    const isServiceResponse = payload !== null
      && typeof payload === "object"
      && !Array.isArray(payload)
      && Number.isInteger(payload.Status)
      && typeof payload.Success === "boolean"
      && Object.prototype.hasOwnProperty.call(payload, "Data");
    if (isServiceResponse) {
      parseServiceResponse(payload, response.status, `导出失败 (${response.status})`);
    }
    if (!response.ok) {
      const error = new Error(`导出失败 (${response.status})`);
      error.status = response.status;
      throw error;
    }
    return blob;
  },
  importPackage: json => serviceRequest(`${base}/import`, { method: "POST", body: json }),
  publishedSkills: () => serviceRequest("/api/skill-versions"),
  publishedTools: () => serviceRequest("/api/mcp/tool-versions"),
  knowledgeReferences: () => serviceRequest("/api/knowledge-base-references"),
  knowledgeBases: (status = "") => serviceRequest(`/api/knowledge-bases${status ? `?status=${encodeURIComponent(status)}` : ""}`),
  knowledgeBase: id => serviceRequest(`/api/knowledge-bases/${encodeURIComponent(id)}`),
  knowledgeDocuments: id => serviceRequest(`/api/knowledge-bases/${encodeURIComponent(id)}/documents`),
  knowledgeDocumentChunks: (id, documentId, skip = 0, take = 10) => serviceRequest(
    `/api/knowledge-bases/${encodeURIComponent(id)}/documents/${encodeURIComponent(documentId)}/chunks?skip=${encodeURIComponent(skip)}&take=${encodeURIComponent(take)}`),
  createKnowledgeBase: body => serviceRequest("/api/knowledge-bases", { method: "POST", body: JSON.stringify(body) }),
  updateKnowledgeBase: (id, body) => serviceRequest(`/api/knowledge-bases/${encodeURIComponent(id)}`, { method: "PUT", body: JSON.stringify(body) }),
  setKnowledgeBaseArchived: (id, body) => serviceRequest(`/api/knowledge-bases/${encodeURIComponent(id)}/archive`, { method: "PUT", body: JSON.stringify(body) }),
  importKnowledgeDocument: (id, body) => serviceRequest(`/api/knowledge-bases/${encodeURIComponent(id)}/documents`, { method: "POST", body: JSON.stringify(body) }),
  importKnowledgePdf: (id, expectedLogicalRevision, file) => {
    const body = new FormData();
    body.set("expectedLogicalRevision", String(expectedLogicalRevision));
    body.set("file", file, file.name);
    return serviceRequest(`/api/knowledge-bases/${encodeURIComponent(id)}/documents/pdf`, {
      method: "POST",
      body
    });
  },
  searchKnowledge: (id, query, take = 6) => serviceRequest(`/api/knowledge-bases/${encodeURIComponent(id)}/search`, {
    method: "POST", body: JSON.stringify({ query, take })
  }),
  orchestrations: (status = "") => serviceRequest(`/api/orchestrations${status ? `?status=${encodeURIComponent(status)}` : ""}`),
  orchestration: id => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}`),
  createOrchestration: body => serviceRequest("/api/orchestrations", { method: "POST", body: JSON.stringify(body) }),
  saveOrchestration: (id, body) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/draft`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  publishOrchestration: (id, expectedLogicalRevision) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/publish`, {
    method: "POST", body: JSON.stringify({ expectedLogicalRevision })
  }),
  setOrchestrationArchived: (id, body) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/archive`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  startOrchestration: (id, input) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/runs`, {
    method: "POST", body: JSON.stringify({ input })
  }),
  orchestrationRun: (id, runId) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}`),
  orchestrationRunDetails: (id, runId) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}/details`),
  cancelOrchestrationRun: (id, runId) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}/cancel`, {
    method: "POST"
  }),
  orchestrationOutput: (id, runId) => serviceRequest(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}/output`),
  mainAgent: async () => {
    try {
      return await serviceRequest("/api/platform/main-agent");
    } catch (error) {
      if (error.status === 404 && error.errorCode === "MAIN_AGENT_NOT_CONFIGURED") return null;
      throw error;
    }
  },
  setMainAgent: (agentId, expectedLogicalRevision) => serviceRequest("/api/platform/main-agent", {
    method: "PUT",
    body: JSON.stringify({ agentId, expectedLogicalRevision })
  }),
  listChatConversations: async (take = 40) => (
    await serviceRequest(`/api/chat/conversations?take=${encodeURIComponent(take)}`)
  ).map(chatConversationPresentation),
  chatConversation: async (conversationId, take = 160) => chatConversationDetailsPresentation(
    await serviceRequest(
      `/api/chat/conversations/${encodeURIComponent(conversationId)}?take=${encodeURIComponent(take)}`)),
  chatConversationRuns: async (conversationId, take = 20) => (
    await serviceRequest(
      `/api/chat/conversations/${encodeURIComponent(conversationId)}/runs?take=${encodeURIComponent(take)}`)
  ).map(chatRunPresentation),
  chatRun: async runId => chatRunPresentation(
    await serviceRequest(`/api/chat/runs/${encodeURIComponent(runId)}`)),
  chatRunDetails: async runId => chatRunDetailsPresentation(
    await serviceRequest(`/api/chat/runs/${encodeURIComponent(runId)}/details`)),
  chatRunEvents: async (runId, take = 160) => (
    await serviceRequest(
      `/api/chat/runs/${encodeURIComponent(runId)}/events?take=${encodeURIComponent(take)}`)
  ).map(chatRunEventPresentation),
  cancelChatRun: async runId => lowerDto(await serviceRequest(
    `/api/chat/runs/${encodeURIComponent(runId)}/cancel`, { method: "POST" })),
  toolApprovals: ({ status = "", take = 100 } = {}) => {
    const query = new URLSearchParams({ take: String(take) });
    if (status) query.set("status", status);
    return serviceRequest(`/api/tool-approvals?${query}`);
  },
  toolApproval: id => serviceRequest(`/api/tool-approvals/${encodeURIComponent(id)}`),
  approveToolApproval: (id, reason = "") => serviceRequest(
    `/api/tool-approvals/${encodeURIComponent(id)}/approve`,
    { method: "POST", body: JSON.stringify({ reason }) }),
  rejectToolApproval: (id, reason = "") => serviceRequest(
    `/api/tool-approvals/${encodeURIComponent(id)}/reject`,
    { method: "POST", body: JSON.stringify({ reason }) }),
  cancelToolApproval: (id, reason = "") => serviceRequest(
    `/api/tool-approvals/${encodeURIComponent(id)}/cancel`,
    { method: "POST", body: JSON.stringify({ reason }) }),
  resumeToolApproval: id => serviceRequest(
    `/api/tool-approvals/${encodeURIComponent(id)}/resume`,
    { method: "POST" }),
  evaluationSuites: (status = "") => serviceRequest(
    `/api/evaluation-suites${status ? `?status=${encodeURIComponent(status)}` : ""}`),
  evaluationSuite: id => serviceRequest(`/api/evaluation-suites/${encodeURIComponent(id)}`),
  createEvaluationSuite: body => serviceRequest("/api/evaluation-suites", {
    method: "POST", body: JSON.stringify(body)
  }),
  saveEvaluationSuiteDraft: (id, body) => serviceRequest(
    `/api/evaluation-suites/${encodeURIComponent(id)}/draft`, {
      method: "PUT", body: JSON.stringify(body)
    }),
  publishEvaluationSuite: (id, expectedLogicalRevision) => serviceRequest(
    `/api/evaluation-suites/${encodeURIComponent(id)}/publish`, {
      method: "POST", body: JSON.stringify({ expectedLogicalRevision })
    }),
  archiveEvaluationSuite: (id, expectedLogicalRevision, archived) => serviceRequest(
    `/api/evaluation-suites/${encodeURIComponent(id)}/archive`, {
      method: "PUT", body: JSON.stringify({ expectedLogicalRevision, archived })
    }),
  evaluationBatches: (suiteId, take = 50) => serviceRequest(
    `/api/evaluation-batches?suiteId=${encodeURIComponent(suiteId)}&take=${encodeURIComponent(take)}`),
  evaluationBatch: id => serviceRequest(`/api/evaluation-batches/${encodeURIComponent(id)}`),
  runEvaluationBatch: (suiteId, suiteVersionId) => serviceRequest("/api/evaluation-batches", {
    method: "POST", body: JSON.stringify({ suiteId, suiteVersionId })
  }),
  compareEvaluationBatches: body => serviceRequest("/api/evaluation-batches/compare", {
    method: "POST", body: JSON.stringify(body)
  }),
  modelJudgeReports: (batchId, take = 20) => serviceRequest(
    `/api/evaluation-batches/${encodeURIComponent(batchId)}/model-judge-reports?take=${encodeURIComponent(take)}`),
  modelJudgeReport: (batchId, reportId) => serviceRequest(
    `/api/evaluation-batches/${encodeURIComponent(batchId)}/model-judge-reports/${encodeURIComponent(reportId)}`),
  runModelJudge: (batchId, body) => serviceRequest(
    `/api/evaluation-batches/${encodeURIComponent(batchId)}/model-judge`, {
      method: "POST", body: JSON.stringify(body)
    }),
  evaluateRun: (runId, body) => serviceRequest(
    `/api/evaluations/runs/${encodeURIComponent(runId)}`, {
      method: "POST", body: JSON.stringify(body)
    }),
  streamChatRun,
  runHistory: async (id, take = 20) => (
    await serviceRequest(`${base}/${encodeURIComponent(id)}/runs?take=${encodeURIComponent(take)}`)
  ).map(agentRunAuditPresentation),
  run: async (id, input, onEvent, signal) => {
    const response = await authorizedFetch(`${base}/${encodeURIComponent(id)}/runs`, {
      method: "POST",
      headers: { Accept: "text/event-stream", "Content-Type": "application/json" },
      body: JSON.stringify({ input }),
      signal
    });
    if (!response.ok) {
      throw await createApiError(response, `运行失败 (${response.status})`);
    }
    if (!response.body) throw new Error("运行响应不支持流式读取。");
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";
    while (true) {
      const { value, done } = await reader.read();
      buffer += decoder.decode(value || new Uint8Array(), { stream: !done }).replace(/\r/g, "");
      let boundary;
      while ((boundary = buffer.indexOf("\n\n")) >= 0) {
        const block = buffer.slice(0, boundary);
        buffer = buffer.slice(boundary + 2);
        const eventLine = block.split("\n").find(line => line.startsWith("event: "));
        const dataLines = block.split("\n").filter(line => line.startsWith("data: "));
        if (eventLine && dataLines.length) {
          onEvent(eventLine.slice(7), JSON.parse(dataLines.map(line => line.slice(6)).join("\n")));
        }
      }
      if (done) break;
    }
  }
};
