import { parseServiceResponse, requestJson as request } from "./http.js";
import { authorizedFetch } from "./auth.js";

const base = "/api/skills";

export const skillsApi = {
  list: ({ search = "", category = "", status = "" } = {}) => {
    const query = new URLSearchParams();
    if (search) query.set("search", search);
    if (category) query.set("category", category);
    if (status) query.set("status", status);
    return request(`${base}${query.size ? `?${query}` : ""}`);
  },
  get: id => request(`${base}/${encodeURIComponent(id)}`),
  create: body => request(base, { method: "POST", body: JSON.stringify(body) }),
  update: (id, body) => request(`${base}/${encodeURIComponent(id)}`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  files: id => request(`${base}/${encodeURIComponent(id)}/files`),
  readFile: async (id, path) => {
    const response = await authorizedFetch(
      `${base}/${encodeURIComponent(id)}/files/content?path=${encodeURIComponent(path)}`,
      { headers: { Accept: "text/plain" } });
    if (!response.ok) {
      let payload;
      try { payload = await response.json(); } catch { payload = null; }
      parseServiceResponse(payload, response.status, `读取文件失败 (${response.status})`);
    }
    return response.text();
  },
  saveFile: (id, body) => request(`${base}/${encodeURIComponent(id)}/files/content`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  deleteFile: (id, body) => request(`${base}/${encodeURIComponent(id)}/files/content`, {
    method: "DELETE", body: JSON.stringify(body)
  }),
  publish: (id, body) => request(`${base}/${encodeURIComponent(id)}/publish`, {
    method: "POST", body: JSON.stringify(body)
  }),
  setArchived: (id, body) => request(`${base}/${encodeURIComponent(id)}/archive`, {
    method: "PUT", body: JSON.stringify(body)
  })
};
