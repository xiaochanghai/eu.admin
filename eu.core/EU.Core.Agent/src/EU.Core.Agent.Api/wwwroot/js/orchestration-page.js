import { clear, element, option, setText } from "./dom.js";

export function createOrchestrationPage({ api, toast }) {
  const rows = document.querySelector("#orchestrationRows");
  const listStatus = document.querySelector("#orchestrationListStatus");
  const table = document.querySelector("#orchestrationTableWrap");
  const empty = document.querySelector("#orchestrationEmptyState");
  const workbench = document.querySelector("#orchestrationWorkbench");
  const message = document.querySelector("#orchestrationMessage");
  const nodesContainer = document.querySelector("#orchestrationNodes");
  const edgesContainer = document.querySelector("#orchestrationEdges");
  const runPanel = document.querySelector("#orchestrationRunPanel");
  const timeline = document.querySelector("#orchestrationRunTimeline");
  let current = null;
  let agents = [];
  let activeRun = null;
  let activeDetails = null;
  let pollTimer = null;

  async function load() {
    listStatus.hidden = false;
    try {
      const [values, agentValues] = await Promise.all([api.orchestrations(), api.list()]);
      agents = agentValues;
      renderList(values);
    } catch (error) {
      setText(listStatus, error.message);
    }
  }

  function renderList(values) {
    clear(rows);
    setText(document.querySelector("#orchestrationCount"), `共 ${values.length} 个编排`);
    listStatus.hidden = true;
    table.hidden = values.length === 0;
    empty.hidden = values.length !== 0;
    for (const value of values) {
      const manage = element("button", { className: "row-action", type: "button" }, "管理");
      manage.addEventListener("click", () => open(value.id));
      rows.append(element("tr", {},
        element("td", {}, element("div", { className: "agent-identity" },
          element("span", { className: "agent-avatar", ariaHidden: "true" }, "F"),
          element("div", {}, element("strong", {}, value.name || value.code), element("code", {}, value.code)))),
        element("td", {}, value.description || "尚未填写说明"),
        element("td", {}, String(value.draftNodeCount)),
        element("td", {}, value.currentPublishedLabel ? `v${value.currentPublishedLabel}` : "仅 Draft"),
        element("td", {}, element("span", { className: `badge ${value.status === "Enabled" ? "enabled" : "disabled"}` }, value.status)),
        element("td", {}, manage)));
    }
  }

  function create() {
    current = null;
    workbench.hidden = false;
    runPanel.hidden = true;
    setText(document.querySelector("#orchestrationWorkbenchTitle"), "创建编排");
    setText(document.querySelector("#orchestrationWorkbenchMeta"), "先添加节点，再保存 Draft。");
    document.querySelector("#orchestrationCodeInput").value = "";
    document.querySelector("#orchestrationCodeInput").readOnly = false;
    document.querySelector("#orchestrationNameInput").value = "";
    document.querySelector("#orchestrationDescriptionInput").value = "";
    document.querySelector("#orchestrationStatusInput").value = "Enabled";
    clear(nodesContainer);
    clear(edgesContainer);
    addNode();
    workbench.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  async function open(id) {
    current = await api.orchestration(id);
    workbench.hidden = false;
    setText(document.querySelector("#orchestrationWorkbenchTitle"), current.name || current.code);
    setText(document.querySelector("#orchestrationWorkbenchMeta"),
      `DRAFT ${current.draft.label} · REV ${current.logicalRevision}`);
    document.querySelector("#orchestrationCodeInput").value = current.code;
    document.querySelector("#orchestrationCodeInput").readOnly = true;
    document.querySelector("#orchestrationNameInput").value = current.name;
    document.querySelector("#orchestrationDescriptionInput").value = current.description;
    document.querySelector("#orchestrationStatusInput").value = current.status;
    clear(nodesContainer);
    clear(edgesContainer);
    current.draft.nodes.forEach(addNode);
    current.draft.edges.forEach(addEdge);
    runPanel.hidden = !(current.publishedVersions?.length);
    setText(message, "");
    workbench.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function agentSelect(selected) {
    const select = element("select", { className: "flow-agent" });
    select.append(option("", "选择已发布 Agent"));
    agents.filter(agent => agent.currentPublishedLabel && agent.runtimeStatus === "Enabled")
      .forEach(agent => select.append(option(agent.id, `${agent.name || agent.code} · v${agent.currentPublishedLabel}`)));
    select.value = selected || "";
    return select;
  }

  function addNode(value = {}) {
    const remove = element("button", { className: "row-action danger", type: "button" }, "移除");
    const row = element("div", { className: "flow-node" },
      element("input", { className: "flow-node-id", value: value.id || `node-${nodesContainer.children.length + 1}`, ariaLabel: "节点 ID" }),
      element("input", { className: "flow-node-name", value: value.name || "", placeholder: "节点名称", ariaLabel: "节点名称" }),
      agentSelect(value.agentId),
      (() => {
        const select = element("select", { className: "flow-input-mode", ariaLabel: "输入模式" });
        ["InitialInput", "PreviousOutput", "Template"].forEach(item => select.append(option(item, item)));
        select.value = value.inputMode || "InitialInput";
        return select;
      })(),
      element("input", { className: "flow-template", value: value.inputTemplate || "", placeholder: "{{previous}}", ariaLabel: "输入模板" }),
      element("input", { className: "flow-retries", type: "number", min: "0", max: "3", value: String(value.maximumRetries ?? 0), ariaLabel: "重试次数" }),
      element("input", { className: "flow-timeout", type: "number", min: "5", max: "600", value: String(value.timeoutSeconds ?? 120), ariaLabel: "超时秒数" }),
      remove);
    remove.addEventListener("click", () => row.remove());
    nodesContainer.append(row);
  }

  function addEdge(value = {}) {
    const remove = element("button", { className: "row-action danger", type: "button" }, "移除");
    const condition = element("select", { className: "flow-condition", ariaLabel: "连接条件" });
    ["Always", "Succeeded", "Failed", "OutputContains"].forEach(item => condition.append(option(item, item)));
    condition.value = value.condition || "Succeeded";
    const row = element("div", { className: "flow-edge" },
      element("input", { className: "flow-from", value: value.fromNodeId || "", placeholder: "起点 ID", ariaLabel: "起点 ID" }),
      element("span", { ariaHidden: "true" }, "→"),
      element("input", { className: "flow-to", value: value.toNodeId || "", placeholder: "终点 ID", ariaLabel: "终点 ID" }),
      condition,
      element("input", { className: "flow-condition-value", value: value.conditionValue || "", placeholder: "条件值", ariaLabel: "条件值" }),
      element("input", { className: "flow-order", type: "number", min: "0", value: String(value.order ?? edgesContainer.children.length), ariaLabel: "匹配顺序" }),
      remove);
    remove.addEventListener("click", () => row.remove());
    edgesContainer.append(row);
  }

  function readNodes() {
    return [...nodesContainer.children].map(row => ({
      id: row.querySelector(".flow-node-id").value.trim(),
      name: row.querySelector(".flow-node-name").value.trim(),
      agentId: row.querySelector(".flow-agent").value,
      inputMode: row.querySelector(".flow-input-mode").value,
      inputTemplate: row.querySelector(".flow-template").value,
      maximumRetries: Number(row.querySelector(".flow-retries").value),
      timeoutSeconds: Number(row.querySelector(".flow-timeout").value)
    }));
  }

  function readEdges() {
    return [...edgesContainer.children].map(row => ({
      fromNodeId: row.querySelector(".flow-from").value.trim(),
      toNodeId: row.querySelector(".flow-to").value.trim(),
      condition: row.querySelector(".flow-condition").value,
      conditionValue: row.querySelector(".flow-condition-value").value,
      order: Number(row.querySelector(".flow-order").value)
    }));
  }

  async function save() {
    try {
      if (!current) {
        const code = document.querySelector("#orchestrationCodeInput").value.trim();
        current = await api.createOrchestration({
          code,
          name: document.querySelector("#orchestrationNameInput").value.trim(),
          description: document.querySelector("#orchestrationDescriptionInput").value
        });
        document.querySelector("#orchestrationCodeInput").readOnly = true;
      }
      const nodes = readNodes();
      current = await api.saveOrchestration(current.id, {
        expectedLogicalRevision: current.logicalRevision,
        name: document.querySelector("#orchestrationNameInput").value.trim(),
        description: document.querySelector("#orchestrationDescriptionInput").value,
        status: document.querySelector("#orchestrationStatusInput").value,
        startNodeId: nodes[0]?.id || "",
        nodes,
        edges: readEdges()
      });
      setText(message, "Draft 已保存。");
      message.dataset.tone = "success";
      await load();
      return true;
    } catch (error) {
      setText(message, `${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`);
      message.dataset.tone = "error";
      return false;
    }
  }

  async function publish() {
    if (!(await save()) || !current) return;
    try {
      current = await api.publishOrchestration(current.id, current.logicalRevision);
      runPanel.hidden = false;
      setText(message, `已发布 v${current.publishedVersions.at(-1).label}。`);
      message.dataset.tone = "success";
      await load();
    } catch (error) {
      setText(message, `${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`);
      message.dataset.tone = "error";
    }
  }

  function renderRun(value) {
    clear(timeline);
    timeline.append(element("div", { className: `flow-run-summary ${value.status.toLowerCase()}` },
      element("strong", {}, value.status),
      element("small", {}, `Run ${String(value.id).slice(0, 8)}${value.errorCode ? ` · ${value.errorCode}` : ""}`)));
    for (const node of value.nodes) {
      timeline.append(element("div", { className: `flow-run-node ${node.status.toLowerCase()}` },
        element("span", { className: "flow-run-marker", ariaHidden: "true" }),
        element("div", {}, element("strong", {}, node.nodeName || node.nodeId),
          element("small", {}, `${node.status} · ${node.attempts} attempt(s) · ${node.outputCharacters} chars${node.errorCode ? ` · ${node.errorCode}` : ""}`))));
    }
    if (activeDetails) {
      timeline.append(payloadBlock("编排输入", activeDetails.input));
      for (const node of value.nodes) {
        const attempts = activeDetails.attempts?.filter(attempt => attempt.nodeId === node.nodeId) || [];
        if (!attempts.length) continue;
        const details = element("details", { className: "flow-run-details" },
          element("summary", {},
            element("strong", {}, `${node.nodeName || node.nodeId} · 节点工具调用明细`),
            element("small", {}, `${attempts.length} attempt(s)`)));
        const body = element("div", { className: "flow-run-node-details" });
        attempts.forEach(attempt => body.append(renderAttempt(attempt)));
        details.append(body);
        timeline.append(details);
      }
    }
    const running = value.status === "Running";
    document.querySelector("#cancelOrchestrationRunButton").hidden = !running;
    document.querySelector("#startOrchestrationRunButton").disabled = running;
  }

  function renderAttempt(attempt) {
    const section = element("section", { className: "flow-run-attempt" },
      element("div", { className: "flow-run-attempt-heading" },
        element("strong", {}, `Attempt ${attempt.attempt}`),
        element("small", {}, `${attempt.status}${attempt.errorCode ? ` · ${attempt.errorCode}` : ""}`)),
      payloadBlock("节点输入", attempt.input),
      payloadBlock("Agent 输出", attempt.output));
    const calls = element("div", { className: "flow-tool-calls" });
    if (!attempt.toolCalls?.length) {
      calls.append(element("p", { className: "muted" }, "本次节点未调用 MCP 工具。"));
    } else {
      attempt.toolCalls.forEach(tool => calls.append(element("article", { className: "flow-tool-call" },
        element("div", { className: "flow-tool-call-heading" },
          element("strong", {}, tool.toolName || "MCP tool"),
          element("small", {}, `${tool.status}${tool.errorCode ? ` · ${tool.errorCode}` : ""}`)),
        payloadBlock("调用参数", tool.argumentsJson),
        payloadBlock("原始返回值", tool.resultContent))));
    }
    section.append(calls);
    return section;
  }

  function payloadBlock(label, value) {
    const pre = element("pre", { className: "flow-payload" });
    pre.textContent = prettyPayload(value);
    return element("div", { className: "flow-payload-block" },
      element("span", { className: "flow-payload-label" }, label),
      pre);
  }

  function prettyPayload(value) {
    const text = value ?? "";
    if (!text) return "（空）";
    try {
      return JSON.stringify(JSON.parse(text), null, 2);
    } catch {
      return text;
    }
  }

  async function startRun() {
    const input = document.querySelector("#orchestrationRunInput").value.trim();
    if (!current || !input) return;
    try {
      activeRun = await api.startOrchestration(current.id, input);
      activeDetails = null;
      document.querySelector("#orchestrationRunOutput").hidden = true;
      renderRun(activeRun);
      poll();
    } catch (error) {
      toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    }
  }

  async function poll() {
    window.clearTimeout(pollTimer);
    if (!current || !activeRun) return;
    try {
      activeRun = await api.orchestrationRun(current.id, activeRun.id);
      renderRun(activeRun);
      if (activeRun.status === "Running") {
        pollTimer = window.setTimeout(poll, 800);
      } else {
        activeDetails = await api.orchestrationRunDetails(current.id, activeRun.id);
        renderRun(activeRun);
        const output = document.querySelector("#orchestrationRunOutput");
        if (activeDetails?.output) {
          output.textContent = activeDetails.output;
          output.hidden = false;
        }
      }
    } catch (error) {
      toast(`运行状态读取失败：${error.message}`, "error");
    }
  }

  async function cancelRun() {
    if (!current || !activeRun) return;
    await api.cancelOrchestrationRun(current.id, activeRun.id);
    poll();
  }

  document.querySelector("#createOrchestrationButton").addEventListener("click", create);
  document.querySelector("#addOrchestrationNodeButton").addEventListener("click", () => addNode());
  document.querySelector("#addOrchestrationEdgeButton").addEventListener("click", () => addEdge());
  document.querySelector("#saveOrchestrationButton").addEventListener("click", save);
  document.querySelector("#publishOrchestrationButton").addEventListener("click", publish);
  document.querySelector("#startOrchestrationRunButton").addEventListener("click", startRun);
  document.querySelector("#cancelOrchestrationRunButton").addEventListener("click", cancelRun);
  return { load };
}
