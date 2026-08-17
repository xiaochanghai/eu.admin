import { clear, element, setText } from "./dom.js";

const MAX_CONVERSATIONS = 40;
const MAX_MESSAGES = 160;
const MAX_TRACE_ROWS = 160;
const TERMINAL_EVENTS = new Set(["completed", "failed", "cancelled"]);

const TRACE_LABELS = {
  "run-started": "Run",
  "main-agent-started": "Main Agent",
  "route-selected": "Route",
  "child-agent-started": "Child Agent",
  "skill-started": "Skill",
  "knowledge-retrieved": "知识库检索",
  "knowledge-citation": "知识库引用",
  "tool-started": "MCP",
  "tool-succeeded": "MCP",
  "tool-blocked": "MCP",
  "tool-failed": "MCP",
  "approval-required": "Approval",
  "approval-decided": "Approval",
  "approval-resumed": "Approval",
  "business-query-result": "Business Query",
  "orchestration-started": "Orchestration",
  "message": "Message",
  "child-agent-completed": "Child Agent",
  "completed": "Completed",
  "failed": "Failed",
  "cancelled": "Cancelled"
};

const KNOWLEDGE_FAILURE_MESSAGES = Object.freeze({
  KNOWLEDGE_REVISION_STALE: "当前 Main Agent 使用的知识库版本已更新，请管理员重新发布 Agent 并更新 Main Agent 版本。",
  KNOWLEDGE_BINDING_UNAVAILABLE: "当前 Agent 的知识库配置不可用，请管理员检查知识库状态和授权后重新发布 Agent。",
  KNOWLEDGE_ACCESS_DENIED: "当前 Agent 未获得所需知识库权限，请联系管理员调整授权并重新发布 Agent。",
  KNOWLEDGE_SERVICE_UNAVAILABLE: "知识检索服务暂时不可用，请稍后再试或联系管理员。",
  KNOWLEDGE_BASE_UNAVAILABLE: "当前 Agent 的知识库配置不可用，请管理员检查授权和版本后重新发布 Agent。"
});

const BUSINESS_QUERY_FAILURE_MESSAGES = Object.freeze({
  BUSINESS_QUERY_EVIDENCE_REQUIRED: "业务查询没有返回可验证的服务器结果，请管理员检查 SQL Agent 的工具授权和版本。",
  BUSINESS_QUERY_CALL_LIMIT_EXCEEDED: "业务查询 Agent 尝试了多次工具调用，运行已被安全终止。",
  BUSINESS_QUERY_POLICY_DENIED: "当前账号无权查询该业务范围，请联系管理员申请相应权限。",
  BUSINESS_QUERY_PERMISSION_DENIED: "当前账号无权查询该业务范围，请联系管理员申请相应权限。",
  BUSINESS_QUERY_SCOPE_REQUIRED: "当前账号缺少必要的数据范围，请联系管理员配置业务数据范围。",
  BUSINESS_QUERY_SCOPE_CONFLICT: "当前查询超出账号的数据范围，请缩小区域或组织范围。",
  BUSINESS_QUERY_TIME_RANGE_EXCEEDED: "查询时间范围过大，请缩短时间范围后重试。",
  BUSINESS_QUERY_QUOTA_EXCEEDED: "当前业务查询额度已用完，请稍后重试或联系管理员。",
  BUSINESS_QUERY_CATALOG_STALE: "业务数据目录已更新，请管理员同步工具并重新发布 SQL Agent 和 Main Agent。",
  BUSINESS_QUERY_FIELD_UNKNOWN: "当前业务目录不支持问题中的部分字段，请调整问题或联系管理员更新目录。",
  BUSINESS_QUERY_FIELD_INVALID: "当前业务目录不支持问题中的部分字段，请调整问题或联系管理员更新目录。",
  BUSINESS_QUERY_ENTITY_UNKNOWN: "当前业务目录不支持该查询对象，请调整问题或联系管理员更新目录。",
  BUSINESS_QUERY_RESULT_LIMIT_EXCEEDED: "查询结果过大，请缩小时间范围、区域或返回数量。",
  BUSINESS_QUERY_EXECUTION_TIMEOUT: "业务查询超时，请缩小查询范围后重试。",
  BUSINESS_QUERY_TIMEOUT: "业务查询超时，请缩小查询范围后重试。",
  BUSINESS_QUERY_EXECUTION_FAILED: "业务查询服务执行失败，请稍后重试或联系管理员。",
  BUSINESS_QUERY_AUDIT_UNAVAILABLE: "业务查询审计暂时不可用，系统已安全终止本次查询。",
  BUSINESS_QUERY_SERVICE_UNAVAILABLE: "业务查询服务暂时不可用，请稍后重试或联系管理员。"
});

const RUN_FAILURE_MESSAGES = Object.freeze({
  UNIFIED_ENTRY_TIMEOUT: "\u6267\u884c\u5df2\u8d85\u65f6\uff0c\u5df2\u4fdd\u7559\u8d85\u65f6\u524d\u751f\u6210\u7684\u5185\u5bb9\uff1b\u56de\u7b54\u53ef\u80fd\u4e0d\u5b8c\u6574\uff0c\u53ef\u7ee7\u7eed\u8ffd\u95ee\u3002"
});

export function friendlyKnowledgeFailure(errorCode, fallback = "") {
  return KNOWLEDGE_FAILURE_MESSAGES[errorCode] || fallback;
}

export function friendlyRunFailure(errorCode, fallback = "") {
  return RUN_FAILURE_MESSAGES[errorCode]
    || BUSINESS_QUERY_FAILURE_MESSAGES[errorCode]
    || KNOWLEDGE_FAILURE_MESSAGES[errorCode]
    || fallback;
}

export function businessQueryPresentationModel(message) {
  try {
    const value = JSON.parse(message?.businessQueryPresentationJson || "{}");
    return {
      title: String(value.title || "业务查询结果"),
      markdown: String(value.markdown || "暂无结果。"),
      columns: Array.isArray(value.columns) ? value.columns.slice(0, 64) : [],
      rows: Array.isArray(value.rows) ? value.rows.slice(0, 1000) : []
    };
  } catch {
    return null;
  }
}

export function mainAgentPresentation(assignment, agent) {
  if (!assignment) {
    return {
      state: "warning",
      name: "未配置 Main Agent",
      detail: "请在 Agent 管理中选择已启用且已发布的 Agent。",
      canUpdate: false,
      updateLabel: ""
    };
  }
  if (!agent) {
    return {
      state: "warning",
      name: "Main Agent 配置不可用",
      detail: "请检查已配置 Agent 是否存在并重新指派。",
      canUpdate: false,
      updateLabel: ""
    };
  }

  const versions = Array.isArray(agent.PublishedVersions)
    ? agent.PublishedVersions
    : [];
  const pinned = versions.find(version =>
    String(version.Id) === String(assignment.AgentVersionId));
  const latest = versions.at(-1);
  const version = pinned?.Label ? `v${pinned.Label}` : "绑定版本不可用";
  const status = agent.RuntimeStatus === "Enabled" ? "已启用" : "已停用";
  const freshness = !pinned
    ? "请重新指派"
    : latest && String(latest.Id) !== String(pinned.Id)
      ? `可更新至 v${latest.Label}`
      : "当前绑定版本";
  const canUpdate = Boolean(
    pinned && latest && String(latest.Id) !== String(pinned.Id));
  return {
    state: agent.RuntimeStatus === "Enabled" && pinned ? "ready" : "warning",
    name: `Main Agent · ${agent.Name || agent.Code}`,
    detail: [agent.Code, version, status, freshness].filter(Boolean).join(" · "),
    canUpdate,
    updateLabel: canUpdate ? `更新至 v${latest.Label}` : ""
  };
}

function traceCapabilityPresentation(kind, payload) {
  const toolName = payload.toolName || payload.ToolName || "";
  if (payload.capability === "business-query") {
    return { label: "Business Query", argumentsLabel: "QueryPlan", toolName };
  }
  const internal = {
    run_orchestration: ["P7 编排", "P7 编排参数"],
    delegate_to_agent: ["子 Agent", "子 Agent 参数"],
    use_skill: ["Skill", "Skill 参数"],
    search_knowledge: ["知识库", "知识库参数"]
  }[toolName];
  if (internal) {
    return { label: internal[0], argumentsLabel: internal[1], toolName };
  }
  if (kind.startsWith("tool-")) {
    return { label: "MCP", argumentsLabel: "MCP 参数", toolName };
  }
  if (kind === "skill-started") {
    return { label: "Skill", argumentsLabel: "Skill 参数", toolName };
  }
  return {
    label: TRACE_LABELS[kind] || kind || "Event",
    argumentsLabel: "Event 参数",
    toolName
  };
}

export function createChatPage({ api, toast, onUpdateMain, onOpenApproval }) {
  const page = document.querySelector("#chatPage");
  const conversationList = document.querySelector("#chatConversationList");
  const conversationEmpty = document.querySelector("#chatConversationEmpty");
  const timeline = document.querySelector("#chatTimeline");
  const traceList = document.querySelector("#chatTraceList");
  const title = document.querySelector("#chatConversationTitle");
  const meta = document.querySelector("#chatConversationMeta");
  const form = document.querySelector("#chatComposer");
  const input = document.querySelector("#chatInput");
  const sendButton = document.querySelector("#sendChatButton");
  const cancelButton = document.querySelector("#cancelChatRunButton");
  const newButton = document.querySelector("#newChatButton");
  const status = document.querySelector("#chatStatus");
  const traceStatus = document.querySelector("#chatTraceStatus");
  const mainAgentSummary = document.querySelector("#chatMainAgentSummary");
  const mainAgentName = document.querySelector("#chatMainAgentName");
  const mainAgentDetail = document.querySelector("#chatMainAgentDetail");
  const updateMainAgentButton = document.querySelector("#chatUpdateMainAgentButton");
  let displayedMainAgent = null;

  const state = {
    conversations: [],
    selectedConversationId: null,
    selectionRevision: 0,
    conversationListRevision: 0,
    runRevision: 0,
    activeRun: null,
    waitingApproval: null,
    traceRows: [],
    initialized: false
  };

  function safeJson(value) {
    if (!value) return {};
    try {
      const parsed = JSON.parse(value);
      return parsed && typeof parsed === "object" ? parsed : {};
    } catch {
      return { text: String(value) };
    }
  }

  function pretty(value) {
    if (typeof value !== "string") return JSON.stringify(value, null, 2);
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }

  function formatTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    return new Intl.DateTimeFormat("zh-CN", {
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit"
    }).format(date);
  }

  function shortIdentifier(value) {
    const text = String(value || "");
    return text.length > 12 ? `${text.slice(0, 8)}…` : text;
  }

  function setStatus(text, tone = "") {
    setText(status, text);
    status.dataset.tone = tone;
  }

  function setMainAgent(assignment, agent) {
    const presentation = mainAgentPresentation(assignment, agent);
    displayedMainAgent = agent;
    setText(mainAgentName, presentation.name);
    setText(mainAgentDetail, presentation.detail);
    mainAgentSummary.dataset.state = presentation.state;
    updateMainAgentButton.hidden = !presentation.canUpdate || !onUpdateMain;
    updateMainAgentButton.disabled = false;
    setText(updateMainAgentButton, presentation.updateLabel || "更新 Main Agent");
  }

  updateMainAgentButton.addEventListener("click", async () => {
    if (!displayedMainAgent || updateMainAgentButton.disabled || !onUpdateMain) return;
    updateMainAgentButton.disabled = true;
    setStatus("正在更新 Main Agent 版本…");
    try {
      await onUpdateMain(displayedMainAgent);
      setStatus("Main Agent 已更新到最新发布版本。", "success");
      toast("Main Agent 已更新到最新发布版本。", "success");
    } catch (error) {
      updateMainAgentButton.disabled = false;
      setStatus("Main Agent 版本更新失败。", "error");
      toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    }
  });

  function setComposerState(mode, preserveStatus = false) {
    const running = mode === "running" || mode === "cancelling";
    input.disabled = running;
    sendButton.hidden = running;
    cancelButton.hidden = !running;
    cancelButton.disabled = mode === "cancelling";
    form.dataset.state = mode;
    if (mode === "running") setStatus("Main Agent 正在处理…");
    if (mode === "cancelling") setStatus("正在取消运行…", "warning");
    if (mode === "ready" && !preserveStatus) setStatus("就绪");
  }

  function renderConversations() {
    const focusedConversationId =
      document.activeElement?.dataset?.conversationId ?? null;
    clear(conversationList);
    const values = state.conversations.slice(0, MAX_CONVERSATIONS);
    conversationEmpty.hidden = values.length !== 0;
    for (const conversation of values) {
      const button = element("button", {
        className: "chat-conversation-item",
        type: "button"
      });
      button.dataset.conversationId = String(conversation.id);
      button.classList.toggle(
        "is-active",
        String(conversation.id) === String(state.selectedConversationId));
      button.toggleAttribute(
        "aria-current",
        String(conversation.id) === String(state.selectedConversationId));
      const name = element("strong");
      name.textContent = conversation.title || "未命名会话";
      const timestamp = element("small");
      timestamp.textContent = formatTime(conversation.updatedAtUtc);
      button.append(name, timestamp);
      button.addEventListener("click", () => selectConversation(conversation.id));
      conversationList.append(button);
      if (focusedConversationId === String(conversation.id)) {
        button.focus({ preventScroll: true });
      }
    }
  }

  function messageNode(role, content, pending = false) {
    const article = element("article", {
      className: `chat-message ${role === "User" ? "is-user" : "is-assistant"}${pending ? " is-streaming" : ""}`
    });
    const label = element("span", { className: "chat-message-role" });
    label.textContent = role === "User" ? "You" : "Main Agent";
    const body = element("div", { className: "chat-message-content" });
    body.textContent = content;
    article.append(label, body);
    return { article, body };
  }

  function businessQueryMessageNode(message) {
    const rendered = messageNode("Assistant", "");
    rendered.article.classList.add("is-business-query");
    rendered.article.querySelector(".chat-message-role").textContent = "Business Query";
    const presentation = businessQueryPresentationModel(message);
    if (!presentation) {
      rendered.body.textContent = "业务查询结果暂时无法显示，请联系管理员检查结果完整性。";
      return rendered.article;
    }
    const title = element("strong", { className: "business-query-title" });
    title.textContent = presentation.title || "业务查询结果";
    rendered.body.append(title);
    const columns = presentation.columns;
    const rows = presentation.rows;
    if (!columns.length) {
      const fallback = element("pre", { className: "business-query-markdown" });
      fallback.textContent = presentation.markdown || "暂无结果。";
      rendered.body.append(fallback);
      return rendered.article;
    }
    const table = element("table", { className: "business-query-table" });
    const header = element("tr");
    for (const column of columns) {
      const cell = element("th");
      cell.textContent = [column.label || column.key, column.unit, column.currency]
        .filter(Boolean).join(" · ");
      header.append(cell);
    }
    table.append(element("thead", {}, header));
    const body = element("tbody");
    for (const row of rows) {
      const line = element("tr");
      for (const column of columns) {
        const cell = element("td");
        cell.textContent = row?.[column.key]?.displayValue ?? "";
        line.append(cell);
      }
      body.append(line);
    }
    table.append(body);
    rendered.body.append(table);
    return rendered.article;
  }

  function renderMessages(messages) {
    clear(timeline);
    const bounded = [...messages].slice(-MAX_MESSAGES);
    if (!bounded.length) {
      timeline.append(
        element("div", { className: "chat-welcome" },
          element("span", { className: "chat-welcome-mark", ariaHidden: "true" }, "◎"),
          element("h2", {}, "Unified Chat"),
          element("p", {}, "输入任务即可开始。平台会自动使用已配置的 Main Agent 路由能力。")));
      return;
    }
    for (const message of bounded) {
      timeline.append(message.kind === "BusinessQueryResult"
        ? businessQueryMessageNode(message)
        : messageNode(message.role, message.content).article);
    }
    scrollTimeline();
  }

  function scrollTimeline() {
    requestAnimationFrame(() => {
      timeline.scrollTop = timeline.scrollHeight;
    });
  }

  function traceDescription(kind, payload, event) {
    const presentation = traceCapabilityPresentation(kind, payload);
    if (kind === "route-selected") return payload.route || event.route || "direct";
    if (kind === "approval-required") return `等待审批 · ${shortIdentifier(payload.approvalId || event.approvalId)}`;
    if (kind === "approval-decided") return payload.status || "审批已决定";
    if (kind === "approval-resumed") return "已恢复原会话";
    if (kind === "skill-started") {
      return payload.skillName || payload.skillVersionId ||
        presentation.toolName || "Skill";
    }
    if (kind === "knowledge-retrieved") {
      const knowledgeBaseCount = Number(payload.knowledgeBaseCount || 0);
      const knowledgeHitCount = Number(payload.knowledgeHitCount || 0);
      return `检索 ${knowledgeBaseCount} 个知识库，命中 ${knowledgeHitCount} 个分块`;
    }
    if (kind === "knowledge-citation") {
      return payload.text || "知识库引用分块";
    }
    if (kind.startsWith("tool-")) {
      return [
        presentation.toolName ||
          `Tool ${shortIdentifier(payload.toolVersionId)}`,
        payload.errorCode
      ].filter(Boolean).join(" · ");
    }
    if (kind.startsWith("child-agent")) {
      return [
        payload.agentName || `Agent ${shortIdentifier(payload.agentVersionId)}`,
        payload.reason,
        payload.errorCode
      ].filter(Boolean).join(" · ");
    }
    if (kind === "orchestration-started") {
      return [
        payload.orchestrationName ||
          `P7 ${shortIdentifier(payload.orchestrationVersionId)}`,
        payload.reason
      ].filter(Boolean).join(" · ");
    }
    if (kind === "failed") return payload.errorCode || "运行失败";
    if (kind === "cancelled") return "运行已取消";
    if (kind === "completed") return "运行已完成";
    return payload.eventKind || `Depth ${event.depth ?? 0}`;
  }

  function appendSupplierFields(container, value) {
    let source = value;
    if (typeof source === "string") {
      try { source = JSON.parse(source); } catch { return; }
    }
    if (!source || typeof source !== "object") return;
    const fields = ["type", "id", "moduleCode"];
    if (!fields.some(field => Object.hasOwn(source, field))) return;
    const group = element("dl", { className: "supplier-fields" });
    for (const field of fields) {
      if (!Object.hasOwn(source, field)) continue;
      const key = element("dt");
      key.textContent = field;
      const valueNode = element("dd");
      valueNode.textContent = String(source[field]);
      group.append(key, valueNode);
    }
    container.append(group);
  }

  function createPayloadBlock(label, value) {
    const wrap = element("div", { className: "chat-payload-block" });
    const heading = element("span", { className: "chat-payload-label" });
    heading.textContent = label;
    const pre = element("pre", { className: "chat-payload" });
    pre.textContent = pretty(value);
    wrap.append(heading, pre);
    appendSupplierFields(wrap, value);
    return wrap;
  }

  function traceNode(event) {
    const payload = event.payload ?? safeJson(event.payloadJson);
    const presentation = traceCapabilityPresentation(event.kind, payload);
    const details = element("details", { className: `chat-trace-row is-${event.kind}` });
    const summary = element("summary");
    const sequence = element("span", { className: "chat-trace-sequence" });
    sequence.textContent = String(event.sequence ?? "—").padStart(2, "0");
    const copy = element("span", { className: "chat-trace-copy" });
    const label = element("strong");
    label.textContent = presentation.label;
    const description = element("small");
    description.textContent = traceDescription(event.kind, payload, event);
    copy.append(label, description);
    const time = element("time");
    time.textContent = formatTime(event.occurredAtUtc);
    summary.append(sequence, copy, time);
    details.append(summary);

    const body = element("div", { className: "chat-trace-detail" });
    if (payload.argumentsJson) {
      body.append(createPayloadBlock(
        presentation.argumentsLabel,
        payload.argumentsJson));
    }
    if (payload.text) body.append(createPayloadBlock("Raw result", payload.text));
    if (!payload.argumentsJson && !payload.text) body.append(createPayloadBlock("Event payload", payload));
    if (payload.errorCode) {
      const failure = element("p", { className: "chat-failure" });
      const detail = friendlyRunFailure(payload.errorCode, payload.detail || "");
      failure.textContent = `${payload.errorCode}${detail ? ` · ${detail}` : ""}`;
      body.append(failure);
    }
    details.append(body);
    return details;
  }

  function renderTrace() {
    clear(traceList);
    const rows = state.traceRows.slice(-MAX_TRACE_ROWS);
    if (!rows.length) {
      traceList.append(element("p", { className: "chat-trace-empty" }, "运行开始后，这里会按顺序显示 Main Agent、Child Agent、Skill、MCP 与 Orchestration。"));
      setText(traceStatus, "等待运行");
      return;
    }
    for (const row of rows) traceList.append(traceNode(row));
    setText(traceStatus, `${rows.length} 个事件`);
  }

  function addTrace(event) {
    const row = {
      ...event,
      payload: safeJson(event.payloadJson)
    };
    if (!state.traceRows.length) clear(traceList);
    state.traceRows.push(row);
    traceList.append(traceNode(row));
    if (state.traceRows.length > MAX_TRACE_ROWS) {
      state.traceRows.splice(0, state.traceRows.length - MAX_TRACE_ROWS);
      traceList.firstElementChild?.remove();
    }
    setText(traceStatus, `${state.traceRows.length} 个事件`);
  }

  function historicalTrace(details) {
    const rows = [];
    for (const run of details?.agentRuns ?? []) {
      rows.push({
        kind: run.kind === "Main" ? "main-agent-started" : "child-agent-started",
        sequence: 0,
        occurredAtUtc: run.startedAtUtc,
        depth: run.depth,
        payload: {
          agentVersionId: run.agentVersionId,
          text: run.output,
          errorCode: run.errorCode
        }
      });
    }
    for (const tool of details?.toolCalls ?? []) {
      rows.push({
        kind: tool.status === "Completed" ? "tool-succeeded" :
          tool.status === "Blocked" ? "tool-blocked" : "tool-failed",
        sequence: 0,
        occurredAtUtc: tool.startedAtUtc,
        depth: tool.depth,
        payload: {
          toolName: tool.toolVersionId,
          argumentsJson: tool.argumentsJson,
          text: tool.resultContent,
          errorCode: tool.errorCode
        }
      });
    }
    for (const orchestration of details?.orchestrations ?? []) {
      rows.push({
        kind: "orchestration-started",
        sequence: 0,
        occurredAtUtc: orchestration.startedAtUtc,
        depth: orchestration.depth,
        payload: {
          orchestrationVersionId: orchestration.orchestrationVersionId,
          text: orchestration.output,
          errorCode: orchestration.errorCode
        }
      });
    }
    rows.sort((left, right) =>
      new Date(left.occurredAtUtc).getTime() - new Date(right.occurredAtUtc).getTime());
    rows.forEach((row, index) => { row.sequence = index + 1; });
    return rows.slice(-MAX_TRACE_ROWS);
  }

  async function loadConversations() {
    const revision = ++state.conversationListRevision;
    try {
      const values = await api.listChatConversations(MAX_CONVERSATIONS);
      if (revision !== state.conversationListRevision) return;
      state.conversations = [...values].slice(0, MAX_CONVERSATIONS);
      renderConversations();
    } catch (error) {
      if (revision !== state.conversationListRevision) return;
      toast(`会话读取失败：${error.message}`, "error");
    }
  }

  async function selectConversation(conversationId) {
    if (!conversationId) return;
    stopApprovalWatch();
    const revision = ++state.selectionRevision;
    state.selectedConversationId = conversationId;
    renderConversations();
    setText(title, "正在读取会话…");
    setText(meta, "");
    renderMessages([]);
    state.traceRows = [];
    renderTrace();
    try {
      const [result, runs] = await Promise.all([
        api.chatConversation(conversationId, MAX_MESSAGES),
        api.chatConversationRuns(conversationId, 20)
      ]);
      if (revision !== state.selectionRevision ||
          String(conversationId) !== String(state.selectedConversationId)) return;
      setText(title, result.conversation.title || "未命名会话");
      setText(meta, `${result.messages.length} 条消息 · ${formatTime(result.conversation.updatedAtUtc)}`);
      const latest = runs[0];
      const visibleMessages = [...result.messages];
      const recoverableOutput = String(latest?.output || "");
      if (["Failed", "Cancelled"].includes(latest?.status) &&
          recoverableOutput.trim() &&
          !visibleMessages.some(message =>
            message.role === "Assistant" && message.content === recoverableOutput)) {
        visibleMessages.push({
          role: "Assistant",
          content: recoverableOutput,
          kind: "AssistantNarrative"
        });
      }
      renderMessages(visibleMessages);
      if (latest) {
        const [details, persistedEvents] = await Promise.all([
          api.chatRunDetails(latest.id),
          api.chatRunEvents(latest.id, MAX_TRACE_ROWS)
        ]);
        if (revision !== state.selectionRevision ||
            String(conversationId) !== String(state.selectedConversationId)) return;
        state.traceRows = persistedEvents.length
          ? [...persistedEvents]
            .sort((left, right) => left.sequence - right.sequence)
            .slice(-MAX_TRACE_ROWS)
          : historicalTrace(details);
        renderTrace();
        if (latest.errorCode) {
          const detail = friendlyRunFailure(latest.errorCode);
          setStatus(`${latest.errorCode}${detail ? ` · ${detail}` : ""}`, "error");
        }
        if (latest.status === "WaitingForApproval") {
          await restoreWaitingApproval(latest);
        }
      }
    } catch (error) {
      if (revision !== state.selectionRevision) return;
      setStatus(`${error.errorCode || "CHAT_READ_FAILED"} · ${error.message}`, "error");
    }
  }

  function startNewConversation() {
    stopApprovalWatch();
    state.selectionRevision++;
    state.selectedConversationId = null;
    state.traceRows = [];
    setText(title, "新会话");
    setText(meta, "将由当前 Main Agent 处理");
    renderMessages([]);
    renderTrace();
    renderConversations();
    input.focus();
  }

  async function refreshConversationAfterRun(conversationId, selectionRevision) {
    if (!conversationId) return;
    try {
      const result = await api.chatConversation(conversationId, MAX_MESSAGES);
      if (selectionRevision !== state.selectionRevision ||
          String(conversationId) !== String(state.selectedConversationId)) return;
      setText(title, result.conversation.title || "未命名会话");
      setText(meta, `${result.messages.length} 条消息 · ${formatTime(result.conversation.updatedAtUtc)}`);
    } catch (error) {
      if (selectionRevision !== state.selectionRevision) return;
      toast(`会话刷新失败：${error.message}`, "error");
    }
  }

  function terminalMessage(event, assistantBody) {
    const payload = safeJson(event.payloadJson);
    if (event.kind === "completed") {
      if (!assistantBody.textContent && payload.output) assistantBody.textContent = payload.output;
      setStatus("运行已完成", "success");
    } else if (event.kind === "cancelled") {
      if (!assistantBody.textContent) assistantBody.textContent = "运行已取消。";
      setStatus("运行已取消", "warning");
    } else {
      const code = payload.errorCode || "UNIFIED_ENTRY_FAILED";
      const output = String(payload.output || "");
      if (output.startsWith(assistantBody.textContent)) {
        assistantBody.textContent = output;
      } else if (output && !assistantBody.textContent.includes(output)) {
        assistantBody.textContent += `${assistantBody.textContent ? "\n\n" : ""}${output}`;
      }
      const detail = payload.detail || payload.text ||
        "运行失败，服务端未提供详细原因。";
      const friendlyDetail = friendlyRunFailure(code, detail);
      const failure = KNOWLEDGE_FAILURE_MESSAGES[code] || BUSINESS_QUERY_FAILURE_MESSAGES[code]
        ? friendlyDetail
        : `${code}\n${friendlyDetail}`;
      if (!assistantBody.textContent) assistantBody.textContent = failure;
      else if (!assistantBody.textContent.includes(failure)) {
        assistantBody.textContent += `\n\n${failure}`;
      }
      setStatus(`${code} · ${friendlyDetail}`, "error");
    }
    assistantBody.closest(".chat-message")?.classList.remove("is-streaming");
  }

  async function reconcileDisconnectedRun(active, assistantBody) {
    let authoritativeRun = null;
    for (let attempt = 0; attempt < 4; attempt++) {
      try {
        authoritativeRun = await api.chatRun(active.runId);
      } catch {
        authoritativeRun = null;
      }
      const status = String(authoritativeRun?.status || "").toLowerCase();
      if (["completed", "failed", "cancelled", "blocked"].includes(status)) {
        try {
          const events = await api.chatRunEvents(active.runId, MAX_TRACE_ROWS);
          if (state.activeRun === active && Array.isArray(events) && events.length) {
            state.traceRows = [...events]
              .sort((left, right) => left.sequence - right.sequence)
              .slice(-MAX_TRACE_ROWS);
            renderTrace();
          }
        } catch {
          // The authoritative terminal Run is sufficient when event reading fails.
        }
        active.terminal = true;
        terminalMessage({
          kind: status === "blocked" ? "failed" : status,
          payloadJson: JSON.stringify({
            output: authoritativeRun.output,
            errorCode: authoritativeRun.errorCode,
            detail: authoritativeRun.errorCode
              ? "流连接已中断；已从服务端读取最终运行状态。"
              : ""
          })
        }, assistantBody);
        return true;
      }
      if (attempt < 3) {
        await new Promise(resolve => globalThis.setTimeout(resolve, 100 * (2 ** attempt)));
      }
    }

    if (authoritativeRun) {
      setStatus(
        "CHAT_STREAM_RECONCILING · 流已中断，服务端正在终止运行；请从当前会话查看最终状态。",
        "warning");
      return true;
    }
    return false;
  }

  async function finalizeActiveRunUi(active, abortStream = false) {
    if (state.activeRun !== active) return;
    const conversationId = active.conversationId;
    state.activeRun = null;
    setComposerState("ready", active.terminal || active.cancelRequested || active.waitingApproval);
    if (abortStream) active.controller.abort();
    await loadConversations();
    if (conversationId &&
        active.selectionRevision === state.selectionRevision &&
        String(state.selectedConversationId) === String(conversationId)) {
      await refreshConversationAfterRun(
        conversationId,
        active.selectionRevision);
    }
    if (!page.hidden) input.focus({ preventScroll: true });
  }

  async function submit() {
    const value = input.value.trim();
    if (!value || state.activeRun) return;

    stopApprovalWatch();
    const runRevision = ++state.runRevision;
    const controller = new AbortController();
    const user = messageNode("User", value);
    const assistant = messageNode("Assistant", "", true);
    const welcome = timeline.querySelector(".chat-welcome");
    welcome?.remove();
    timeline.append(user.article, assistant.article);
    scrollTimeline();
    input.value = "";
    state.traceRows = [];
    renderTrace();
    state.activeRun = {
      revision: runRevision,
      controller,
      runId: null,
      conversationId: state.selectedConversationId,
      selectionRevision: state.selectionRevision,
      cancelRequested: false,
      cancelDispatched: false,
      terminal: false,
      waitingApproval: false,
      approvalId: null,
      assistantBody: assistant.body
    };
    setComposerState("running");
    cancelButton.focus({ preventScroll: true });

    try {
      await api.streamChatRun({
        input: value,
        conversationId: state.selectedConversationId,
        signal: controller.signal,
        onOpen: metadata => {
          const active = state.activeRun;
          if (!active || active.revision !== runRevision) return;
          active.runId = active.runId || metadata.runId;
          active.conversationId = active.conversationId || metadata.conversationId;
          if (!state.selectedConversationId &&
              active.conversationId &&
              active.selectionRevision === state.selectionRevision) {
            state.selectedConversationId = active.conversationId;
            active.selectionRevision = ++state.selectionRevision;
            renderConversations();
          }
        },
        onEvent: (name, event) => {
          const active = state.activeRun;
          if (!active || active.revision !== runRevision) return;
          event.kind = event.kind || name;
          active.runId = active.runId || event.runId;
          active.conversationId = active.conversationId || event.conversationId;
          if (!state.selectedConversationId &&
              active.conversationId &&
              active.selectionRevision === state.selectionRevision) {
            state.selectedConversationId = active.conversationId;
            active.selectionRevision = ++state.selectionRevision;
            renderConversations();
          }
          const visible = String(state.selectedConversationId ?? "") ===
            String(active.conversationId ?? "");
          if (!visible) return;
          addTrace(event);
          const payload = safeJson(event.payloadJson);
          if (event.kind === "message" &&
              (event.depth ?? 0) === 0 &&
              payload.eventKind === "Delta") {
            assistant.body.textContent += payload.text || "";
            scrollTimeline();
          }
          if (event.kind === "approval-required") {
            const approvalId = event.approvalId || payload.approvalId;
            active.waitingApproval = true;
            active.approvalId = approvalId;
            assistant.article.classList.remove("is-streaming");
            renderApprovalCard(assistant.body, {
              id: approvalId,
              status: "Pending",
              toolName: payload.toolName,
              risk: payload.risk
            });
            setStatus("等待人工审批", "warning");
            watchApproval({
              id: approvalId,
              conversationId: active.conversationId,
              runId: active.runId,
              body: assistant.body
            });
            scrollTimeline();
          }
          if (TERMINAL_EVENTS.has(event.kind)) {
            active.terminal = true;
            terminalMessage(event, assistant.body);
          }
        }
      });
      if (state.activeRun?.revision === runRevision &&
          !state.activeRun.terminal &&
          !state.activeRun.waitingApproval) {
        setStatus("流已关闭，可继续发送。", "warning");
      }
    } catch (error) {
      if (state.activeRun?.revision !== runRevision) return;
      if (state.activeRun.terminal || state.activeRun.waitingApproval) {
        // A durable terminal or approval event is authoritative even if the
        // transport reports a late close while releasing the response body.
      } else if (error.name === "AbortError" && state.activeRun.cancelRequested) {
        if (!assistant.body.textContent) assistant.body.textContent = "运行已取消。";
        setStatus("运行已取消", "warning");
      } else {
        const reconciled = state.activeRun.runId
          ? await reconcileDisconnectedRun(state.activeRun, assistant.body)
          : false;
        if (!reconciled) {
          const code = error.errorCode || "CHAT_STREAM_DISCONNECTED";
          const friendlyDetail = friendlyRunFailure(code, error.message);
          if (!assistant.body.textContent) {
            assistant.body.textContent = KNOWLEDGE_FAILURE_MESSAGES[code] || BUSINESS_QUERY_FAILURE_MESSAGES[code]
              ? friendlyDetail
              : `${code}\n${friendlyDetail}`;
          }
          setStatus(`${code} · ${friendlyDetail}`, "error");
        }
      }
      assistant.article.classList.remove("is-streaming");
    } finally {
      if (state.activeRun?.revision !== runRevision) return;
      await finalizeActiveRunUi(state.activeRun);
    }
  }

  async function cancel() {
    const active = state.activeRun;
    if (!active || active.terminal || active.cancelDispatched) return;
    active.cancelRequested = true;
    active.cancelDispatched = true;
    setComposerState("cancelling");
    if (!active.runId) {
      active.controller.abort();
      return;
    }
    try {
      await api.cancelChatRun(active.runId);
    } catch (error) {
      if (state.activeRun !== active || active.terminal) return;
      try {
        const run = await api.chatRun(active.runId);
        if (state.activeRun !== active || active.terminal) return;
        const normalized = String(run.status || "").toLowerCase();
        if (["completed", "failed", "cancelled", "blocked"].includes(normalized)) {
          active.terminal = true;
          const terminalKind = normalized === "blocked" ? "failed" : normalized;
          terminalMessage({
            kind: terminalKind,
            payloadJson: JSON.stringify({
              output: run.output,
              errorCode: run.errorCode,
              detail: run.errorCode
                ? "取消请求的响应不明确；已从服务端读取最终运行状态。"
                : ""
            })
          }, active.assistantBody);
          await finalizeActiveRunUi(active, true);
          return;
        }
      } catch {
        // The cancel result is still ambiguous. Do not issue a second cancel.
      }
      setStatus(
        `${error.errorCode || "CANCEL_RESULT_UNKNOWN"} · 取消结果暂不明确，系统将继续等待服务端终态。`,
        "warning");
    }
  }

  form.addEventListener("submit", event => {
    event.preventDefault();
    submit();
  });
  input.addEventListener("keydown", event => {
    if (event.key === "Enter" &&
        !event.shiftKey &&
        !event.isComposing &&
        event.keyCode !== 229) {
      event.preventDefault();
      form.requestSubmit();
    }
  });
  cancelButton.addEventListener("click", cancel);
  newButton.addEventListener("click", startNewConversation);

  function stopApprovalWatch() {
    if (state.waitingApproval?.timer) {
      window.clearInterval(state.waitingApproval.timer);
    }
    state.waitingApproval = null;
  }

  function renderApprovalCard(body, approval) {
    clear(body);
    const highRisk = approval?.risk === "HighRisk";
    const card = element("div", {
      className: `chat-approval-card${highRisk ? " is-high" : ""}`
    });
    const heading = element("strong");
    const statusLabel = {
      Pending: "等待人工审批",
      Approved: "审批已通过，等待恢复",
      Rejected: "审批已拒绝",
      Cancelled: "审批已取消",
      Expired: "审批已过期",
      Consumed: "工具已执行",
      Failed: "工具执行失败",
      Invalidated: "审批已失效"
    }[approval?.status] || "审批状态已更新";
    heading.textContent = statusLabel;
    const copy = element("p");
    copy.textContent = approval?.status === "Pending"
      ? `${approval.toolName || "该工具"} 在调用 MCP 前已暂停；批准只适用于本次冻结参数。`
      : "审批与执行状态已写入服务端，可在审批中心查看完整安全摘要和决定记录。";
    const actions = element("div", { className: "chat-approval-actions" });
    const open = element("button", { className: "button secondary", type: "button" });
    open.textContent = "查看审批详情";
    open.addEventListener("click", () => onOpenApproval?.(approval.id));
    actions.append(open);
    if (approval?.status === "Pending") {
      const cancel = element("button", { className: "button ghost", type: "button" });
      cancel.textContent = "取消申请";
      let armed = false;
      cancel.addEventListener("click", async () => {
        if (!armed) {
          armed = true;
          cancel.textContent = "再次点击确认取消";
          return;
        }
        cancel.disabled = true;
        try {
          await api.cancelToolApproval(approval.id, "Requester cancelled from Unified Chat.");
          await pollApproval();
        } catch (error) {
          cancel.disabled = false;
          toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
        }
      });
      actions.append(cancel);
    }
    if (approval?.status === "Approved") {
      const resume = element("button", { className: "button primary", type: "button" });
      resume.textContent = "恢复执行";
      resume.addEventListener("click", async () => {
        resume.disabled = true;
        await resumeApproval(approval.id);
      });
      actions.append(resume);
    }
    card.append(heading, copy, actions);
    body.append(card);
  }

  async function resumeApproval(id) {
    try {
      await api.resumeToolApproval(id);
      const conversationId = state.waitingApproval?.conversationId;
      stopApprovalWatch();
      setStatus("审批结果已同步到原会话", "success");
      await loadConversations();
      if (conversationId) await selectConversation(conversationId);
    } catch (error) {
      setStatus(`${error.errorCode || "TOOL_APPROVAL_RESUME_FAILED"} · ${error.message}`, "error");
      toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    }
  }

  async function pollApproval() {
    const waiting = state.waitingApproval;
    if (!waiting?.id) return;
    try {
      const value = await api.toolApproval(waiting.id);
      if (state.waitingApproval !== waiting) return;
      const approval = value.approval;
      renderApprovalCard(waiting.body, approval);
      if (approval.status === "Approved" && !waiting.resuming) {
        waiting.resuming = true;
        await resumeApproval(approval.id);
        return;
      }
      if (["Rejected", "Cancelled", "Expired", "Invalidated", "Failed"].includes(approval.status)) {
        waiting.resuming = true;
        await resumeApproval(approval.id);
        return;
      }
      if (approval.status === "Consumed") {
        const conversationId = waiting.conversationId;
        stopApprovalWatch();
        if (conversationId) await selectConversation(conversationId);
      }
    } catch (error) {
      if (error.status === 404) stopApprovalWatch();
    }
  }

  function watchApproval(waiting) {
    stopApprovalWatch();
    state.waitingApproval = waiting;
    waiting.timer = window.setInterval(pollApproval, 2500);
    pollApproval();
  }

  async function restoreWaitingApproval(run) {
    try {
      const approvals = await api.toolApprovals({ take: 200 });
      const approval = approvals.find(value => String(value.entryRunId) === String(run.id));
      if (!approval) return;
      const rendered = messageNode("Assistant", "");
      timeline.append(rendered.article);
      renderApprovalCard(rendered.body, approval);
      setStatus("等待人工审批", "warning");
      watchApproval({
        id: approval.id,
        conversationId: run.conversationId,
        runId: run.id,
        body: rendered.body
      });
      scrollTimeline();
    } catch (error) {
      toast(`审批状态读取失败：${error.message}`, "error");
    }
  }

  return {
    setMainAgent,
    async load() {
      if (!page) return;
      if (!state.initialized) {
        state.initialized = true;
        setComposerState("ready");
      } else if (state.activeRun) {
        return;
      }
      await loadConversations();
      if (state.selectedConversationId) {
        await selectConversation(state.selectedConversationId);
      } else if (state.conversations[0]) {
        await selectConversation(state.conversations[0].id);
      } else {
        startNewConversation();
      }
    },
    focusComposer() {
      input.focus();
    },
    async openConversation(conversationId) {
      await selectConversation(conversationId);
    }
  };
}
