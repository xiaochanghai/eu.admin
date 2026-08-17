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

export function parseServiceResponse(payload, httpStatus, fallbackMessage) {
  const isObject = payload !== null && typeof payload === "object" && !Array.isArray(payload);
  const hasRequiredShape = isObject
    && Number.isInteger(payload.Status)
    && typeof payload.Success === "boolean"
    && typeof payload.Message === "string"
    && Object.prototype.hasOwnProperty.call(payload, "Data");

  if (!hasRequiredShape) {
    const error = new Error(fallbackMessage);
    error.status = httpStatus;
    error.code = "SERVICE_RESPONSE_INVALID";
    throw error;
  }

  const httpSucceeded = httpStatus >= 200 && httpStatus < 300;
  if (!httpSucceeded || !payload.Success || payload.Status !== 200) {
    const error = new Error(payload.Message || fallbackMessage);
    error.status = httpStatus;
    error.businessStatus = payload.Status;
    error.errorCode = payload.Data?.ErrorCode;
    error.traceId = payload.Data?.TraceId;
    throw error;
  }

  return payload.Data;
}

export async function requestServiceJson(path, options = {}) {
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
  if (response.status === 204) return null;

  let payload;
  try { payload = await response.json(); } catch { payload = null; }
  return parseServiceResponse(payload, response.status, `请求失败 (${response.status})`);
}
