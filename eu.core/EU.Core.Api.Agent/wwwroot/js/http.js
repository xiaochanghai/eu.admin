export async function createApiError(response, fallbackMessage) {
  let problem = {};
  try { problem = await response.json(); } catch { problem = {}; }

  const error = new Error(problem.detail || problem.title || fallbackMessage);
  error.status = response.status;
  error.errorCode = problem.errorCode || problem.code;
  error.traceId = problem.traceId || problem.correlationId;
  return error;
}

export async function requestJson(path, options = {}) {
  const { headers = {}, body, ...requestOptions } = options;
  const isFormData = typeof FormData !== "undefined" && body instanceof FormData;
  const response = await fetch(path, {
    ...requestOptions,
    body,
    headers: {
      Accept: "application/json",
      ...(body && !isFormData ? { "Content-Type": "application/json" } : {}),
      ...headers
    }
  });
  if (!response.ok) {
    throw await createApiError(response, `请求失败 (${response.status})`);
  }
  return response.status === 204 ? null : response.json();
}
