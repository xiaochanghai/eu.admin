import { clear, element, setText } from "./dom.js";

export function createAgentRunner({ api, toast }) {
  const drawer = document.querySelector("#runDrawer");
  const backdrop = document.querySelector("#runDrawerBackdrop");
  const input = document.querySelector("#runInput");
  const output = document.querySelector("#runOutput");
  const status = document.querySelector("#runStatus");
  const meta = document.querySelector("#runMeta");
  const tools = document.querySelector("#runToolEvents");
  const citations = document.querySelector("#runCitations");
  const history = document.querySelector("#runHistory");
  const startButton = document.querySelector("#startRunButton");
  const cancelButton = document.querySelector("#cancelRunButton");
  let current = null;
  let controller = null;
  let startedAt = null;
  const toolRows = new Map();

  function open(agent) {
    current = agent;
    setText(document.querySelector("#runDrawerTitle"), agent.Name || agent.Code);
    setText(document.querySelector("#runDrawerEyebrow"), `PUBLISHED v${agent.PublishedVersions.at(-1)?.Label ?? "—"} · ${agent.Code}`);
    drawer.setAttribute("aria-hidden", "false");
    backdrop.hidden = false;
    document.body.classList.add("drawer-open");
    input.focus();
    loadHistory();
  }

  function close() {
    if (controller) return;
    drawer.setAttribute("aria-hidden", "true");
    backdrop.hidden = true;
    document.body.classList.remove("drawer-open");
  }

  function setRunning(value) {
    startButton.disabled = value;
    input.disabled = value;
    cancelButton.hidden = !value;
  }

  function resetRun() {
    output.textContent = "";
    output.className = "run-output is-running";
    clear(tools);
    clear(citations);
    toolRows.clear();
    setText(meta, "正在连接运行时…");
    setText(status, "运行中，可随时取消。");
    status.dataset.tone = "";
  }

  function renderTool(eventName, value) {
    const id = String(value.toolVersionId);
    let row = toolRows.get(id);
    if (!row) {
      const title = element("strong");
      title.textContent = value.toolName;
      const detail = element("small");
      const result = element("pre", { className: "run-tool-result" });
      row = element("div", { className: "run-tool-event" }, element("div", { className: "run-tool-heading" }, title, detail), result);
      row.detail = detail;
      row.result = result;
      tools.append(row);
      toolRows.set(id, row);
    }
    row.className = `run-tool-event ${eventName}`;
    row.detail.textContent = eventName.replace("tool-", "") + (value.errorCode ? ` · ${value.errorCode}` : "");
    if (value.text) {
      try {
        row.result.textContent = JSON.stringify(JSON.parse(value.text), null, 2);
      } catch {
        row.result.textContent = value.text;
      }
      row.result.hidden = false;
    } else {
      row.result.hidden = true;
    }
  }

  function handleEvent(eventName, value) {
    if (eventName === "started") {
      startedAt = new Date(value.occurredAtUtc);
      setText(meta, `Run ${String(value.runId).slice(0, 8)} · 已启动`);
    } else if (eventName === "delta") {
      output.textContent += value.text;
      output.scrollTop = output.scrollHeight;
    } else if (eventName === "citation") {
      citations.append(element("div", { className: "run-tool-event tool-succeeded" },
        element("strong", {}, value.text)));
    } else if (eventName === "knowledge-retrieved") {
      citations.append(element("div", { className: "run-tool-event tool-succeeded" },
        element("strong", {}, "知识库检索"),
        element("small", {},
          `检索 ${Number(value.knowledgeBaseCount || 0)} 个知识库，命中 ${Number(value.knowledgeHitCount || 0)} 个分块`)));
    } else if (eventName.startsWith("tool-")) {
      renderTool(eventName, value);
    } else if (["completed", "failed", "cancelled"].includes(eventName)) {
      output.className = `run-output ${eventName}`;
      const elapsed = startedAt ? Math.max(0, new Date(value.occurredAtUtc) - startedAt) : 0;
      setText(meta, `${eventName.toUpperCase()} · ${(elapsed / 1000).toFixed(1)}s`);
      setText(status, eventName === "completed" ? "运行完成。" : eventName === "cancelled" ? "运行已取消。" : `运行失败：${value.errorCode || "MODEL_INVOCATION_FAILED"}`);
      status.dataset.tone = eventName === "completed" ? "success" : eventName === "cancelled" ? "warning" : "error";
    }
  }

  async function start() {
    if (!current || !input.value.trim() || controller) {
      if (!input.value.trim()) input.focus();
      return;
    }
    controller = new AbortController();
    setRunning(true);
    resetRun();
    try {
      await api.run(current.Id, input.value.trim(), handleEvent, controller.signal);
    } catch (error) {
      if (error.name === "AbortError") {
        setText(status, "取消请求已发送。");
        status.dataset.tone = "warning";
        output.className = "run-output cancelled";
      } else {
        setText(status, `${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`);
        status.dataset.tone = "error";
        output.className = "run-output failed";
      }
    } finally {
      controller = null;
      setRunning(false);
      await loadHistory();
    }
  }

  async function loadHistory() {
    if (!current) return;
    try {
      const values = await api.runHistory(current.Id, 10);
      clear(history);
      if (!values.length) {
        history.append(element("p", { className: "binding-empty" }, "尚无运行记录。"));
        return;
      }
      for (const value of values) {
        const title = element("strong");
        title.textContent = value.status;
        const detail = element("small");
        const duration = value.finishedAtUtc
          ? Math.max(0, new Date(value.finishedAtUtc) - new Date(value.startedAtUtc))
          : 0;
        detail.textContent = `${new Date(value.startedAtUtc).toLocaleString()} · ${(duration / 1000).toFixed(1)}s · ${value.toolCallCount} 次工具调用`;
        history.append(element("div", { className: `run-history-row ${value.status.toLowerCase()}` }, title, detail));
      }
    } catch (error) {
      toast(`运行审计读取失败：${error.message}`, "error");
    }
  }

  startButton.addEventListener("click", start);
  cancelButton.addEventListener("click", () => controller?.abort());
  document.querySelector("#closeRunDrawerButton").addEventListener("click", close);
  backdrop.addEventListener("click", close);

  return { open, close };
}
