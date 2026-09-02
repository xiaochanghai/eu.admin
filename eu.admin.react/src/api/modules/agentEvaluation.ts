import http from "@/api";

export type EvaluationSuiteStatus = "Active" | "Archived";
export type EvaluationBatchStatus = "Running" | "Completed" | "Cancelled" | "Failed";
export type EvaluationCaseStatus = "Pending" | "Running" | "Passed" | "Failed" | "Cancelled";

export interface EvaluationSpecification {
  ExpectedStatus?: string | null;
  OutputContains: string[];
  OutputExcludes: string[];
  RequiredEventKinds: string[];
  MaximumToolCalls?: number | null;
  MaximumDurationMilliseconds?: number | null;
}

export interface EvaluationCase {
  Id: string;
  Name: string;
  Input: string;
  TargetAgentId: string;
  TargetAgentVersionId: string;
  Specification: EvaluationSpecification;
}

export interface EvaluationSuiteVersion {
  Id: string;
  Label: string;
  ContentSha256: string;
  PublishedAtUtc: string;
  PublishedBy: string;
  Cases: EvaluationCase[];
}

export interface EvaluationSuite {
  Id: string;
  Code: string;
  Name: string;
  Description: string;
  Status: EvaluationSuiteStatus;
  LogicalRevision: number;
  Draft: { Cases: EvaluationCase[] };
  PublishedVersions: EvaluationSuiteVersion[];
}

export interface EvaluationCheck {
  Code: string;
  Passed: boolean;
  Expected: string;
  Actual: string;
}

export interface EvaluationBatchCase {
  CaseId: string;
  CaseName: string;
  TargetAgentId: string;
  TargetAgentVersionId: string;
  Status: EvaluationCaseStatus;
  UnifiedRunId?: string | null;
  UnifiedRunStatus?: string | null;
  DurationMilliseconds?: number | null;
  ToolCallCount: number;
  ObservedEventKinds: string[];
  ObservedRoutes: string[];
  ErrorCode: string;
  Report?: { Passed: boolean; Score: number; Checks: EvaluationCheck[] } | null;
}

export interface EvaluationBatch {
  Id: string;
  SuiteId: string;
  SuiteVersionId: string;
  Status: EvaluationBatchStatus;
  StartedAtUtc: string;
  FinishedAtUtc?: string | null;
  Cases: EvaluationBatchCase[];
  ErrorCode: string;
}

export interface EvaluationQualityGate {
  minimumCandidatePassRate: number;
  maximumPassRateRegression: number;
  maximumAverageDurationRegressionPercent?: number | null;
  maximumToolCallIncreasePerCase?: number | null;
  requireNoNewFailures: boolean;
  requireSameCaseSet: boolean;
  requireStableRoutes: boolean;
}

export interface EvaluationComparison {
  GatePassed: boolean;
  GateChecks: EvaluationCheck[];
  Baseline: { TotalCases: number; PassedCases: number; FailedCases: number; PassRate: number; AverageDurationMilliseconds?: number | null; TotalToolCalls: number };
  Candidate: { TotalCases: number; PassedCases: number; FailedCases: number; PassRate: number; AverageDurationMilliseconds?: number | null; TotalToolCalls: number };
}

export interface SaveEvaluationDraftInput {
  expectedLogicalRevision: number;
  name: string;
  description: string;
  cases: EvaluationCase[];
}

const url = (path = "") => `/Agent/api/evaluation-suites${path}`;
const batchUrl = (path = "") => `/Agent/api/evaluation-batches${path}`;

export const listEvaluationSuites = async (status?: EvaluationSuiteStatus) =>
  (await http.get<EvaluationSuite[]>(url(), status ? { status } : undefined)).Data;
export const getEvaluationSuite = async (id: string) =>
  (await http.get<EvaluationSuite>(url(`/${encodeURIComponent(id)}`))).Data;
export const createEvaluationSuite = async (input: { code: string; name: string; description: string }) =>
  (await http.post<EvaluationSuite>(url(), input)).Data;
export const saveEvaluationDraft = async (id: string, input: SaveEvaluationDraftInput) =>
  (await http.put<EvaluationSuite>(url(`/${encodeURIComponent(id)}/draft`), input)).Data;
export const publishEvaluationSuite = async (id: string, expectedLogicalRevision: number) =>
  (await http.post<EvaluationSuite>(url(`/${encodeURIComponent(id)}/publish`), { expectedLogicalRevision })).Data;
export const setEvaluationArchived = async (id: string, expectedLogicalRevision: number, archived: boolean) =>
  (await http.put<EvaluationSuite>(url(`/${encodeURIComponent(id)}/archive`), { expectedLogicalRevision, archived })).Data;
export const runEvaluationBatch = async (suiteId: string, suiteVersionId: string) =>
  (await http.post<EvaluationBatch>(batchUrl(), { suiteId, suiteVersionId })).Data;
export const listEvaluationBatches = async (suiteId: string) =>
  (await http.get<EvaluationBatch[]>(batchUrl(), { suiteId, take: 50 })).Data;
export const compareEvaluationBatches = async (baselineBatchId: string, candidateBatchId: string, gate: EvaluationQualityGate) =>
  (await http.post<EvaluationComparison>(batchUrl("/compare"), { baselineBatchId, candidateBatchId, gate })).Data;
