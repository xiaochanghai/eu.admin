const base = "/api/mcp";

async function request(path, options = {}) {
  const response = await fetch(path, {
    headers: { Accept: "application/json", ...(options.body ? { "Content-Type": "application/json" } : {}) },
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
  return response.status === 204 ? null : response.json();
}

export const mcpApi = {
  list: ({ search = "", status = "" } = {}) => {
    const query = new URLSearchParams();
    if (search) query.set("search", search);
    if (status) query.set("status", status);
    const suffix = query.toString();
    return request(`${base}/servers${suffix ? `?${suffix}` : ""}`);
  },
  get: id => request(`${base}/servers/${encodeURIComponent(id)}`),
  create: body => request(`${base}/servers`, { method: "POST", body: JSON.stringify(body) }),
  update: (id, body) => request(`${base}/servers/${encodeURIComponent(id)}`, { method: "PUT", body: JSON.stringify(body) }),
  sync: (id, expectedLogicalRevision) => request(`${base}/servers/${encodeURIComponent(id)}/sync`, {
    method: "POST", body: JSON.stringify({ expectedLogicalRevision })
  }),
  setArchived: (id, expectedLogicalRevision, archived) => request(
    `${base}/servers/${encodeURIComponent(id)}/archive`, {
      method: "PUT", body: JSON.stringify({ expectedLogicalRevision, archived })
    }),
  classify: (serverId, toolVersionId, body) => request(
    `${base}/servers/${encodeURIComponent(serverId)}/tools/${encodeURIComponent(toolVersionId)}/risk`,
    { method: "PUT", body: JSON.stringify(body) }),
  toolVersions: () => request(`${base}/tool-versions`)
};
