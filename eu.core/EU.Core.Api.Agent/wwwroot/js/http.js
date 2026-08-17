export async function createApiError(response, fallbackMessage) {
  let payload = {};
  try { payload = await response.json(); } catch { payload = {}; }

  const serviceResponse = payload !== null
    && typeof payload === "object"
    && !Array.isArray(payload)
    && Number.isInteger(payload.Status)
    && typeof payload.Success === "boolean"
    && Object.prototype.hasOwnProperty.call(payload, "Data");
  if (serviceResponse) {
    const error = new Error(payload.Message || fallbackMessage);
    error.status = response.status;
    error.businessStatus = payload.Status;
    error.errorCode = payload.Data?.ErrorCode;
    error.traceId = payload.Data?.TraceId;
    return error;
  }

  const error = new Error(payload.detail || payload.title || fallbackMessage);
  error.status = response.status;
  error.errorCode = payload.errorCode || payload.code;
  error.traceId = payload.traceId || payload.correlationId;
  return error;
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
  if (response.status === 204) return null;

  let payload;
  try { payload = await response.json(); } catch { payload = null; }
  return parseServiceResponse(payload, response.status, `请求失败 (${response.status})`);
}
