import { clear, element, option, setText } from "./dom.js";

const STATUS_LABELS = Object.freeze({
  Running: "运行中",
  Completed: "已完成",
  Cancelled: "已取消",
  Failed: "失败",
  Pending: "等待",
  Passed: "通过",
  WaitingForApproval: "等待审批",
  Blocked: "已阻断"
});

export function evaluationStatusLabel(status) {
  return STATUS_LABELS[status] || String(status || "未知");
}

export function evaluationComparisonPresentation(report) {
  return {
    title: report?.gatePassed ? "质量门禁通过" : "质量门禁阻断",
    tone: report?.gatePassed ? "passed" : "failed",
    passRates: `${formatPercent(report?.baseline?.passRate)} → ${formatPercent(report?.candidate?.passRate)}`,
    checks: (report?.gateChecks || []).map(check => ({
      code: String(check.code || "unknown"),
      passed: Boolean(check.passed),
      expected: String(check.expected ?? ""),
      actual: String(check.actual ?? "")
    }))
  };
}

export function parseEvaluationRules(value) {
  return String(value || "")
    .split(/\r?\n|,/)
    .map(item => item.trim())
    .filter(Boolean);
}

export function modelJudgePresentation(report) {
  const metrics = (report?.cases || []).flatMap(item => item.metrics || []);
  return {
    title: report?.advisoryPassed ? "Advisory 通过" : "Advisory 未通过",
    tone: report?.advisoryPassed ? "passed" : "failed",
    model: String(report?.modelProfileId || ""),
    configuration: String(report?.configurationSha256 || "").slice(0, 12),
    summary: `${metrics.filter(item => item.passed).length}/${metrics.length} 指标通过`
  };
}

function formatPercent(value) {
  const number = Number(value);
  return Number.isFinite(number) ? `${(number * 100).toFixed(1)}%` : "—";
}

function formatDuration(value) {
  if (value === null || value === undefined) return "—";
  return `${Number(value).toLocaleString()} ms`;
}

function shortId(value) {
  return String(value || "").slice(0, 8) || "—";
}

function statusClass(status) {
  return ["Passed", "Completed"].includes(status) ? "passed"
    : ["Failed", "Cancelled"].includes(status) ? "failed"
      : "pending";
}

function nullableNumber(selector) {
  const value = document.querySelector(selector).value.trim();
  return value === "" ? null : Number(value);
}

export function createEvaluationPage({ api, toast, onOpenTrace }) {
  const page = document.querySelector("#evaluationPage");
  if (!page) return { load() {} };

  const state = {
    initialized: false,
    suites: [],
    agents: [],
    current: null,
    batches: [],
    selectedBatchId: null,
    capabilities: null
  };
  const suiteList = document.querySelector("#evaluationSuiteList");
  const caseList = document.querySelector("#evaluationCaseList");
  const batchList = document.querySelector("#evaluationBatchList");
  const batchDetail = document.querySelector("#evaluationBatchDetail");
  const comparePanel = document.querySelector("#evaluationComparePanel");
  const result = document.querySelector("#evaluationComparisonResult");

  async function load() {
    setText(document.querySelector("#evaluationSuiteStatus"), "正在读取…");
    try {
      const [suites, summaries, capabilities] = await Promise.all([
        api.evaluationSuites(document.querySelector("#evaluationSuiteStatusFilter").value),
        api.list({ status: "Enabled" }),
        api.capabilities()
      ]);
      state.suites = suites;
      state.capabilities = capabilities;
      const published = summaries.filter(value => value.currentPublishedLabel);
      state.agents = await Promise.all(published.map(value => api.get(value.id)));
      renderSuites();
      if (state.current) {
        const exists = suites.some(value => value.id === state.current.id);
        if (exists) await openSuite(state.current.id, false);
        else resetWorkspace();
      }
      state.initialized = true;
    } catch (error) {
      setText(document.querySelector("#evaluationSuiteStatus"), error.message);
    }
  }

  function renderSuites() {
    clear(suiteList);
    setText(document.querySelector("#evaluationSuiteCount"), String(state.suites.length));
    document.querySelector("#evaluationSuiteStatus").hidden = state.suites.length > 0;
    if (!state.suites.length) {
      setText(document.querySelector("#evaluationSuiteStatus"), "还没有评估套件。");
      return;
    }
    for (const suite of state.suites) {
      const button = element("button", {
        className: `evaluation-suite-item${state.current?.id === suite.id ? " is-active" : ""}`,
        type: "button",
        ariaLabel: `打开 ${suite.name || suite.code}`
      },
      element("strong", {}, suite.name || suite.code),
      element("code", {}, suite.code),
      element("small", {}, `${suite.status === "Archived" ? "已归档 · " : ""}${suite.draft?.cases?.length || 0} Case · ${suite.publishedVersions?.length || 0} 版本`));
      button.addEventListener("click", () => openSuite(suite.id));
      suiteList.append(button);
    }
  }

  function resetWorkspace() {
    state.current = null;
    document.querySelector("#evaluationWorkbench").hidden = true;
    document.querySelector("#evaluationEmpty").hidden = false;
    document.querySelector("#evaluationCreatePanel").hidden = true;
    comparePanel.hidden = true;
    document.querySelector("#evaluationModelJudgePanel").hidden = true;
    batchDetail.hidden = true;
    clear(batchList);
    batchList.append(element("p", { className: "evaluation-hint" }, "选择 Suite 后显示批次。"));
  }

  async function openSuite(id, scroll = true) {
    try {
      state.current = await api.evaluationSuite(id);
      document.querySelector("#evaluationEmpty").hidden = true;
      document.querySelector("#evaluationCreatePanel").hidden = true;
      document.querySelector("#evaluationWorkbench").hidden = false;
      setText(document.querySelector("#evaluationSuiteCode"), state.current.code);
      setText(document.querySelector("#evaluationSuiteName"), state.current.name || state.current.code);
      setText(document.querySelector("#evaluationSuiteMeta"),
        `REV ${state.current.logicalRevision} · ${state.current.publishedVersions.length} 个不可变版本`);
      document.querySelector("#evaluationNameInput").value = state.current.name;
      document.querySelector("#evaluationDescriptionInput").value = state.current.description;
      renderCases(state.current.draft?.cases || []);
      renderVersions();
      setArchivedState();
      renderSuites();
      await loadBatches();
      if (scroll) document.querySelector("#evaluationWorkbench").scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (error) {
      showMessage(error, true);
    }
  }

  function showCreate() {
    state.current = null;
    document.querySelector("#evaluationEmpty").hidden = true;
    document.querySelector("#evaluationWorkbench").hidden = true;
    document.querySelector("#evaluationCreatePanel").hidden = false;
    ["#evaluationCreateCode", "#evaluationCreateName", "#evaluationCreateDescription"]
      .forEach(selector => { document.querySelector(selector).value = ""; });
    document.querySelector("#evaluationCreateCode").focus();
    renderSuites();
  }

  async function createSuite() {
    try {
      const created = await api.createEvaluationSuite({
        code: document.querySelector("#evaluationCreateCode").value.trim(),
        name: document.querySelector("#evaluationCreateName").value.trim(),
        description: document.querySelector("#evaluationCreateDescription").value.trim()
      });
      toast("评估 Suite 已创建。", "success");
      await load();
      await openSuite(created.id);
    } catch (error) {
      toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    }
  }

  function renderCases(cases) {
    clear(caseList);
    cases.forEach(value => addCase(value));
    if (!cases.length) {
      caseList.append(element("p", { className: "evaluation-hint evaluation-case-empty" },
        "Draft 还没有 Case。新增 Case 后配置目标版本和至少一条断言。"));
    }
  }

  function publishedVersions(agent) {
    return (agent?.publishedVersions || []).filter(value => value.snapshot);
  }

  function agentSelect(selected) {
    const select = element("select", { className: "evaluation-case-agent", ariaLabel: "目标 Agent" });
    select.append(option("", "选择已发布 Agent"));
    state.agents.forEach(agent => select.append(option(agent.id,
      `${agent.name || agent.code} · ${publishedVersions(agent).length} 版本`)));
    select.value = selected || "";
    return select;
  }

  function fillVersionSelect(select, agentId, selected) {
    clear(select);
    select.append(option("", "选择冻结版本"));
    const agent = state.agents.find(value => value.id === agentId);
    publishedVersions(agent).forEach(version => select.append(option(version.id, `v${version.label}`)));
    select.value = selected || "";
  }

  function addCase(value = {}) {
    caseList.querySelector(".evaluation-case-empty")?.remove();
    const id = value.id || crypto.randomUUID();
    const targetAgent = agentSelect(value.targetAgentId);
    const targetVersion = element("select", { className: "evaluation-case-version", ariaLabel: "目标 Agent 版本" });
    fillVersionSelect(targetVersion, targetAgent.value, value.targetAgentVersionId);
    targetAgent.addEventListener("change", () => fillVersionSelect(targetVersion, targetAgent.value, ""));
    const expectedStatus = element("select", { className: "evaluation-case-status" });
    ["", "Completed", "WaitingForApproval", "Failed", "Cancelled", "Blocked"]
      .forEach(status => expectedStatus.append(option(status, status || "不检查状态")));
    expectedStatus.value = value.specification?.expectedStatus || "";
    const input = textArea("evaluation-case-input", value.input, { rows: "3", maxlength: "32768", placeholder: "发送给冻结 Agent 版本的输入" });
    const includes = textArea("evaluation-case-includes", (value.specification?.outputContains || []).join("\n"), { rows: "3" });
    const excludes = textArea("evaluation-case-excludes", (value.specification?.outputExcludes || []).join("\n"), { rows: "3" });
    const events = textArea("evaluation-case-events", (value.specification?.requiredEventKinds || []).join("\n"), { rows: "2" });
    const remove = element("button", { className: "row-action danger", type: "button" }, "移除");
    const row = element("article", { className: "evaluation-case", dataset: { caseId: id } },
      element("header", {},
        element("div", {}, element("span", { className: "evaluation-case-index" }, `CASE ${caseList.children.length + 1}`),
          element("input", { className: "evaluation-case-name", value: value.name || "", maxlength: "120", placeholder: "Case 名称", ariaLabel: "Case 名称" })),
        remove),
      element("label", { className: "field" }, element("span", {}, "输入"),
        input),
      element("div", { className: "field-grid two" },
        element("label", { className: "field" }, element("span", {}, "目标 Agent"), targetAgent),
        element("label", { className: "field" }, element("span", {}, "冻结发布版本"), targetVersion)),
      element("details", { className: "evaluation-assertions" },
        element("summary", {}, "确定性断言"),
        element("div", { className: "field-grid two" },
          element("label", { className: "field" }, element("span", {}, "期望 Run 状态"), expectedStatus),
          element("label", { className: "field" }, element("span", {}, "最大工具调用数"),
            element("input", { className: "evaluation-case-max-tools", type: "number", min: "0", max: "1000", value: value.specification?.maximumToolCalls ?? "", placeholder: "不检查" })),
          element("label", { className: "field" }, element("span", {}, "输出必须包含（每行一条）"),
            includes),
          element("label", { className: "field" }, element("span", {}, "输出不得包含（每行一条）"),
            excludes),
          element("label", { className: "field" }, element("span", {}, "必需事件类型（逗号或换行）"),
            events),
          element("label", { className: "field" }, element("span", {}, "最大耗时 ms"),
            element("input", { className: "evaluation-case-max-duration", type: "number", min: "1", max: "3600000", value: value.specification?.maximumDurationMilliseconds ?? "", placeholder: "不检查" })))));
    remove.addEventListener("click", () => {
      row.remove();
      renumberCases();
      if (!caseList.children.length) renderCases([]);
    });
    caseList.append(row);
  }

  function renumberCases() {
    [...caseList.querySelectorAll(".evaluation-case-index")]
      .forEach((node, index) => setText(node, `CASE ${index + 1}`));
  }

  function readCases() {
    return [...caseList.querySelectorAll(".evaluation-case")].map(row => ({
      id: row.dataset.caseId,
      name: row.querySelector(".evaluation-case-name").value.trim(),
      input: row.querySelector(".evaluation-case-input").value,
      targetAgentId: row.querySelector(".evaluation-case-agent").value,
      targetAgentVersionId: row.querySelector(".evaluation-case-version").value,
      specification: {
        expectedStatus: row.querySelector(".evaluation-case-status").value || null,
        outputContains: parseEvaluationRules(row.querySelector(".evaluation-case-includes").value),
        outputExcludes: parseEvaluationRules(row.querySelector(".evaluation-case-excludes").value),
        requiredEventKinds: parseEvaluationRules(row.querySelector(".evaluation-case-events").value),
        maximumToolCalls: numberFrom(row.querySelector(".evaluation-case-max-tools").value),
        maximumDurationMilliseconds: numberFrom(row.querySelector(".evaluation-case-max-duration").value)
      }
    }));
  }

  function numberFrom(value) {
    return String(value).trim() === "" ? null : Number(value);
  }

  function textArea(className, value, attributes = {}) {
    const node = element("textarea", { className, ...attributes });
    node.value = value || "";
    return node;
  }

  async function saveDraft(quiet = false) {
    if (!state.current || state.current.status === "Archived") return false;
    try {
      state.current = await api.saveEvaluationSuiteDraft(state.current.id, {
        expectedLogicalRevision: state.current.logicalRevision,
        name: document.querySelector("#evaluationNameInput").value.trim(),
        description: document.querySelector("#evaluationDescriptionInput").value,
        cases: readCases()
      });
      renderVersions();
      setText(document.querySelector("#evaluationSuiteMeta"),
        `REV ${state.current.logicalRevision} · ${state.current.publishedVersions.length} 个不可变版本`);
      showMessage("Draft 已保存。", false);
      if (!quiet) await refreshSuiteList();
      return true;
    } catch (error) {
      showMessage(error, true);
      return false;
    }
  }

  async function publishSuite() {
    if (!(await saveDraft(true)) || !state.current) return;
    try {
      state.current = await api.publishEvaluationSuite(state.current.id, state.current.logicalRevision);
      renderVersions();
      showMessage(`已发布 v${state.current.publishedVersions.at(-1).label}。`, false);
      await refreshSuiteList();
    } catch (error) {
      showMessage(error, true);
    }
  }

  async function refreshSuiteList() {
    state.suites = await api.evaluationSuites(document.querySelector("#evaluationSuiteStatusFilter").value);
    renderSuites();
  }

  function setArchivedState() {
    const archived = state.current?.status === "Archived";
    document.querySelector("#archiveEvaluationSuiteButton").textContent = archived ? "恢复" : "归档";
    document.querySelector("#saveEvaluationDraftButton").disabled = archived;
    document.querySelector("#publishEvaluationSuiteButton").disabled = archived;
    document.querySelector("#addEvaluationCaseButton").disabled = archived;
    document.querySelector("#evaluationNameInput").disabled = archived;
    document.querySelector("#evaluationDescriptionInput").disabled = archived;
    caseList.querySelectorAll("input, textarea, select, button").forEach(node => { node.disabled = archived; });
    document.querySelector("#evaluationSuiteMeta").textContent =
      `REV ${state.current.logicalRevision} · ${state.current.publishedVersions.length} 个不可变版本${archived ? " · 已归档（只读）" : ""}`;
    renderVersions();
  }

  async function toggleArchive() {
    if (!state.current) return;
    const archived = state.current.status === "Archived";
    try {
      state.current = await api.archiveEvaluationSuite(
        state.current.id, state.current.logicalRevision, !archived);
      document.querySelector("#evaluationSuiteStatusFilter").value = archived ? "" : "Archived";
      await refreshSuiteList();
      setArchivedState();
      showMessage(archived ? "Evaluation Suite 已恢复为 Active。" : "Evaluation Suite 已归档。", false);
    } catch (error) {
      showMessage(error, true);
    }
  }

  function showMessage(value, failed) {
    const message = document.querySelector("#evaluationEditorMessage");
    const text = value instanceof Error
      ? `${value.message}${value.errorCode ? ` · ${value.errorCode}` : ""}`
      : value;
    setText(message, text);
    message.dataset.tone = failed ? "error" : "success";
  }

  function renderVersions() {
    const select = document.querySelector("#evaluationVersionSelect");
    clear(select);
    const versions = state.current?.publishedVersions || [];
    if (!versions.length) select.append(option("", "尚未发布"));
    versions.slice().reverse().forEach(version => select.append(option(version.id,
      `v${version.label} · ${version.cases.length} Case · ${version.contentSha256.slice(0, 10)}`)));
    document.querySelector("#runEvaluationBatchButton").disabled =
      !versions.length || state.current?.status === "Archived";
  }

  async function runBatch() {
    if (!state.current) return;
    const versionId = document.querySelector("#evaluationVersionSelect").value;
    if (!versionId) return;
    const button = document.querySelector("#runEvaluationBatchButton");
    button.disabled = true;
    setText(button, "运行中…");
    try {
      const batch = await api.runEvaluationBatch(state.current.id, versionId);
      state.selectedBatchId = batch.id;
      toast(`评估批次 ${shortId(batch.id)} 已完成。`, "success");
      await loadBatches();
      renderBatchDetail(batch);
    } catch (error) {
      toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    } finally {
      button.disabled = state.current?.status === "Archived";
      setText(button, "运行批次");
    }
  }

  async function loadBatches() {
    if (!state.current) return;
    state.batches = await api.evaluationBatches(state.current.id, 50);
    renderBatches();
  }

  function renderBatches() {
    clear(batchList);
    setText(document.querySelector("#evaluationBatchCount"), String(state.batches.length));
    if (!state.batches.length) {
      batchList.append(element("p", { className: "evaluation-hint" }, "还没有运行批次。"));
      comparePanel.hidden = true;
      batchDetail.hidden = true;
      return;
    }
    for (const batch of state.batches) {
      const passed = batch.cases.filter(value => value.status === "Passed").length;
      const button = element("button", {
        className: `evaluation-batch-item${state.selectedBatchId === batch.id ? " is-active" : ""}`,
        type: "button"
      },
      element("span", { className: `evaluation-state ${statusClass(batch.status)}` }, evaluationStatusLabel(batch.status)),
      element("strong", {}, `Batch ${shortId(batch.id)}`),
      element("small", {}, `${passed}/${batch.cases.length} 通过 · ${new Date(batch.startedAtUtc).toLocaleString()}`));
      button.addEventListener("click", () => {
        state.selectedBatchId = batch.id;
        renderBatches();
        renderBatchDetail(batch);
      });
      batchList.append(button);
    }
    populateComparisonSelectors();
  }

  function renderBatchDetail(batch) {
    clear(batchDetail);
    batchDetail.hidden = false;
    batchDetail.append(element("header", {},
      element("div", {}, element("strong", {}, `Batch ${shortId(batch.id)}`),
        element("small", {}, `${evaluationStatusLabel(batch.status)} · Suite v${versionLabel(batch.suiteVersionId)}`)),
      element("span", { className: `evaluation-state ${statusClass(batch.status)}` }, evaluationStatusLabel(batch.status))));
    for (const item of batch.cases) {
      const row = element("article", { className: "evaluation-case-result" },
        element("div", {}, element("strong", {}, item.caseName || shortId(item.caseId)),
          element("small", {}, `${evaluationStatusLabel(item.status)} · ${formatDuration(item.durationMilliseconds)} · ${item.toolCallCount || 0} 工具`)),
        element("span", { className: `evaluation-state ${statusClass(item.status)}` }, evaluationStatusLabel(item.status)));
      if (item.unifiedRunId) {
        const trace = element("button", { className: "row-action", type: "button" }, "查看追踪");
        trace.addEventListener("click", () => onOpenTrace(item.unifiedRunId));
        row.append(trace);
      }
      if (item.report?.checks?.length) {
        const details = element("details", {}, element("summary", {}, `${item.report.checks.length} 条断言`));
        item.report.checks.forEach(check => details.append(element("p", { className: check.passed ? "is-pass" : "is-fail" },
          `${check.code} · ${check.passed ? "通过" : "失败"} · expected ${check.expected} · actual ${check.actual}`)));
        row.append(details);
      }
      batchDetail.append(row);
    }
    renderModelJudgePanel(batch);
  }

  function renderModelJudgePanel(batch) {
    const panel = document.querySelector("#evaluationModelJudgePanel");
    panel.hidden = batch.status !== "Completed";
    if (panel.hidden) return;
    const enabled = Boolean(state.capabilities?.features?.modelJudge);
    const availability = document.querySelector("#evaluationModelJudgeAvailability");
    availability.className = `evaluation-state ${enabled ? "passed" : "pending"}`;
    setText(availability, enabled ? "Host 已启用" : "Host 默认关闭");
    const model = document.querySelector("#evaluationJudgeModel");
    clear(model);
    (state.capabilities?.modelProfileIds || []).forEach(id => model.append(option(id, id)));
    const run = document.querySelector("#runEvaluationModelJudgeButton");
    run.disabled = !enabled || !model.options.length || state.current?.status === "Archived";
    setText(run, enabled ? "运行模型裁判" : "需先启用 Host 配置");
    document.querySelector("#evaluationJudgeExplicit").checked = false;
    loadModelJudgeReports(batch.id);
  }

  async function loadModelJudgeReports(batchId) {
    const container = document.querySelector("#evaluationModelJudgeReports");
    clear(container);
    try {
      const reports = await api.modelJudgeReports(batchId, 20);
      if (!reports.length) {
        container.append(element("p", { className: "evaluation-hint" }, "尚无模型裁判报告。"));
        return;
      }
      reports.forEach(report => container.append(renderModelJudgeReport(report)));
    } catch (error) {
      container.append(element("p", { className: "evaluation-hint" }, `报告读取失败：${error.message}`));
    }
  }

  function renderModelJudgeReport(report) {
    const view = modelJudgePresentation(report);
    const article = element("article", { className: `evaluation-judge-report ${view.tone}` },
      element("header", {}, element("strong", {}, view.title), element("span", {}, view.summary)),
      element("small", {}, `${view.model} · ${report.provider} ${report.packageVersion} · CFG ${view.configuration}`));
    const details = element("details", {}, element("summary", {}, `${report.cases.length} 个 Case · 查看 advisory 指标`));
    report.cases.forEach(item => {
      const section = element("section", {}, element("strong", {}, item.caseName));
      item.metrics.forEach(metric => section.append(element("p", { className: metric.passed ? "is-pass" : "is-fail" },
        `${metric.name} · ${metric.score ?? "unavailable"} / 最低 ${metric.minimumScore} · ${metric.passed ? "通过" : "未通过"}`)));
      details.append(section);
    });
    article.append(details);
    return article;
  }

  async function runModelJudge() {
    const batch = state.batches.find(value => value.id === state.selectedBatchId);
    if (!batch) return;
    const evaluators = [];
    const minimumScores = {};
    if (document.querySelector("#evaluationJudgeRelevance").checked) {
      evaluators.push("Relevance");
      minimumScores.Relevance = Number(document.querySelector("#evaluationJudgeRelevanceMinimum").value);
    }
    if (document.querySelector("#evaluationJudgeCoherence").checked) {
      evaluators.push("Coherence");
      minimumScores.Coherence = Number(document.querySelector("#evaluationJudgeCoherenceMinimum").value);
    }
    const button = document.querySelector("#runEvaluationModelJudgeButton");
    button.disabled = true;
    setText(button, "模型评估中…");
    try {
      const report = await api.runModelJudge(batch.id, {
        explicitlyEnabled: document.querySelector("#evaluationJudgeExplicit").checked,
        modelProfileId: document.querySelector("#evaluationJudgeModel").value,
        evaluators,
        minimumScores
      });
      toast(report.advisoryPassed ? "模型裁判 advisory 通过。" : "模型裁判 advisory 未通过。",
        report.advisoryPassed ? "success" : "error");
      document.querySelector("#evaluationJudgeExplicit").checked = false;
      await loadModelJudgeReports(batch.id);
    } catch (error) {
      toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    } finally {
      button.disabled = !Boolean(state.capabilities?.features?.modelJudge)
        || state.current?.status === "Archived";
      setText(button, state.capabilities?.features?.modelJudge ? "运行模型裁判" : "需先启用 Host 配置");
    }
  }

  function versionLabel(id) {
    return state.current?.publishedVersions.find(value => value.id === id)?.label || shortId(id);
  }

  function populateComparisonSelectors() {
    const completed = state.batches.filter(value => value.status === "Completed");
    const baseline = document.querySelector("#evaluationBaselineBatch");
    const candidate = document.querySelector("#evaluationCandidateBatch");
    clear(baseline);
    clear(candidate);
    completed.forEach(batch => {
      const label = `v${versionLabel(batch.suiteVersionId)} · ${shortId(batch.id)} · ${new Date(batch.startedAtUtc).toLocaleString()}`;
      baseline.append(option(batch.id, label));
      candidate.append(option(batch.id, label));
    });
    if (completed.length > 1) {
      baseline.value = completed[1].id;
      candidate.value = completed[0].id;
    }
    comparePanel.hidden = completed.length < 2;
    clear(result);
  }

  async function compareBatches() {
    const baselineBatchId = document.querySelector("#evaluationBaselineBatch").value;
    const candidateBatchId = document.querySelector("#evaluationCandidateBatch").value;
    if (!baselineBatchId || !candidateBatchId || baselineBatchId === candidateBatchId) {
      toast("请选择两个不同的 Completed 批次。", "error");
      return;
    }
    try {
      const report = await api.compareEvaluationBatches({
        baselineBatchId,
        candidateBatchId,
        gate: {
          minimumCandidatePassRate: Number(document.querySelector("#evaluationMinimumPassRate").value),
          maximumPassRateRegression: Number(document.querySelector("#evaluationMaximumPassRegression").value),
          maximumAverageDurationRegressionPercent: nullableNumber("#evaluationMaximumDurationRegression"),
          maximumToolCallIncreasePerCase: nullableNumber("#evaluationMaximumToolIncrease"),
          requireNoNewFailures: document.querySelector("#evaluationNoNewFailures").checked,
          requireSameCaseSet: document.querySelector("#evaluationSameCaseSet").checked,
          requireStableRoutes: document.querySelector("#evaluationStableRoutes").checked
        }
      });
      renderComparison(report);
    } catch (error) {
      toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    }
  }

  function renderComparison(report) {
    clear(result);
    const view = evaluationComparisonPresentation(report);
    result.append(element("header", { className: view.tone },
      element("strong", {}, view.title), element("span", {}, view.passRates)));
    const checks = element("div", { className: "evaluation-gate-checks" });
    view.checks.forEach(check => checks.append(element("div", { className: check.passed ? "is-pass" : "is-fail" },
      element("strong", {}, check.code),
      element("span", {}, check.passed ? "通过" : "阻断"),
      element("small", {}, `期望 ${check.expected} · 实际 ${check.actual}`))));
    result.append(checks);
    if (report.cases?.length) {
      const changed = report.cases.filter(value => value.newFailure || value.routesChanged || value.eventKindsChanged || value.toolCallDelta !== 0);
      if (changed.length) {
        const details = element("details", {}, element("summary", {}, `${changed.length} 个 Case 存在差异`));
        changed.forEach(item => details.append(element("p", {},
          `${item.caseName} · ${item.baselineStatus} → ${item.candidateStatus} · 工具 Δ ${item.toolCallDelta}${item.routesChanged ? " · 路由变化" : ""}`)));
        result.append(details);
      }
    }
  }

  document.querySelector("#createEvaluationSuiteButton").addEventListener("click", showCreate);
  document.querySelector("#cancelEvaluationCreateButton").addEventListener("click", resetWorkspace);
  document.querySelector("#confirmEvaluationCreateButton").addEventListener("click", createSuite);
  document.querySelector("#refreshEvaluationsButton").addEventListener("click", load);
  document.querySelector("#evaluationSuiteStatusFilter").addEventListener("change", load);
  document.querySelector("#archiveEvaluationSuiteButton").addEventListener("click", toggleArchive);
  document.querySelector("#addEvaluationCaseButton").addEventListener("click", () => addCase());
  document.querySelector("#saveEvaluationDraftButton").addEventListener("click", () => saveDraft());
  document.querySelector("#publishEvaluationSuiteButton").addEventListener("click", publishSuite);
  document.querySelector("#runEvaluationBatchButton").addEventListener("click", runBatch);
  document.querySelector("#compareEvaluationBatchesButton").addEventListener("click", compareBatches);
  document.querySelector("#runEvaluationModelJudgeButton").addEventListener("click", runModelJudge);

  return { load };
}
