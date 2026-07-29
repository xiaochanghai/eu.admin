const base = "/api/skills";

async function request(path, options = {}) {
  const response = await fetch(path, {
    headers: {
      Accept: "application/json",
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...options.headers
    },
    ...options
  });
  if (!response.ok) {
    let problem = {};
    try { problem = await response.json(); } catch { problem = {}; }
    const error = new Error(problem.title || `请求失败 (${response.status})`);
    error.status = response.status;
    error.errorCode = problem.errorCode;
    throw error;
  }
  return response.status === 204 ? null : response.json();
}

export const skillsApi = {
  list: ({ search = "", category = "" } = {}) => {
    const query = new URLSearchParams();
    if (search) query.set("search", search);
    if (category) query.set("category", category);
    return request(`${base}${query.size ? `?${query}` : ""}`);
  },
  get: id => request(`${base}/${encodeURIComponent(id)}`),
  create: body => request(base, { method: "POST", body: JSON.stringify(body) }),
  update: (id, body) => request(`${base}/${encodeURIComponent(id)}`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  files: id => request(`${base}/${encodeURIComponent(id)}/files`),
  readFile: (id, path) => request(
    `${base}/${encodeURIComponent(id)}/files/content?path=${encodeURIComponent(path)}`),
  saveFile: (id, body) => request(`${base}/${encodeURIComponent(id)}/files/content`, {
    method: "PUT", body: JSON.stringify(body)
  }),
  publish: (id, body) => request(`${base}/${encodeURIComponent(id)}/publish`, {
    method: "POST", body: JSON.stringify(body)
  })
};
