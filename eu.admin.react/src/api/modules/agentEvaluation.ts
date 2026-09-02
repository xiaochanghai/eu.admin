import http from "@/api";

export type EvaluationSuiteStatus = "Active" | "Archived";
export interface EvaluationSuite { Id: string; Code: string; Name: string; Description: string; Status: EvaluationSuiteStatus; LogicalRevision: number; Draft: { Cases: unknown[] }; PublishedVersions: Array<{ Id: string; Label: string }> }
const url = (path = "") => `/Agent/api/evaluation-suites${path}`;
export const listEvaluationSuites = async (status?: EvaluationSuiteStatus) => (await http.get<EvaluationSuite[]>(url(), status ? { status } : undefined)).Data;
export const getEvaluationSuite = async (id: string) => (await http.get<EvaluationSuite>(url(`/${encodeURIComponent(id)}`))).Data;
export const createEvaluationSuite = async (input: { code: string; name: string; description: string }) => (await http.post<EvaluationSuite>(url(), input)).Data;
export const saveEvaluationDraft = async (id: string, input: { expectedLogicalRevision: number; name: string; description: string; cases: unknown[] }) => (await http.put<EvaluationSuite>(url(`/${encodeURIComponent(id)}/draft`), input)).Data;
export const publishEvaluationSuite = async (id: string, expectedLogicalRevision: number) => (await http.post<EvaluationSuite>(url(`/${encodeURIComponent(id)}/publish`), { expectedLogicalRevision })).Data;
export const setEvaluationArchived = async (id: string, expectedLogicalRevision: number, archived: boolean) => (await http.put<EvaluationSuite>(url(`/${encodeURIComponent(id)}/archive`), { expectedLogicalRevision, archived })).Data;
const batchUrl = (path = "") => `/Agent/api/evaluation-batches${path}`;
export const runEvaluationBatch = async (suiteId: string, suiteVersionId: string) => (await http.post<{ Id: string; Status: string }>(batchUrl(), { suiteId, suiteVersionId })).Data;
export const listEvaluationBatches = async (suiteId: string) => (await http.get<Array<{ Id: string; Status: string; StartedAtUtc: string; Cases: unknown[] }>>(batchUrl(), { suiteId, take: 50 })).Data;
export const compareEvaluationBatches = async (baselineBatchId: string, candidateBatchId: string) => (await http.post<{ GatePassed: boolean; GateChecks: Array<{ Code: string; Passed: boolean; Expected: string; Actual: string }> }>(batchUrl("/compare"), { baselineBatchId, candidateBatchId, gate: { minimumCandidatePassRate: 1, maximumPassRateRegression: 0, requireNoNewFailures: true, requireSameCaseSet: true, requireStableRoutes: false } })).Data;
