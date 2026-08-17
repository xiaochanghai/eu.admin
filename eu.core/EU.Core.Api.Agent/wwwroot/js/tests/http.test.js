import assert from "node:assert/strict";
import test from "node:test";
import * as http from "../http.js";
import { agentApi } from "../api-client.js";
import { mainAgentPresentation } from "../chat-page.js";
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
