import { createApiError, requestJson as request } from "./http.js";

const base = "/api/agents";

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
  const response = await fetch("/api/chat/runs", {
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
  capabilities: () => request("/api/platform/capabilities"),
  list: ({ search = "", status = "" } = {}) => {
    const query = new URLSearchParams();
    if (search) query.set("search", search);
    if (status) query.set("status", status);
    const suffix = query.toString();
    return request(`${base}${suffix ? `?${suffix}` : ""}`);
  },
  get: id => request(`${base}/${encodeURIComponent(id)}`),
  create: body => request(base, { method: "POST", body: JSON.stringify(body) }),
  saveDraft: (id, body) => request(`${base}/${encodeURIComponent(id)}/draft`, { method: "PUT", body: JSON.stringify(body) }),
  publish: (id, expectedLogicalRevision) => request(`${base}/${encodeURIComponent(id)}/publish`, {
    method: "POST", body: JSON.stringify({ expectedLogicalRevision })
  }),
  setStatus: (id, runtimeStatus, expectedLogicalRevision) => request(`${base}/${encodeURIComponent(id)}/status`, {
    method: "PUT", body: JSON.stringify({ runtimeStatus, expectedLogicalRevision })
  }),
  exportPackage: async id => {
    const response = await fetch(`${base}/${encodeURIComponent(id)}/export`, { headers: { Accept: "application/json" } });
    if (!response.ok) {
      throw await createApiError(response, `导出失败 (${response.status})`);
    }
    return response.blob();
  },
  importPackage: json => request(`${base}/import`, { method: "POST", body: json }),
  publishedSkills: () => request("/api/skill-versions"),
  publishedTools: () => request("/api/mcp/tool-versions"),
  knowledgeReferences: () => request("/api/knowledge-base-references"),
  knowledgeBases: (status = "") => request(`/api/knowledge-bases${status ? `?status=${encodeURIComponent(status)}` : ""}`),
  knowledgeBase: id => request(`/api/knowledge-bases/${encodeURIComponent(id)}`),
  knowledgeDocuments: id => request(`/api/knowledge-bases/${encodeURIComponent(id)}/documents`),
  knowledgeDocumentChunks: (id, documentId, skip = 0, take = 10) => request(
    `/api/knowledge-bases/${encodeURIComponent(id)}/documents/${encodeURIComponent(documentId)}/chunks?skip=${encodeURIComponent(skip)}&take=${encodeURIComponent(take)}`),
  createKnowledgeBase: body => request("/api/knowledge-bases", { method: "POST", body: JSON.stringify(body) }),
  updateKnowledgeBase: (id, body) => request(`/api/knowledge-bases/${encodeURIComponent(id)}`, { method: "PUT", body: JSON.stringify(body) }),
  setKnowledgeBaseArchived: (id, body) => request(`/api/knowledge-bases/${encodeURIComponent(id)}/archive`, { method: "PUT", body: JSON.stringify(body) }),
  importKnowledgeDocument: (id, body) => request(`/api/knowledge-bases/${encodeURIComponent(id)}/documents`, { method: "POST", body: JSON.stringify(body) }),
  importKnowledgePdf: (id, expectedLogicalRevision, file) => {
    const body = new FormData();
    body.set("expectedLogicalRevision", String(expectedLogicalRevision));
    body.set("file", file, file.name);
    return request(`/api/knowledge-bases/${encodeURIComponent(id)}/documents/pdf`, {
      method: "POST",
      body
    });
  },
  searchKnowledge: (id, query, take = 6) => request(`/api/knowledge-bases/${encodeURIComponent(id)}/search`, {
    method: "POST", body: JSON.stringify({ query, take })
  }),
  orchestrations: (status = "") => request(`/api/orchestrations${status ? `?status=${encodeURIComponent(status)}` : ""}`),
  orchestration: id => request(`/api/orchestrations/${encodeURIComponent(id)}`),
  createOrchestration: body => request("/api/orchestrations", { method: "POST", body: JSON.stringify(body) }),
  saveOrchestration: (id, body) => request(`/api/orchestrations/${encodeURIComponent(id)}/draft`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  publishOrchestration: (id, expectedLogicalRevision) => request(`/api/orchestrations/${encodeURIComponent(id)}/publish`, {
    method: "POST", body: JSON.stringify({ expectedLogicalRevision })
  }),
  setOrchestrationArchived: (id, body) => request(`/api/orchestrations/${encodeURIComponent(id)}/archive`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  startOrchestration: (id, input) => request(`/api/orchestrations/${encodeURIComponent(id)}/runs`, {
    method: "POST", body: JSON.stringify({ input })
  }),
  orchestrationRun: (id, runId) => request(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}`),
  orchestrationRunDetails: (id, runId) => request(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}/details`),
  cancelOrchestrationRun: (id, runId) => request(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}/cancel`, {
    method: "POST"
  }),
  orchestrationOutput: (id, runId) => request(`/api/orchestrations/${encodeURIComponent(id)}/runs/${encodeURIComponent(runId)}/output`),
  mainAgent: async () => {
    try {
      return await request("/api/platform/main-agent");
    } catch (error) {
      if (error.status === 404 && error.errorCode === "MAIN_AGENT_NOT_CONFIGURED") return null;
      throw error;
    }
  },
  setMainAgent: (agentId, expectedLogicalRevision) => request("/api/platform/main-agent", {
    method: "PUT",
    body: JSON.stringify({ agentId, expectedLogicalRevision })
  }),
  listChatConversations: (take = 40) => request(`/api/chat/conversations?take=${encodeURIComponent(take)}`),
  chatConversation: (conversationId, take = 160) => request(
    `/api/chat/conversations/${encodeURIComponent(conversationId)}?take=${encodeURIComponent(take)}`),
  chatConversationRuns: (conversationId, take = 20) => request(
    `/api/chat/conversations/${encodeURIComponent(conversationId)}/runs?take=${encodeURIComponent(take)}`),
  chatRun: runId => request(`/api/chat/runs/${encodeURIComponent(runId)}`),
  chatRunDetails: runId => request(`/api/chat/runs/${encodeURIComponent(runId)}/details`),
  chatRunEvents: (runId, take = 160) => request(
    `/api/chat/runs/${encodeURIComponent(runId)}/events?take=${encodeURIComponent(take)}`),
  cancelChatRun: runId => request(`/api/chat/runs/${encodeURIComponent(runId)}/cancel`, { method: "POST" }),
  toolApprovals: ({ status = "", take = 100 } = {}) => {
    const query = new URLSearchParams({ take: String(take) });
    if (status) query.set("status", status);
    return request(`/api/tool-approvals?${query}`);
  },
  toolApproval: id => request(`/api/tool-approvals/${encodeURIComponent(id)}`),
  approveToolApproval: (id, reason = "") => request(
    `/api/tool-approvals/${encodeURIComponent(id)}/approve`,
    { method: "POST", body: JSON.stringify({ reason }) }),
  rejectToolApproval: (id, reason = "") => request(
    `/api/tool-approvals/${encodeURIComponent(id)}/reject`,
    { method: "POST", body: JSON.stringify({ reason }) }),
  cancelToolApproval: (id, reason = "") => request(
    `/api/tool-approvals/${encodeURIComponent(id)}/cancel`,
    { method: "POST", body: JSON.stringify({ reason }) }),
  resumeToolApproval: id => request(
    `/api/tool-approvals/${encodeURIComponent(id)}/resume`,
    { method: "POST" }),
  evaluationSuites: (status = "") => request(
    `/api/evaluation-suites${status ? `?status=${encodeURIComponent(status)}` : ""}`),
  evaluationSuite: id => request(`/api/evaluation-suites/${encodeURIComponent(id)}`),
  createEvaluationSuite: body => request("/api/evaluation-suites", {
    method: "POST", body: JSON.stringify(body)
  }),
  saveEvaluationSuiteDraft: (id, body) => request(
    `/api/evaluation-suites/${encodeURIComponent(id)}/draft`, {
      method: "PUT", body: JSON.stringify(body)
    }),
  publishEvaluationSuite: (id, expectedLogicalRevision) => request(
    `/api/evaluation-suites/${encodeURIComponent(id)}/publish`, {
      method: "POST", body: JSON.stringify({ expectedLogicalRevision })
    }),
  archiveEvaluationSuite: (id, expectedLogicalRevision, archived) => request(
    `/api/evaluation-suites/${encodeURIComponent(id)}/archive`, {
      method: "PUT", body: JSON.stringify({ expectedLogicalRevision, archived })
    }),
  evaluationBatches: (suiteId, take = 50) => request(
    `/api/evaluation-batches?suiteId=${encodeURIComponent(suiteId)}&take=${encodeURIComponent(take)}`),
  evaluationBatch: id => request(`/api/evaluation-batches/${encodeURIComponent(id)}`),
  runEvaluationBatch: (suiteId, suiteVersionId) => request("/api/evaluation-batches", {
    method: "POST", body: JSON.stringify({ suiteId, suiteVersionId })
  }),
  compareEvaluationBatches: body => request("/api/evaluation-batches/compare", {
    method: "POST", body: JSON.stringify(body)
  }),
  modelJudgeReports: (batchId, take = 20) => request(
    `/api/evaluation-batches/${encodeURIComponent(batchId)}/model-judge-reports?take=${encodeURIComponent(take)}`),
  runModelJudge: (batchId, body) => request(
    `/api/evaluation-batches/${encodeURIComponent(batchId)}/model-judge`, {
      method: "POST", body: JSON.stringify(body)
    }),
  streamChatRun,
  runHistory: (id, take = 20) => request(`${base}/${encodeURIComponent(id)}/runs?take=${encodeURIComponent(take)}`),
  run: async (id, input, onEvent, signal) => {
    const response = await fetch(`${base}/${encodeURIComponent(id)}/runs`, {
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
