/** 仅将服务端确认的业务结果事件转换为聊天结果，不使用 tool-succeeded 调试载荷。 */
export const readBusinessQueryEvent = (event: { kind: string; payloadJson: string }) => {
  if (event.kind !== "business-query-result") return undefined;
  try {
    const payload: unknown = JSON.parse(event.payloadJson);
    if (!payload || typeof payload !== "object" || Array.isArray(payload)) return undefined;
    const queryId = "queryId" in payload ? payload.queryId : "QueryId" in payload ? payload.QueryId : undefined;
    const presentation = "presentation" in payload ? payload.presentation : "Presentation" in payload ? payload.Presentation : undefined;
    if (typeof queryId !== "string" || !queryId || !presentation || typeof presentation !== "object" || Array.isArray(presentation)) return undefined;
    return {
      id: `business-query-${queryId}`,
      content: event.payloadJson,
      businessQueryPresentationJson: JSON.stringify(presentation)
    };
  } catch {
    return undefined;
  }
};
