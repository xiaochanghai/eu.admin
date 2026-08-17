import { requestServiceJson as request } from "./http.js";

const base = "/api/mcp";

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
