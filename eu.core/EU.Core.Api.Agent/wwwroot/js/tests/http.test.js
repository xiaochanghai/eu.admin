import assert from "node:assert/strict";
import test from "node:test";
import * as http from "../http.js";
import {
  agentApi,
  platformCapabilitiesPresentation,
  chatConversationPresentation,
  chatRunPresentation,
  chatRunDetailsPresentation,
  chatRunEventPresentation,
  agentRunAuditPresentation
} from "../api-client.js";
import { mainAgentPresentation } from "../chat-page.js";
import { approvalPresentation } from "../approval-page.js";
import { mcpApi } from "../mcp-api.js";
import { skillsApi } from "../skills-api.js";

test("exports the strict service response client", () => {
  assert.equal(typeof http.parseServiceResponse, "function");
  assert.equal(typeof http.requestServiceJson, "function");
});

test("returns Data from a successful PascalCase response", () => {
  const data = { ID: "agent-1", Name: "Main Agent" };
  const result = http.parseServiceResponse({
    Status: 200,
    Success: true,
    Message: "查询成功！",
    MessageDev: null,
    Count: 1,
    Data: data
  }, 200, "请求失败");
  assert.strictEqual(result, data);
});

test("accepts HTTP 201 when the business status is 200", () => {
  const data = { ID: "agent-2" };
  const result = http.parseServiceResponse({
    Status: 200,
    Success: true,
    Message: "创建成功",
    Data: data
  }, 201, "请求失败");
  assert.strictEqual(result, data);
});

test("keeps dynamic Data keys unchanged", () => {
  const data = { json_schema_key: { required_field: true } };
  const result = http.parseServiceResponse({
    Status: 200,
    Success: true,
    Message: "查询成功！",
    Data: data
  }, 200, "请求失败");
  assert.strictEqual(result, data);
  assert.equal(result.json_schema_key.required_field, true);
});

test("throws structured error from a failed service response", () => {
  assert.throws(
    () => http.parseServiceResponse({
      Status: 610001,
      Success: false,
      Message: "Agent 不存在。",
      Data: { ErrorCode: "AGENT_NOT_FOUND", TraceId: "trace-123" }
    }, 404, "请求失败"),
    error => {
      assert.equal(error.message, "Agent 不存在。");
      assert.equal(error.status, 404);
      assert.equal(error.businessStatus, 610001);
      assert.equal(error.errorCode, "AGENT_NOT_FOUND");
      assert.equal(error.traceId, "trace-123");
      return true;
    });
});

test("rejects raw and incomplete payloads", () => {
  const invalidPayloads = [
    [],
    { ID: "agent-1" },
    { Status: 200, Success: true, Message: "查询成功！" },
    { Status: 200, Success: "true", Message: "查询成功！", Data: {} }
  ];
  for (const payload of invalidPayloads) {
    assert.throws(
      () => http.parseServiceResponse(payload, 200, "响应格式无效"),
      error => error.code === "SERVICE_RESPONSE_INVALID");
  }
});

test("requestServiceJson sends JSON headers and unwraps Data", async t => {
  const originalFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = originalFetch; });
  globalThis.fetch = async (path, options) => {
    assert.equal(path, "/api/agents");
    assert.equal(options.headers.Accept, "application/json");
    assert.equal(options.headers["Content-Type"], "application/json");
    return {
      ok: true,
      status: 200,
      async json() {
        return {
          Status: 200,
          Success: true,
          Message: "查询成功！",
          Data: [{ ID: "agent-1" }]
        };
      }
    };
  };
  const result = await http.requestServiceJson("/api/agents", {
    method: "POST",
    body: "{}"
  });
  assert.deepEqual(result, [{ ID: "agent-1" }]);
});

test("agent API unwraps migrated list responses without renaming DTO fields", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const agents = [{ Id: "agent-1", Code: "main", RuntimeStatus: "Enabled" }];
  globalThis.fetch = async () => ({
    ok: true,
    status: 200,
    json: async () => ({
      Status: 200,
      Success: true,
      Message: "查询成功！",
      Count: 0,
      Data: agents
    })
  });

  const result = await agentApi.list({ status: "Enabled" });

  assert.strictEqual(result, agents);
  assert.equal(result[0].RuntimeStatus, "Enabled");
  assert.equal(result[0].runtimeStatus, undefined);
});

test("skill file content keeps its text protocol", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  globalThis.fetch = async () => ({
    ok: true,
    status: 200,
    text: async () => "# Skill"
  });

  const result = await skillsApi.readFile("skill-1", "SKILL.md");

  assert.equal(result, "# Skill");
});

test("Agent export parses migrated service errors before returning a file", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  globalThis.fetch = async () => ({
    ok: false,
    status: 404,
    json: async () => ({
      Status: 610001,
      Success: false,
      Message: "The Agent was not found.",
      Data: { ErrorCode: "AGENT_NOT_FOUND", TraceId: "trace-export" }
    })
  });

  await assert.rejects(
    () => agentApi.exportPackage("missing"),
    error => error.errorCode === "AGENT_NOT_FOUND" && error.traceId === "trace-export");
});

test("main Agent presentation consumes PascalCase assignment and Agent DTOs", () => {
  const versionId = "version-1";
  const result = mainAgentPresentation(
    { AgentVersionId: versionId, LogicalRevision: 3 },
    {
      Code: "main",
      Name: "Main Agent",
      RuntimeStatus: "Enabled",
      PublishedVersions: [{ Id: versionId, Label: "1.0.0" }]
    });

  assert.equal(result.state, "ready");
  assert.equal(result.name, "Main Agent · Main Agent");
  assert.match(result.detail, /v1\.0\.0/);
});

test("MCP API unwraps PascalCase service responses", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const servers = [{ Id: "server-1", Code: "business-query", Status: "Healthy" }];
  globalThis.fetch = async () => ({
    status: 200,
    json: async () => ({
      Status: 200,
      Success: true,
      Message: "查询成功！",
      Data: servers
    })
  });

  const result = await mcpApi.list();

  assert.strictEqual(result, servers);
  assert.equal(result[0].Code, "business-query");
});

test("tool approval API and presentation consume PascalCase DTOs", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const approvals = [{
    Id: "approval-1",
    Status: "Pending",
    Risk: "HighRisk",
    ExpiresAtUtc: "2099-01-01T00:00:00Z"
  }];
  globalThis.fetch = async () => ({
    status: 200,
    json: async () => ({
      Status: 200,
      Success: true,
      Message: "查询成功！",
      Data: approvals
    })
  });

  const result = await agentApi.toolApprovals();
  const view = approvalPresentation(result[0], Date.parse("2098-12-31T23:59:00Z"));

  assert.strictEqual(result, approvals);
  assert.equal(view.status, "待审批");
  assert.equal(view.risk, "高风险");
});

test("Agent editor formats migrated PascalCase MCP tool references", async () => {
  const module = await import("../agent-editor.js");
  assert.equal(typeof module.mcpToolReferencePresentation, "function");

  const value = module.mcpToolReferencePresentation({
    ServerCode: "business-query",
    ToolVersionId: "tool-version-1",
    ToolName: "query_business_data",
    Risk: "ReadOnly",
    Sha256: "0123456789abcdef"
  });

  assert.deepEqual(value, {
    id: "tool-version-1",
    title: "query_business_data",
    detail: "business-query · ReadOnly · 0123456789"
  });
});

test("knowledge API unwraps PascalCase service responses", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const values = [{
    Id: "knowledge-1",
    Code: "atlas",
    Name: "Atlas",
    Status: "Enabled",
    LogicalRevision: 3,
    DocumentCount: 1,
    ChunkCount: 2
  }];
  globalThis.fetch = async () => ({
    ok: true,
    status: 200,
    json: async () => ({
      Status: 200,
      Success: true,
      Message: "查询成功！",
      Data: values
    })
  });

  const result = await agentApi.knowledgeBases("Enabled");

  assert.strictEqual(result, values);
  assert.equal(result[0].DocumentCount, 1);
  assert.equal(result[0].documentCount, undefined);
});

test("knowledge PDF upload preserves FormData and unwraps its JSON result", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const file = new Blob(["%PDF-1.7"], { type: "application/pdf" });
  Object.defineProperty(file, "name", { value: "atlas.pdf" });
  const detail = { Id: "knowledge-1", LogicalRevision: 4 };
  globalThis.fetch = async (path, options) => {
    assert.equal(path, "/api/knowledge-bases/knowledge-1/documents/pdf");
    assert.equal(options.method, "POST");
    assert.ok(options.body instanceof FormData);
    assert.equal(options.body.get("expectedLogicalRevision"), "3");
    assert.equal(options.body.get("file").name, "atlas.pdf");
    assert.equal(options.headers.Accept, "application/json");
    assert.equal(options.headers["Content-Type"], undefined);
    return {
      ok: true,
      status: 200,
      json: async () => ({
        Status: 200,
        Success: true,
        Message: "操作成功！",
        Data: detail
      })
    };
  };

  const result = await agentApi.importKnowledgePdf("knowledge-1", 3, file);

  assert.strictEqual(result, detail);
});

test("Agent editor formats migrated PascalCase knowledge references", async () => {
  const module = await import("../agent-editor.js");
  assert.equal(typeof module.knowledgeReferencePresentation, "function");

  const value = module.knowledgeReferencePresentation({
    KnowledgeBaseId: "knowledge-1",
    Code: "atlas",
    Name: "Atlas",
    LogicalRevision: 3
  });

  assert.deepEqual(value, {
    id: "knowledge-1",
    title: "Atlas",
    detail: "atlas · REV 3"
  });
});

test("orchestration API unwraps PascalCase service responses", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const responses = new Map([
    ["/api/orchestrations", [{ Id: "flow-1", Code: "flow", Status: "Enabled" }]],
    ["/api/orchestrations/flow-1/runs/run-1", {
      Id: "run-1",
      Status: "Failed",
      ErrorCode: "ORCHESTRATION_RUN_FAILED",
      Nodes: []
    }],
    ["/api/orchestrations/flow-1/runs/run-1/output", {
      Output: "{\"snake_key\":true}",
      Ephemeral: false
    }]
  ]);
  globalThis.fetch = async path => ({
    ok: true,
    status: 200,
    json: async () => ({
      Status: 200,
      Success: true,
      Message: "查询成功！",
      Data: responses.get(path)
    })
  });

  const values = await agentApi.orchestrations();
  const run = await agentApi.orchestrationRun("flow-1", "run-1");
  const output = await agentApi.orchestrationOutput("flow-1", "run-1");

  assert.strictEqual(values, responses.get("/api/orchestrations"));
  assert.equal(run.ErrorCode, "ORCHESTRATION_RUN_FAILED");
  assert.equal(output.Output, "{\"snake_key\":true}");
});

test("orchestration presentation consumes PascalCase list and run DTOs", async () => {
  const module = await import("../orchestration-page.js");
  assert.equal(typeof module.orchestrationListItemPresentation, "function");
  assert.equal(typeof module.orchestrationRunPresentation, "function");
  assert.equal(typeof module.orchestrationAgentOptionPresentation, "function");

  assert.deepEqual(module.orchestrationListItemPresentation({
    Id: "flow-1",
    Code: "flow",
    Name: "Flow",
    Description: "Description",
    DraftNodeCount: 2,
    CurrentPublishedLabel: "1.0.0",
    Status: "Enabled"
  }), {
    id: "flow-1",
    code: "flow",
    name: "Flow",
    description: "Description",
    draftNodeCount: 2,
    currentPublishedLabel: "1.0.0",
    status: "Enabled"
  });

  assert.deepEqual(module.orchestrationRunPresentation({
    Id: "run-1",
    Status: "Failed",
    ErrorCode: "ORCHESTRATION_RUN_FAILED",
    Nodes: [{
      NodeId: "node-1",
      NodeName: "Node 1",
      Status: "Failed",
      Attempts: 1,
      OutputCharacters: 0,
      ErrorCode: "ORCHESTRATION_NODE_FAILED"
    }]
  }), {
    id: "run-1",
    status: "Failed",
    errorCode: "ORCHESTRATION_RUN_FAILED",
    nodes: [{
      nodeId: "node-1",
      nodeName: "Node 1",
      status: "Failed",
      attempts: 1,
      outputCharacters: 0,
      errorCode: "ORCHESTRATION_NODE_FAILED"
    }]
  });

  assert.deepEqual(module.orchestrationAgentOptionPresentation({
    Id: "agent-1",
    Code: "flow-agent",
    Name: "Flow Agent",
    RuntimeStatus: "Enabled",
    CurrentPublishedLabel: "2.0.0"
  }), {
    id: "agent-1",
    code: "flow-agent",
    name: "Flow Agent",
    runtimeStatus: "Enabled",
    currentPublishedLabel: "2.0.0"
  });
});

test("evaluation API unwraps PascalCase service responses", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const responses = new Map([
    ["/api/evaluation-suites", [{ Id: "suite-1", Status: "Active" }]],
    ["/api/evaluation-batches?suiteId=suite-1&take=50", [{ Id: "batch-1", Status: "Completed" }]],
    ["/api/evaluation-batches/batch-1/model-judge-reports?take=20", [{
      Id: "report-1",
      ConfigurationSha256: "configuration-sha256"
    }]]
  ]);
  globalThis.fetch = async path => ({
    ok: true,
    status: 200,
    json: async () => ({
      Status: 200,
      Success: true,
      Message: "查询成功！",
      Data: responses.get(path)
    })
  });

  const suites = await agentApi.evaluationSuites();
  const batches = await agentApi.evaluationBatches("suite-1", 50);
  const reports = await agentApi.modelJudgeReports("batch-1", 20);

  assert.strictEqual(suites, responses.get("/api/evaluation-suites"));
  assert.strictEqual(batches, responses.get("/api/evaluation-batches?suiteId=suite-1&take=50"));
  assert.equal(reports[0].ConfigurationSha256, "configuration-sha256");
});

test("evaluation presentation consumes PascalCase suites batches comparisons and reports", async () => {
  const module = await import("../evaluation-page.js");
  assert.equal(typeof module.evaluationSuitePresentation, "function");
  assert.equal(typeof module.evaluationBatchPresentation, "function");
  assert.equal(typeof module.evaluationComparisonContractPresentation, "function");
  assert.equal(typeof module.modelJudgeContractPresentation, "function");

  const suite = module.evaluationSuitePresentation({
    Id: "suite-1",
    Code: "quality",
    Name: "Quality",
    Description: "Description",
    LogicalRevision: 2,
    Status: "Active",
    Draft: { Cases: [] },
    PublishedVersions: [{
      Id: "version-1",
      Label: "1.0.0",
      ContentSha256: "abcdef",
      Cases: []
    }]
  });
  assert.equal(suite.id, "suite-1");
  assert.equal(suite.publishedVersions[0].contentSha256, "abcdef");

  const batch = module.evaluationBatchPresentation({
    Id: "batch-1",
    SuiteVersionId: "version-1",
    Status: "Completed",
    StartedAtUtc: "2026-08-17T00:00:00Z",
    Cases: [{
      CaseId: "case-1",
      CaseName: "Case",
      Status: "Failed",
      UnifiedRunId: "run-1",
      Report: { Passed: false, Checks: [{ Code: "status", Passed: false, Expected: "Completed", Actual: "Failed" }] }
    }]
  });
  assert.equal(batch.cases[0].report.checks[0].passed, false);

  const comparison = module.evaluationComparisonContractPresentation({
    GatePassed: false,
    Baseline: { PassRate: 1 },
    Candidate: { PassRate: 0.5 },
    GateChecks: [{ Code: "pass-rate", Passed: false, Expected: "1", Actual: "0.5" }],
    Cases: []
  });
  assert.equal(comparison.gateChecks[0].code, "pass-rate");

  const report = module.modelJudgeContractPresentation({
    Id: "report-1",
    AdvisoryPassed: false,
    ModelProfileId: "model",
    ConfigurationSha256: "configuration-sha256",
    Cases: [{ CaseName: "Case", Metrics: [{ Name: "Relevance", Passed: false }] }]
  });
  assert.equal(report.configurationSha256, "configuration-sha256");
  assert.equal(report.cases[0].metrics[0].passed, false);
});

test("runtime API unwraps ServiceResult and maps only ordinary JSON DTO fields", async t => {
  const previousFetch = globalThis.fetch;
  t.after(() => { globalThis.fetch = previousFetch; });
  const payloads = new Map([
    ["/api/platform/capabilities", { StorageMode: "sqlsugar", Volatile: false, ModelProfileIds: ["model-1"], Deployment: { Target: "Server", Host: "EU.Core.Api.Agent" }, Features: { Runtime: true } }],
    ["/api/chat/conversations?take=40", [{ Id: "conversation-1", Title: "Chat", UpdatedAtUtc: "2026-08-17T00:00:00Z" }]],
    ["/api/chat/conversations/conversation-1?take=160", { Conversation: { Id: "conversation-1", Title: "Chat" }, Messages: [{ Id: "message-1", Role: "User", Content: "hello", BusinessQueryPresentationJson: "{\"snake_key\":true}" }] }],
    ["/api/chat/conversations/conversation-1/runs?take=20", [{ Id: "run-1", Status: "Completed", Output: "done" }]],
    ["/api/chat/runs/run-1/details", { EntryRun: { Id: "run-1", Status: "Completed" }, AgentRuns: [], Orchestrations: [], ToolCalls: [{ ToolVersionId: "tool-1", ArgumentsJson: "{\"snake_key\":true}" }] }],
    ["/api/chat/runs/run-1/events?take=160", [{ Id: "event-1", Kind: "message", PayloadJson: "{\"snake_key\":true}" }]],
    ["/api/agents/agent-1/runs?take=10", [{ RunId: "agent-run-1", Status: "Completed", ToolCallCount: 0 }]]
  ]);
  globalThis.fetch = async path => ({
    ok: true,
    status: 200,
    json: async () => ({ Status: 200, Success: true, Message: "OK", Data: payloads.get(path) })
  });

  const capabilities = await agentApi.capabilities();
  const conversations = await agentApi.listChatConversations();
  const conversation = await agentApi.chatConversation("conversation-1");
  const runs = await agentApi.chatConversationRuns("conversation-1");
  const details = await agentApi.chatRunDetails("run-1");
  const events = await agentApi.chatRunEvents("run-1");
  const agentRuns = await agentApi.runHistory("agent-1", 10);

  assert.equal(capabilities.storageMode, "sqlsugar");
  assert.equal(conversations[0].id, "conversation-1");
  assert.equal(conversation.messages[0].businessQueryPresentationJson, "{\"snake_key\":true}");
  assert.equal(runs[0].status, "Completed");
  assert.equal(details.toolCalls[0].argumentsJson, "{\"snake_key\":true}");
  assert.equal(events[0].payloadJson, "{\"snake_key\":true}");
  assert.equal(agentRuns[0].runId, "agent-run-1");
});

test("runtime JSON presentation functions are explicit and preserve embedded payload keys", () => {
  assert.equal(platformCapabilitiesPresentation({ StorageMode: "sqlsugar" }).storageMode, "sqlsugar");
  assert.equal(chatConversationPresentation({ Id: "conversation-1" }).id, "conversation-1");
  assert.equal(chatRunPresentation({ Id: "run-1" }).id, "run-1");
  assert.equal(chatRunDetailsPresentation({
    EntryRun: { Id: "run-1" }, AgentRuns: [], Orchestrations: [],
    ToolCalls: [{ ArgumentsJson: "{\"snake_key\":true}" }]
  }).toolCalls[0].argumentsJson, "{\"snake_key\":true}");
  assert.equal(chatRunEventPresentation({ PayloadJson: "{\"snake_key\":true}" }).payloadJson, "{\"snake_key\":true}");
  assert.equal(agentRunAuditPresentation({ RunId: "run-1" }).runId, "run-1");
});
