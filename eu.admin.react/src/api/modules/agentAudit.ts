import http from "@/api";

export interface AgentOperationAudit {
  Id: string;
  OccurredAtUtc: string;
  TenantId: string;
  UserId: string;
  CorrelationId: string;
  Policy: string;
  Method: string;
  Path: string;
  StatusCode: number;
  Outcome: string;
  ErrorCode?: string | null;
  DurationMilliseconds: number;
}

const auditUrl = "/Agent/api/audit/operations";

export const getAgentAuditErrorMessage = (error: unknown, fallback: string) => {
  if (typeof error === "object" && error !== null) {
    const value = error as { Message?: string; message?: string; response?: { data?: { Message?: string; message?: string } } };
    return value.response?.data?.Message || value.response?.data?.message || value.Message || value.message || fallback;
  }
  return fallback;
};

export const listAgentOperationAudits = (take = 100) =>
  http.get<AgentOperationAudit[]>(auditUrl, { take }).then(response => response.Data);
