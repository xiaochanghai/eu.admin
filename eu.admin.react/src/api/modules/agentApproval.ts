import http from "@/api";

export type ToolApprovalStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "Cancelled"
  | "Expired"
  | "Consuming"
  | "Consumed"
  | "Failed"
  | "Invalidated";

export type ToolApprovalRisk = "Mutating" | "HighRisk";

export interface ToolApproval {
  Id: string;
  RequesterUserId: string;
  ConversationId: string;
  EntryRunId: string;
  AgentVersionId: string;
  ToolVersionId: string;
  ToolName: string;
  Risk: ToolApprovalRisk;
  ToolSchemaSha256: string;
  ArgumentsSha256: string;
  SafeArgumentsSummaryJson: string;
  Status: ToolApprovalStatus;
  RequestedAtUtc: string;
  ExpiresAtUtc: string;
  DecisionUserId: string;
  DecisionReason: string;
  DecidedAtUtc?: string | null;
  ErrorCode: string;
}

export interface ToolApprovalDecision {
  Id: string;
  ToStatus: ToolApprovalStatus;
  DecisionUserId: string;
  DecisionReason: string;
  DecidedAtUtc: string;
}

export interface ToolApprovalDetail {
  Approval: ToolApproval;
  Decisions: ToolApprovalDecision[];
}

export interface ToolApprovalResumeResult {
  EntryRunId: string;
  Status: string;
  ErrorCode: string;
}

const approvalUrl = (path = "") => `/Agent/api/tool-approvals${path}`;
const encoded = (value: string) => encodeURIComponent(value);

export const getApprovalErrorMessage = (error: unknown, fallback: string) => {
  if (typeof error === "object" && error !== null) {
    const value = error as {
      Message?: string;
      message?: string;
      response?: { data?: { Message?: string; message?: string } };
    };
    return value.response?.data?.Message || value.response?.data?.message || value.Message || value.message || fallback;
  }
  return fallback;
};

export const listToolApprovals = (status?: ToolApprovalStatus, take = 200) =>
  http.get<ToolApproval[]>(approvalUrl(), {
    ...(status ? { status } : {}),
    take
  }).then(response => response.Data);

export const getToolApproval = (id: string) =>
  http.get<ToolApprovalDetail>(approvalUrl(`/${encoded(id)}`)).then(response => response.Data);

const decideToolApproval = (id: string, action: "approve" | "reject" | "cancel", reason: string) =>
  http.post<ToolApproval>(approvalUrl(`/${encoded(id)}/${action}`), { reason }).then(response => response.Data);

export const approveToolApproval = (id: string, reason: string) => decideToolApproval(id, "approve", reason);
export const rejectToolApproval = (id: string, reason: string) => decideToolApproval(id, "reject", reason);
export const cancelToolApproval = (id: string, reason: string) => decideToolApproval(id, "cancel", reason);

export const resumeToolApproval = (id: string) =>
  http.post<ToolApprovalResumeResult>(approvalUrl(`/${encoded(id)}/resume`)).then(response => response.Data);
