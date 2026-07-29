const base = "/api/agents";

async function request(path, options = {}) {
  const response = await fetch(path, {
    headers: { Accept: "application/json", ...(options.body ? { "Content-Type": "application/json" } : {}), ...options.headers },
    ...options
  });
  if (!response.ok) {
    let problem = {};
    try { problem = await response.json(); } catch { problem = {}; }
    const error = new Error(problem.detail || problem.title || `请求失败 (${response.status})`);
    error.status = response.status;
    error.errorCode = problem.errorCode;
    error.traceId = problem.traceId;
    throw error;
  }
  if (response.status === 204) return null;
  return response.json();
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
      let problem = {};
      try { problem = await response.json(); } catch { problem = {}; }
      const error = new Error(problem.detail || problem.title || `导出失败 (${response.status})`);
      error.status = response.status;
      error.errorCode = problem.errorCode;
      throw error;
    }
    return response.blob();
  },
  importPackage: json => request(`${base}/import`, { method: "POST", body: json }),
  publishedSkills: () => request("/api/skill-versions"),
  publishedTools: () => request("/api/mcp/tool-versions"),
  knowledgeReferences: () => request("/api/knowledge-base-references"),
  knowledgeBases: () => request("/api/knowledge-bases"),
  knowledgeBase: id => request(`/api/knowledge-bases/${encodeURIComponent(id)}`),
  createKnowledgeBase: body => request("/api/knowledge-bases", { method: "POST", body: JSON.stringify(body) }),
  updateKnowledgeBase: (id, body) => request(`/api/knowledge-bases/${encodeURIComponent(id)}`, { method: "PUT", body: JSON.stringify(body) }),
  importKnowledgeDocument: (id, body) => request(`/api/knowledge-bases/${encodeURIComponent(id)}/documents`, { method: "POST", body: JSON.stringify(body) }),
  searchKnowledge: (id, query, take = 6) => request(`/api/knowledge-bases/${encodeURIComponent(id)}/search`, {
    method: "POST", body: JSON.stringify({ query, take })
  }),
  orchestrations: () => request("/api/orchestrations"),
  orchestration: id => request(`/api/orchestrations/${encodeURIComponent(id)}`),
  createOrchestration: body => request("/api/orchestrations", { method: "POST", body: JSON.stringify(body) }),
  saveOrchestration: (id, body) => request(`/api/orchestrations/${encodeURIComponent(id)}/draft`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  publishOrchestration: (id, expectedLogicalRevision) => request(`/api/orchestrations/${encodeURIComponent(id)}/publish`, {
    method: "POST", body: JSON.stringify({ expectedLogicalRevision })
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
  runHistory: (id, take = 20) => request(`${base}/${encodeURIComponent(id)}/runs?take=${encodeURIComponent(take)}`),
  run: async (id, input, onEvent, signal) => {
    const response = await fetch(`${base}/${encodeURIComponent(id)}/runs`, {
      method: "POST",
      headers: { Accept: "text/event-stream", "Content-Type": "application/json" },
      body: JSON.stringify({ input }),
      signal
    });
    if (!response.ok) {
      let problem = {};
      try { problem = await response.json(); } catch { problem = {}; }
      const error = new Error(problem.detail || problem.title || `运行失败 (${response.status})`);
      error.status = response.status;
      error.errorCode = problem.errorCode;
      throw error;
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
