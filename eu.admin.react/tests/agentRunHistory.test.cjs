// Lightweight offline checks: Node's built-in runner + the project's TypeScript compiler.
// No DOM, real React renderer, database or network. An optional argument selects an already installed compiler.
const assert = require("node:assert/strict");
const { test } = require("node:test");
const { readFileSync } = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const ts = require(process.argv[2] || "typescript");
const root = path.resolve(__dirname, "..");
const presentationFile = "src/views/agent/agentDefinition/runHistoryPresentation.ts";

function load(file, mocks = {}, globals = {}) {
  const source = readFileSync(path.join(root, file), "utf8").replaceAll("import.meta.env.VITE_API_URL", '""');
  const result = ts.transpileModule(source, {
    fileName: file,
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX },
    reportDiagnostics: true
  });
  assert.equal((result.diagnostics || []).filter(item => item.category === ts.DiagnosticCategory.Error).length, 0);
  const module = { exports: {} };
  vm.runInNewContext(result.outputText, {
    module,
    exports: module.exports,
    require(name) {
      assert.ok(Object.hasOwn(mocks, name), `Unexpected import ${name}`);
      return mocks[name];
    },
    TextDecoder,
    Uint8Array,
    AbortController,
    Error,
    ...globals
  }, { filename: file });
  return module.exports;
}

const presentation = load(presentationFile);
for (const value of [undefined, null, -1, 0.5, NaN, Infinity, Number.MAX_SAFE_INTEGER + 1, "0"]) {
  test(`unknown/invalid count is not zero: ${String(value)}`, () => assert.equal(presentation.formatRunCount(value), "未知"));
}
test("measured zero and normal counts are displayed", () => {
  assert.equal(presentation.formatRunCount(0), "0");
  assert.equal(presentation.formatRunCount(1234), "1,234");
  assert.equal(presentation.formatRunMilliseconds(0), "0 ms");
  assert.equal(presentation.formatRunMilliseconds(null), "未知");
});
test("invalid, reversed or incomplete dates do not create fake durations", () => {
  assert.equal(presentation.formatRunDate(null), "未知");
  assert.equal(presentation.formatRunDate("not-a-date"), "未知");
  const start = "2026-10-02T01:00:00Z";
  assert.equal(presentation.formatRunDuration(start, null), "未知");
  assert.equal(presentation.formatRunDuration(start, "2026-10-02T00:59:59Z"), "未知");
  assert.equal(presentation.formatRunDuration(start, "2026-10-02T01:00:01Z"), "1,000 ms");
  assert.notEqual(presentation.formatRunDate(start), "未知");
});
test("unknown, partial and reported usage remain distinct", () => {
  assert.equal(presentation.describeRunUsage().label, "用量未知");
  assert.equal(presentation.describeRunUsage("future-status").label, "用量未知");
  assert.equal(presentation.describeRunUsage("Partial").label, "部分统计");
  assert.equal(presentation.describeRunUsage("Reported").label, "完整统计");
});
test("approval, cancellation and failure are not success", () => {
  assert.equal(presentation.describeRunStatus("WaitingForApproval").label, "等待审批");
  assert.equal(presentation.describeRunStatus("Cancelled").label, "已取消");
  assert.equal(presentation.describeRunTerminalEvent("approvalrequired"), "等待审批（运行暂停）");
  assert.equal(presentation.describeRunTerminalEvent("failed", "TEST_FAILURE"), "运行失败 · TEST_FAILURE");
  assert.equal(presentation.describeRunTerminalEvent("cancelled"), "运行已取消");
  assert.equal(presentation.describeRunTerminalEvent("completed"), "运行完成");
  assert.equal(presentation.describeRunTerminalEvent("delta"), undefined);
});

function loadApi(fetch, get = async () => ({ Data: [] }), writes = {}) {
  return load("src/api/modules/agent.ts", {
    "@/api": { default: { get, ...writes } },
    "@/redux": { store: { getState: () => ({ user: { token: "test-token" } }) } }
  }, { fetch });
}
function streamResponse(chunks) {
  return new Response(new ReadableStream({
    start(controller) {
      for (const chunk of chunks) controller.enqueue(chunk);
      controller.close();
    }
  }), { headers: { "Content-Type": "text/event-stream; charset=utf-8" } });
}
test("history reuses the existing URL, wrapper and cancellation signal", async () => {
  const controller = new AbortController();
  const rows = [{ RunId: "test-run" }];
  const api = loadApi(undefined, async (url, params, config) => {
    assert.equal(url, "/Agent/api/agents/a%2Fb/runs");
    assert.equal(params.take, 20);
    assert.equal(config.signal, controller.signal);
    return { Data: rows };
  });
  assert.equal(await api.listAgentRuns("a/b", 20, controller.signal), rows);
});
test("stream delivers split UTF-8 text and terminal events and releases the reader", async () => {
  const bytes = new TextEncoder().encode('event: delta\ndata: {"text":"测试"}\n\nevent: completed\ndata: {}\n\n');
  const response = streamResponse(Array.from(bytes, value => new Uint8Array([value])));
  const signal = new AbortController().signal;
  const api = loadApi(async (url, options) => {
    assert.equal(url, "/Agent/api/agents/test-agent/runs");
    assert.equal(options.headers.Authorization, "Bearer test-token");
    assert.equal(options.signal, signal);
    assert.equal(JSON.parse(options.body).input, "hello");
    return response;
  });
  const events = [];
  await api.runAgent("test-agent", "hello", (name, event) => events.push([name, event.text]), signal);
  assert.deepEqual(events, [["delta", "测试"], ["completed", undefined]]);
  assert.equal(response.body.locked, false);
});
test("nullable tool identifiers in actual server events are accepted", async () => {
  const response = streamResponse([new TextEncoder().encode('event: delta\ndata: {"text":"ok","toolVersionId":null,"toolCallId":null}\n\n')]);
  let text;
  await loadApi(async () => response).runAgent("test", "", (_, event) => { text = event.text; }, new AbortController().signal);
  assert.equal(text, "ok");
});
test("HTTP failures and JSON error responses are not accepted as streams", async () => {
  await assert.rejects(loadApi(async () => new Response(null, { status: 403 })).runAgent(
    "test", "", () => assert.fail(), new AbortController().signal
  ), /403/);
  await assert.rejects(loadApi(async () => new Response('{"Success":false}', {
    headers: { "Content-Type": "application/json" }
  })).runAgent("test", "", () => assert.fail(), new AbortController().signal), /未返回运行事件流/);
});
test("malformed JSON and invalid event fields fail explicitly and unlock the stream", async () => {
  for (const data of ["{bad-json}", "null", "[]", '{"text":{}}', '{"knowledgeHitCount":-1}']) {
    const response = streamResponse([new TextEncoder().encode(`event: delta\ndata: ${data}\n\n`)]);
    await assert.rejects(loadApi(async () => response).runAgent("test", "", () => assert.fail(), new AbortController().signal));
    assert.equal(response.body.locked, false);
  }
});
test("callback errors are not silently swallowed", async () => {
  const response = streamResponse([new TextEncoder().encode("event: completed\ndata: {}\n\n")]);
  await assert.rejects(loadApi(async () => response).runAgent("test", "", () => {
    throw new Error("test-handler-error");
  }, new AbortController().signal), /test-handler-error/);
  assert.equal(response.body.locked, false);
});
test("abort rejects the stream and releases the reader", async () => {
  const controller = new AbortController();
  const response = new Response(new ReadableStream({
    start(stream) {
      controller.signal.addEventListener("abort", () => stream.error(new Error("test-aborted")), { once: true });
    }
  }), { headers: { "Content-Type": "text/event-stream" } });
  const pending = loadApi(async () => response).runAgent("test", "", () => assert.fail(), controller.signal);
  controller.abort();
  await assert.rejects(pending, /test-aborted/);
  assert.equal(response.body.locked, false);
});

// A small hook boundary simulator verifies request ownership. It is not a React/browser integration test.
function historyHarness(file = "RunHistory") {
  const hooks = [];
  const calls = [];
  let cursor = 0;
  let mounted = true;
  let token = "test-session-a";
  let pendingEffects = [];
  const sameDeps = (a, b) => a && b && a.length === b.length && a.every((value, index) => Object.is(value, b[index]));
  const react = {
    useState(initial) {
      const index = cursor++;
      if (!hooks[index]) hooks[index] = { value: initial };
      return [hooks[index].value, value => {
        assert.ok(mounted, "No state updates after unmount");
        hooks[index].value = typeof value === "function" ? value(hooks[index].value) : value;
      }];
    },
    useRef(initial) {
      const index = cursor++;
      if (!hooks[index]) hooks[index] = { current: initial };
      return hooks[index];
    },
    useCallback(callback, deps) {
      const index = cursor++;
      if (!sameDeps(hooks[index]?.deps, deps)) hooks[index] = { value: callback, deps };
      return hooks[index].value;
    },
    useEffect(setup, deps) {
      const index = cursor++;
      if (!sameDeps(hooks[index]?.deps, deps)) pendingEffects.push(() => {
        hooks[index]?.cleanup?.();
        hooks[index] = { deps, cleanup: setup() };
      });
    }
  };
  const jsx = (type, props) => ({ type, props });
  const antd = Object.fromEntries(["Alert", "Button", "Checkbox", "Collapse", "Descriptions", "Flex", "List", "Popconfirm", "Select", "Space", "Spin", "Tag"].map(name => [name, name]));
  antd.Input = { TextArea: "TextArea" };
  antd.Typography = { Title: "Title", Paragraph: "Paragraph", Text: "Text" };
  const loaded = load(`src/views/agent/agentDefinition/${file}.tsx`, {
    react,
    "react/jsx-runtime": { jsx, jsxs: jsx },
    antd,
    "@ant-design/icons": { SyncOutlined: "SyncOutlined" },
    "@/redux": {
      useSelector: select => select({ user: { token } }),
      store: { getState: () => ({ user: { token } }) }
    },
    "@/api/modules/agent": {
      ...loadApi(),
      listAgentRuns: (id, take, signal) => new Promise((resolve, reject) => calls.push({ id, take, signal, resolve, reject })),
      getCurrentUserTokenQuota: signal => new Promise((resolve, reject) => calls.push({ signal, resolve, reject })),
      getAgentQuotaManagementAccess: signal => new Promise((resolve, reject) => calls.push({ method: "access", signal, resolve, reject })),
      getAgentQuotaPolicy: signal => new Promise((resolve, reject) => calls.push({ method: "policy", signal, resolve, reject })),
      getAgentQuotaPending: signal => new Promise((resolve, reject) => calls.push({ method: "pending", signal, resolve, reject })),
      saveAgentQuotaPolicy: (input, signal) => new Promise((resolve, reject) => calls.push({ method: "save", input, signal, resolve, reject })),
      reconcileAgentQuota: (reservationId, input, signal) => new Promise((resolve, reject) => calls.push({ method: "reconcile", reservationId, input, signal, resolve, reject }))
    },
    "./runHistoryPresentation": presentation,
    "./UserTokenQuota": { default: "UserTokenQuota" },
    "./QuotaManagement": { default: "QuotaManagement" }
  }, { crypto: require("node:crypto") });
  const component = loaded.default;
  return {
    calls,
    formatTokens: loaded.formatQuotaTokens,
    setToken(value) { token = value; },
    render(agentId = "test-agent-a", revision = 0) {
      cursor = 0;
      const tree = component({ agentId, revision, onChanged() {} });
      const effects = pendingEffects;
      pendingEffects = [];
      for (const effect of effects) effect();
      return tree;
    },
    unmount() {
      for (const hook of hooks) hook?.cleanup?.();
      mounted = false;
    }
  };
}
function find(tree, type) {
  if (!tree || typeof tree !== "object") return undefined;
  if (Array.isArray(tree)) {
    for (const child of tree) {
      const match = find(child, type);
      if (match) return match;
    }
    return undefined;
  }
  if (tree.type === type) return tree;
  const children = [...(Array.isArray(tree.props?.children) ? tree.props.children : [tree.props?.children]), ...(tree.props?.items || []).map(item => item.children)];
  for (const child of children) {
    const match = find(child, type);
    if (match) return match;
  }
}
const settle = () => new Promise(resolve => setImmediate(resolve));
function findWhere(tree, predicate) {
  if (!tree || typeof tree !== "object") return undefined;
  if (Array.isArray(tree)) return tree.map(child => findWhere(child, predicate)).find(Boolean);
  if (predicate(tree)) return tree;
  return findWhere([tree.props?.children, ...(tree.props?.items || []).map(item => item.children)], predicate);
}
const record = RunId => ({ RunId, Status: "Completed", StartedAtUtc: "2026-10-02T01:00:00Z" });
test("refresh cancels its predecessor and rejects stale success", async () => {
  const harness = historyHarness();
  const tree = harness.render();
  assert.equal(harness.calls[0].take, 20);
  find(tree, "Button").props.onClick();
  assert.equal(harness.calls[0].signal.aborted, true);
  harness.calls[0].resolve([record("stale")]);
  await settle();
  assert.equal(find(harness.render(), "Collapse"), undefined);
  harness.calls[1].resolve([record("latest")]);
  await settle();
  assert.equal(find(harness.render(), "Collapse").props.items[0].key, "latest");
  harness.unmount();
});
test("Agent switch hides old data and ignores the old request", async () => {
  const harness = historyHarness();
  harness.render();
  harness.render("test-agent-b");
  assert.equal(harness.calls[0].signal.aborted, true);
  harness.calls[0].resolve([record("agent-a")]);
  await settle();
  assert.equal(find(harness.render("test-agent-b"), "Collapse"), undefined);
  harness.calls[1].resolve([record("agent-b")]);
  await settle();
  assert.equal(find(harness.render("test-agent-b"), "Collapse").props.items[0].key, "agent-b");
  harness.unmount();
});
test("session switch hides cached data before effects and cancels the old request", async () => {
  const harness = historyHarness();
  let tree = harness.render();
  harness.calls[0].resolve([record("old-session")]);
  await settle();
  tree = harness.render();
  assert.ok(find(tree, "Collapse"));
  find(tree, "Button").props.onClick();
  harness.setToken("test-session-b");
  assert.equal(find(harness.render(), "Collapse"), undefined);
  assert.equal(harness.calls[1].signal.aborted, true);
  harness.calls[1].resolve([record("stale-session")]);
  harness.calls[2].resolve([record("new-session")]);
  await settle();
  assert.equal(find(harness.render(), "Collapse").props.items[0].key, "new-session");
  harness.unmount();
});
test("refresh failure keeps cached records and shows the error", async () => {
  const harness = historyHarness();
  harness.render();
  harness.calls[0].resolve([record("cached")]);
  await settle();
  find(harness.render(), "Button").props.onClick();
  harness.calls[1].reject(new Error("test-unavailable"));
  await settle();
  const tree = harness.render();
  assert.equal(find(tree, "Alert").props.description, "test-unavailable");
  assert.equal(find(tree, "Collapse").props.items[0].key, "cached");
  assert.equal(find(tree, "Spin").props.spinning, false);
  harness.unmount();
});
test("empty and malformed responses are distinguished from loading/failure", async () => {
  const harness = historyHarness();
  harness.render();
  harness.calls[0].resolve([]);
  await settle();
  let tree = harness.render();
  assert.equal(find(tree, "Alert"), undefined);
  assert.equal(find(tree, "Spin").props.spinning, false);
  find(tree, "Button").props.onClick();
  harness.calls[1].resolve([null]);
  await settle();
  tree = harness.render();
  assert.equal(find(tree, "Alert").props.description, "运行历史响应格式无效");
  harness.unmount();
});
test("closing/unmounting aborts history without late state updates", async () => {
  const harness = historyHarness();
  harness.render();
  harness.unmount();
  assert.equal(harness.calls[0].signal.aborted, true);
  harness.calls[0].resolve([record("after-unmount")]);
  await settle();
});
test("run completion refreshes history and ignores a superseded failure", async () => {
  const harness = historyHarness();
  harness.render();
  harness.render("test-agent-a", 1);
  assert.equal(harness.calls[0].signal.aborted, true);
  harness.calls[0].reject(new Error("stale-failure"));
  harness.calls[1].resolve([record("completed-run")]);
  await settle();
  const tree = harness.render("test-agent-a", 1);
  assert.equal(find(tree, "Alert"), undefined);
  assert.equal(find(tree, "Collapse").props.items[0].key, "completed-run");
  harness.unmount();
});

const disabledQuota = () => ({
  Enabled: false, TimeZoneId: "Asia/Shanghai", EvaluatedAtUtc: "2026-10-02T12:00:00Z",
  RequestReservationTokens: null, CanReserve: null, Periods: []
});
const quota = (used = "30", reserved = "50", remaining = "20") => ({
  Enabled: true, TimeZoneId: "Asia/Shanghai", EvaluatedAtUtc: "2026-10-02T12:00:00Z",
  RequestReservationTokens: "50", CanReserve: BigInt(remaining) >= 50n,
  Periods: [{
    Kind: "Daily", StartUtc: "2026-10-01T16:00:00Z", EndUtc: "2026-10-02T16:00:00Z",
    LimitTokens: "100", UsedTokens: used, ReservedTokens: reserved, RemainingTokens: remaining,
    HasUnknownUsage: false, CanReserve: BigInt(remaining) >= 50n
  }]
});
test("current user quota uses existing HTTP wrapper and no owner selector", async () => {
  const signal = new AbortController().signal;
  const data = quota();
  const api = loadApi(undefined, async (url, params, config) => {
    assert.equal(url, "/Agent/api/agent-usage/quota");
    assert.equal(params, undefined);
    assert.equal(config.signal, signal);
    return { Data: data };
  });
  assert.equal(await api.getCurrentUserTokenQuota(signal), data);
});
test("quota boundary preserves disabled, available, exhausted, frozen and long integer states", () => {
  const api = loadApi();
  assert.equal(api.isAgentUserTokenQuotaBalance(disabledQuota()), true);
  assert.equal(api.isAgentUserTokenQuotaBalance(quota("0", "0", "100")), true);
  assert.equal(api.isAgentUserTokenQuotaBalance(quota("100", "0", "0")), true);
  assert.equal(api.isAgentUserTokenQuotaBalance(quota("101", "0", "0")), true);
  const frozen = quota();
  frozen.Periods[0].RemainingTokens = null;
  frozen.Periods[0].HasUnknownUsage = true;
  assert.equal(api.isAgentUserTokenQuotaBalance(frozen), true);
  const large = quota("0", "0", "9223372036854775807");
  large.Periods[0].LimitTokens = "9223372036854775807";
  assert.equal(api.isAgentUserTokenQuotaBalance(large), true);
});
for (const value of [undefined, null, {}, { ...disabledQuota(), Periods: [null] }, { ...quota(), RequestReservationTokens: 50 }]) {
  test(`quota API rejects malformed or numeric-long response: ${JSON.stringify(value)}`, async () => {
    await assert.rejects(loadApi(undefined, async () => ({ Data: value })).getCurrentUserTokenQuota(), /租户额度响应格式无效/);
  });
}
test("quota boundary rejects inconsistent balance, duplicated windows and false availability", () => {
  const valid = loadApi().isAgentUserTokenQuotaBalance;
  for (const [field, value] of [
    ["LimitTokens", "9223372036854775808"], ["UsedTokens", "-1"], ["ReservedTokens", "1.5"],
    ["RemainingTokens", "21"], ["RemainingTokens", 20], ["CanReserve", true], ["HasUnknownUsage", true],
    ["Kind", "Yearly"], ["EndUtc", "2026-10-02T11:00:00Z"], ["StartUtc", "not-a-date"]
  ]) {
    const data = quota();
    data.Periods[0][field] = value;
    assert.equal(valid(data), false, `${field}: ${value}`);
  }
  const duplicate = quota();
  duplicate.Periods.push(duplicate.Periods[0]);
  assert.equal(valid(duplicate), false);
  assert.equal(valid({ ...quota(), CanReserve: true }), false);
});
test("quota boundary types compile in strict mode independently of project ambient types", () => {
  const source = readFileSync(path.join(root, "src/api/modules/agent.ts"), "utf8");
  const boundary = source.slice(source.indexOf("export interface AgentUserTokenQuotaPeriod"), source.indexOf("const isAgentRunEvent"));
  assert.ok(boundary.includes("isAgentUserTokenQuotaBalance"));
  const file = path.join(root, "quota-contract-boundary.ts"); // virtual file, not written
  // The virtual contract has no imports; project-installed @types must not alter this isolated check.
  const options = { strict: true, noEmit: true, target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext, types: [] };
  const host = ts.createCompilerHost(options);
  const getSourceFile = host.getSourceFile.bind(host);
  host.getSourceFile = (name, languageVersion, onError, shouldCreate) => path.resolve(name).toLowerCase() === file.toLowerCase()
    ? ts.createSourceFile(name, boundary, languageVersion, true)
    : getSourceFile(name, languageVersion, onError, shouldCreate);
  const diagnostics = ts.getPreEmitDiagnostics(ts.createProgram([file], options, host));
  assert.deepEqual(diagnostics.map(item => ts.flattenDiagnosticMessageText(item.messageText, "\n")), []);
});
test("quota UI displays exact long counts without converting to Number", () => {
  const harness = historyHarness("UserTokenQuota");
  assert.equal(harness.formatTokens("9223372036854775807"), "9,223,372,036,854,775,807");
  assert.equal(harness.formatTokens("0"), "0");
  assert.equal(harness.formatTokens(null), "未知");
  harness.unmount();
});
test("quota refresh aborts predecessor and hides old availability during refresh/failure", async () => {
  const harness = historyHarness("UserTokenQuota");
  harness.render();
  harness.calls[0].resolve(quota());
  await settle();
  let tree = harness.render();
  assert.ok(find(tree, "Descriptions"));
  find(tree, "Button").props.onClick();
  tree = harness.render();
  assert.equal(find(tree, "Descriptions"), undefined);
  find(tree, "Button").props.onClick();
  assert.equal(harness.calls[1].signal.aborted, true);
  harness.calls[1].resolve(quota("0", "0", "100"));
  harness.calls[2].reject(new Error("offline-unavailable"));
  await settle();
  tree = harness.render();
  assert.equal(find(tree, "Descriptions"), undefined);
  assert.equal(find(tree, "Alert").props.message, "租户额度读取失败，余额未知");
  assert.equal(find(tree, "Spin").props.spinning, false);
  harness.unmount();
});
test("frozen quota shows an unknown balance rather than available capacity", async () => {
  const harness = historyHarness("UserTokenQuota");
  harness.render();
  const frozen = quota();
  frozen.Periods[0].HasUnknownUsage = true;
  frozen.Periods[0].RemainingTokens = null;
  harness.calls[0].resolve(frozen);
  await settle();
  const tree = harness.render();
  const description = find(tree, "Descriptions");
  assert.equal(find(description.props.title, "Tag").props.children, "未知用量，已冻结");
  assert.equal(description.props.children[3].props.children, "未知");
  assert.equal(find(tree, "Alert").props.type, "warning");
  harness.unmount();
});
test("quota session switch immediately hides old owner and ignores its late result", async () => {
  const harness = historyHarness("UserTokenQuota");
  harness.render();
  harness.calls[0].resolve(quota());
  await settle();
  let tree = harness.render();
  find(tree, "Button").props.onClick();
  harness.setToken("test-session-b");
  tree = harness.render();
  assert.equal(find(tree, "Descriptions"), undefined);
  assert.equal(harness.calls[1].signal.aborted, true);
  harness.calls[1].resolve(quota("0", "0", "100"));
  harness.calls[2].resolve(disabledQuota());
  await settle();
  tree = harness.render();
  assert.equal(find(tree, "Descriptions"), undefined);
  assert.equal(find(tree, "Alert").props.message, "租户共享额度未启用");
  harness.unmount();
});
test("Agent switch and run completion reload the shared quota rather than reuse an old snapshot", async () => {
  const harness = historyHarness("UserTokenQuota");
  harness.render();
  harness.render("test-agent-b");
  assert.equal(harness.calls[0].signal.aborted, true);
  harness.calls[0].reject(new Error("stale-failure"));
  harness.render("test-agent-b", 1);
  assert.equal(harness.calls[1].signal.aborted, true);
  harness.calls[1].resolve(quota());
  harness.calls[2].resolve(disabledQuota());
  await settle();
  assert.equal(find(harness.render("test-agent-b", 1), "Alert").props.type, "info");
  harness.unmount();
});
test("closing quota panel aborts requests and does not update after unmount", async () => {
  const harness = historyHarness("UserTokenQuota");
  harness.render();
  harness.unmount();
  assert.equal(harness.calls[0].signal.aborted, true);
  harness.calls[0].resolve(quota());
  await settle();
});

const quotaUser = "50c61a38-f65d-482a-8742-263c27024e63";
const quotaReservation = "37af7e45-e007-4bba-9dbe-23a13af44f69";
const quotaPolicy = (Revision = "1") => ({ UseDefaults: false,
  DailyTotalTokens: "100", MonthlyTotalTokens: null, RequestReservationTokens: "50", Revision });

test("quota management guards keep exact long input and reject invalid contracts", () => {
  const api = loadApi();
  assert.equal(api.parseQuotaTokenInput("9223372036854775807"), "9223372036854775807");
  assert.equal(api.parseQuotaTokenInput(" "), null);
  assert.equal(api.parseQuotaTokenInput("0", true), "0");
  for (const invalid of ["0", "-1", "01", "1.5", "1e3", "9223372036854775808", "NaN"]) assert.throws(() => api.parseQuotaTokenInput(invalid));
  assert.equal(api.isAgentQuotaPolicy(quotaPolicy()), true);
  assert.equal(api.isAgentQuotaPolicy({ ...quotaPolicy(), Revision: 1 }), false);
  assert.equal(api.isAgentQuotaPolicy({ ...quotaPolicy(), DailyTotalTokens: "49" }), false);
  assert.equal(api.isAgentQuotaPolicy({ ...quotaPolicy(), UseDefaults: true }), false);
  assert.equal(api.isQuotaId("00000000-0000-0000-0000-000000000000"), false);
  assert.equal(api.isAgentQuotaPending({ Id: quotaReservation, RunId: quotaUser, ConsumerUserId: quotaUser, State: 5, ReservedTokens: "50", SettledAtUtc: null }), false);
});

test("management API uses current tenant routes, cancellation, validated owner and operation receipt", async () => {
  const signal = new AbortController().signal;
  const access = loadApi(undefined, async (url, params, options) => {
    assert.equal(url, "/Agent/api/agent-usage/management/access"); assert.equal(options.signal, signal); return { Data: { CanManage: false } };
  });
  assert.equal(await access.getAgentQuotaManagementAccess(signal), false);
  await assert.rejects(loadApi(undefined, async () => ({ Data: { CanManage: "true" } })).getAgentQuotaManagementAccess(), /权限响应无效/);
  const api = loadApi(undefined, async () => ({ Data: quotaPolicy() }), {
    put: async (url, input, options) => {
      assert.equal(url, "/Agent/api/agent-usage/management/policy");
      assert.equal(input.DailyTotalTokens, "9223372036854775807"); assert.equal(options.signal, signal);
      return { Data: { ...quotaPolicy("2"), DailyTotalTokens: input.DailyTotalTokens } };
    },
    post: async (url, input, options) => {
      assert.ok(url.endsWith(`/reservations/${quotaReservation}/reconcile`)); assert.equal(options.signal, signal);
      return { Data: { OperationId: input.OperationId, ReservationId: quotaReservation, ActualTokens: input.ActualTokens, AppliedAtUtc: "2026-10-02T12:00:00Z" } };
    }
  });
  await api.saveAgentQuotaPolicy({ DailyTotalTokens: "9223372036854775807" }, signal);
  await assert.rejects(loadApi(undefined, async () => ({ Data: { UserId: quotaUser } })).getAgentQuotaPolicy(), /设置响应无效/);
  const receipt = await api.reconcileAgentQuota(quotaReservation, { OperationId: quotaUser, ActualTokens: "0" }, signal);
  assert.equal(receipt.ActualTokens, "0");
  await assert.rejects(api.reconcileAgentQuota("invalid", { OperationId: quotaUser, ActualTokens: "0" }), /预占标识无效/);
});

async function managementHarness(records = []) {
  const harness = historyHarness("QuotaManagement");
  assert.equal(harness.render(), null);
  harness.calls[0].resolve(true); await settle();
  let tree = harness.render();
  find(tree, "Button").props.onClick();
  harness.calls[1].resolve(quotaPolicy()); harness.calls[2].resolve(records); await settle();
  tree = harness.render();
  findWhere(tree, item => item.props?.["aria-label"] === "修改或对账原因").props.onChange({ target: { value: "offline confirmed reason" } });
  return harness;
}

test("management UI stays hidden without server authorization and ignores stale authorization", async () => {
  const harness = historyHarness("QuotaManagement");
  harness.render(); harness.setToken("session-b"); assert.equal(harness.render(), null);
  assert.equal(harness.calls[0].signal.aborted, true);
  harness.calls[0].resolve(true); harness.calls[1].resolve(false); await settle();
  assert.equal(harness.render(), null); assert.equal(harness.calls.length, 2);
});

test("management authorization failure is visibly unavailable rather than silently granting access", async () => {
  const harness = historyHarness("QuotaManagement"); harness.render();
  harness.calls[0].reject(new Error("offline access failure")); await settle();
  const tree = harness.render(); assert.ok(find(tree, "Alert").props.message.includes("权限暂时无法确认"));
  assert.equal(find(tree, "Popconfirm"), undefined);
});

test("management save guards repeated confirmation and reuses operation ID after an uncertain failure", async () => {
  const harness = await managementHarness();
  let tree = harness.render();
  const confirm = find(tree, "Popconfirm").props.onConfirm;
  const first = confirm(); confirm();
  assert.equal(harness.calls.filter(call => call.method === "save").length, 1);
  const input = harness.calls[3].input;
  assert.equal(input.ExpectedRevision, "1");
  harness.calls[3].reject(new Error("offline network interruption")); await first;
  tree = harness.render(); const retry = find(tree, "Popconfirm").props.onConfirm();
  assert.equal(harness.calls[4].input.OperationId, input.OperationId);
  harness.calls[4].resolve(quotaPolicy("2")); await retry;
  assert.ok(JSON.stringify(harness.render()).includes("已提交并保存审计记录"));
});

test("manual reconciliation requires confirmation and allows explicit genuine zero", async () => {
  const harness = await managementHarness([{ Id: quotaReservation, RunId: quotaUser, ConsumerUserId: quotaUser, State: 3, ReservedTokens: "50", SettledAtUtc: null }]);
  let tree = harness.render();
  const field = label => findWhere(tree, item => item.props?.["aria-label"] === label);
  find(tree, "Select").props.onChange(quotaReservation);
  field("核实实际总 Token").props.onChange({ target: { value: "0" } });
  field("核查依据引用").props.onChange({ target: { value: "provider-receipt" } });
  tree = harness.render();
  const reconcile = findWhere(tree, item => item.type === "Popconfirm" && item.props?.title?.includes("核实用量"));
  await reconcile.props.onConfirm();
  assert.equal(harness.calls.length, 3); // no confirmation -> no write
  tree = harness.render();
  findWhere(tree, item => item.type === "Checkbox" && item.props?.children?.includes("请求已终止")).props.onChange({ target: { checked: true } });
  tree = harness.render(); const call = findWhere(tree, item => item.type === "Popconfirm" && item.props?.title?.includes("核实用量")).props.onConfirm();
  assert.equal(harness.calls[3].input.ActualTokens, "0"); assert.equal(harness.calls[3].input.Confirmed, true);
  harness.calls[3].resolve({}); await call;
  assert.equal(find(harness.render(), "Select").props.options.length, 0);
});

test("management session switch and unmount cancel outstanding writes without updating the new owner", async () => {
  const harness = await managementHarness();
  const write = find(harness.render(), "Popconfirm").props.onConfirm();
  harness.setToken("session-b"); assert.equal(harness.render(), null);
  assert.equal(harness.calls[3].signal.aborted, true);
  harness.calls[3].resolve(quotaPolicy("2")); await write;
  assert.equal(harness.render(), null);
  harness.unmount(); assert.equal(harness.calls[4].signal.aborted, true);
  harness.calls[4].resolve(true); await settle();
});
