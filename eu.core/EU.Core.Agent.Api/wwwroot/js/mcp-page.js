import { clear, element, setText } from "./dom.js";
import { mcpApi } from "./mcp-api.js";

export function createMcpPage({ toast, onToolsChanged }) {
  const state = { servers: [], search: "", status: "" };
  const rows = document.querySelector("#mcpRows");
  const listStatus = document.querySelector("#mcpListStatus");
  const table = document.querySelector("#mcpTableWrap");
  const empty = document.querySelector("#mcpEmptyState");
  const count = document.querySelector("#mcpCount");
  const drawer = document.querySelector("#mcpDrawer");
  const backdrop = document.querySelector("#mcpDrawerBackdrop");
  const form = document.querySelector("#mcpForm");
  const message = document.querySelector("#mcpEditorMessage");
  const fields = {
    code: document.querySelector("#mcpCodeInput"),
    name: document.querySelector("#mcpNameInput"),
    description: document.querySelector("#mcpDescriptionInput"),
    transport: document.querySelector("#mcpTransportInput"),
    endpoint: document.querySelector("#mcpEndpointInput"),
    command: document.querySelector("#mcpCommandInput"),
    arguments: document.querySelector("#mcpArgumentsInput"),
    credentialAlias: document.querySelector("#mcpCredentialAliasInput"),
    enabled: document.querySelector("#mcpEnabledInput")
  };
  const saveButton = document.querySelector("#saveMcpButton");
  const syncButton = document.querySelector("#syncMcpButton");
  const statusButton = document.querySelector("#statusMcpButton");
  const archiveButton = document.querySelector("#archiveMcpButton");
  let current = null;
  let timer;
  let busy = false;

  function setBusy(value) {
    busy = value;
    const archived = current?.status === "Archived";
    saveButton.disabled = value || archived;
    syncButton.disabled = value || archived;
    statusButton.disabled = value || archived;
    archiveButton.disabled = value;
    document.querySelectorAll("#mcpToolList select").forEach(node => {
      node.disabled = value || archived;
    });
  }

  function showMessage(text, tone = "") {
    setText(message, text);
    message.dataset.tone = tone;
  }

  function formatArchiveError(error) {
    if (error.errorCode === "MCP_ARCHIVE_BLOCKED") {
      const marker = "Agent(s): ";
      const backendMessage = error.message || "";
      const markerIndex = backendMessage.indexOf(marker);
      const references = markerIndex >= 0
        ? `Agent“${backendMessage.slice(markerIndex + marker.length).replace(/\.$/, "").replace(/, /g, "”、Agent“")}”`
        : "已启用 Agent";
      return `暂时无法归档：${references}仍在使用该 MCP Server。请先解除工具绑定或停用引用方，再重新归档。· 错误码：${error.errorCode}`;
    }
    return `${error.message} · ${error.errorCode || "REQUEST_FAILED"}`;
  }

  function statusBadge(status) {
    const tone = status === "Healthy" ? "enabled" : status === "Unhealthy" ? "danger" : "disabled";
    return element("span", { className: `badge ${tone}` }, status);
  }

  function render() {
    clear(rows);
    setText(count, `共 ${state.servers.length} 个 MCP Server`);
    listStatus.hidden = true;
    table.hidden = state.servers.length === 0;
    empty.hidden = state.servers.length !== 0;
    for (const server of state.servers) {
      const name = element("strong");
      name.textContent = server.name || server.code;
      const code = element("code");
      code.textContent = server.code;
      const open = element("button", { className: "row-action", type: "button" }, "管理");
      open.addEventListener("click", async () => {
        open.disabled = true;
        try { openEditor(await mcpApi.get(server.id)); }
        catch (error) { toast(error.message, "error"); }
        finally { open.disabled = false; }
      });
      rows.append(element("tr", {},
        element("td", {}, element("div", { className: "agent-identity" },
          element("span", { className: "agent-avatar", ariaHidden: "true" }, "◆"),
          element("div", {}, name, code))),
        element("td", {}, server.transport),
        element("td", {}, element("span", { className: "mcp-target" },
          server.transport === "Stdio" ? server.command : server.endpoint)),
        element("td", {}, String(server.currentToolVersionIds?.length ?? 0)),
        element("td", {}, statusBadge(server.status)),
        element("td", {}, open)));
    }
  }

  async function load() {
    listStatus.hidden = false;
    setText(listStatus, "正在加载 MCP Server…");
    table.hidden = true;
    empty.hidden = true;
    try {
      state.servers = await mcpApi.list({ search: state.search, status: state.status });
      render();
    } catch (error) {
      setText(listStatus, `${error.message} · ${error.errorCode || "REQUEST_FAILED"}`);
      setText(count, "读取失败");
    }
  }

  function values() {
    return {
      code: fields.code.value.trim(),
      name: fields.name.value.trim(),
      description: fields.description.value,
      transport: fields.transport.value,
      endpoint: fields.transport.value === "Stdio" ? "" : fields.endpoint.value.trim(),
      command: fields.transport.value === "Stdio" ? fields.command.value.trim() : "",
      arguments: fields.transport.value === "Stdio"
        ? fields.arguments.value.split(/\r?\n/).map(value => value.trim()).filter(Boolean)
        : [],
      credentialAlias: fields.credentialAlias.value.trim(),
      enabled: fields.enabled.checked
    };
  }

  function updateTransportFields() {
    const stdio = fields.transport.value === "Stdio";
    document.querySelector("#mcpHttpFields").hidden = stdio;
    document.querySelector("#mcpStdioFields").hidden = !stdio;
  }

  fields.code.addEventListener("change", () => {
    if (fields.code.value.trim() === "business-query"
        && !fields.credentialAlias.value.trim()) {
      fields.credentialAlias.value = "alias:business-query-local";
    }
  });

  function openEditor(server = null) {
    current = server;
    const archived = server?.status === "Archived";
    fields.code.value = server?.code ?? "";
    fields.code.readOnly = Boolean(server);
    fields.name.value = server?.name ?? "";
    fields.description.value = server?.description ?? "";
    fields.transport.value = server?.transport ?? "StreamableHttp";
    fields.endpoint.value = server?.endpoint ?? "";
    fields.command.value = server?.command ?? "";
    fields.arguments.value = (server?.arguments ?? []).join("\n");
    fields.credentialAlias.value = server?.credentialAlias ?? "";
    fields.enabled.checked = server?.enabled ?? true;
    setText(document.querySelector("#mcpDrawerTitle"), server ? server.name || server.code : "创建 MCP Server");
    setText(document.querySelector("#mcpDrawerEyebrow"), server ? `REV ${server.logicalRevision} · ${server.status}` : "NEW MCP SERVER");
    setText(saveButton, server ? "保存配置" : "创建 Server");
    syncButton.hidden = !server;
    statusButton.hidden = !server || archived;
    archiveButton.hidden = !server;
    if (server) setText(statusButton, server.enabled ? "停用" : "启用");
    if (server) setText(archiveButton, server.status === "Archived" ? "恢复" : "归档");
    saveButton.disabled = archived;
    syncButton.disabled = archived;
    document.querySelector("#mcpToolsSection").hidden = !server;
    updateTransportFields();
    renderTools();
    Object.values(fields).forEach(field => { field.disabled = archived; });
    showMessage("");
    drawer.setAttribute("aria-hidden", "false");
    backdrop.hidden = false;
    document.body.classList.add("drawer-open");
    (archived ? archiveButton : fields[server ? "name" : "code"]).focus();
  }

  function closeEditor() {
    if (busy) return;
    drawer.setAttribute("aria-hidden", "true");
    backdrop.hidden = true;
    document.body.classList.remove("drawer-open");
  }

  function renderTools() {
    const container = document.querySelector("#mcpToolList");
    clear(container);
    if (!current?.currentToolVersionIds?.length) {
      container.append(element("p", { className: "binding-empty" }, "尚未发现工具。保存配置后执行“同步工具”。"));
      return;
    }
    const versions = new Map((current.toolVersions ?? []).map(tool => [String(tool.id), tool]));
    for (const id of current.currentToolVersionIds) {
      const tool = versions.get(String(id));
      if (!tool) continue;
      const select = element("select", { ariaLabel: `${tool.name} 风险等级` },
        ...["Unknown", "ReadOnly", "Mutating", "HighRisk"].map(risk => {
          const item = element("option", { value: risk }, risk);
          item.selected = risk === tool.risk;
          return item;
        }));
      select.disabled = busy || current?.status === "Archived";
      select.addEventListener("change", async () => {
        setBusy(true);
        showMessage(`正在更新 ${tool.name}…`);
        try {
          current = await mcpApi.classify(current.id, tool.id, {
            expectedLogicalRevision: current.logicalRevision,
            risk: select.value
          });
          renderTools();
          await load();
          await onToolsChanged();
          showMessage("风险分类已保存，新工具版本已生成。", "success");
        } catch (error) {
          showMessage(`${error.message} · ${error.errorCode || "REQUEST_FAILED"}`, "error");
        } finally { setBusy(false); }
      });
      const title = element("strong");
      title.textContent = tool.name;
      const description = element("small");
      description.textContent = tool.description || "无说明";
      container.append(element("div", { className: "mcp-tool-row" },
        element("div", {}, title, description), select));
    }
  }

  form.addEventListener("submit", event => {
    event.preventDefault();
    if (!form.reportValidity() || busy) return;
    setBusy(true);
    showMessage("正在保存…");
    const value = values();
    const action = current
      ? mcpApi.update(current.id, { ...value, expectedLogicalRevision: current.logicalRevision })
      : mcpApi.create(value);
    action.then(async server => {
      current = server;
      openEditor(server);
      await load();
      showMessage("MCP Server 配置已保存。", "success");
    }).catch(error => {
      showMessage(`${error.message} · ${error.errorCode || "REQUEST_FAILED"}`, "error");
    }).finally(() => setBusy(false));
  });

  syncButton.addEventListener("click", async () => {
    if (!current || busy) return;
    setBusy(true);
    showMessage("正在连接并发现工具…");
    try {
      current = await mcpApi.sync(current.id, current.logicalRevision);
      renderTools();
      await load();
      await onToolsChanged();
      showMessage(`同步完成，共发现 ${current.currentToolVersionIds.length} 个工具。`, "success");
    } catch (error) {
      try { current = await mcpApi.get(current.id); renderTools(); } catch { /* retain original failure */ }
      showMessage(`${error.message} · ${error.errorCode || "MCP_DISCOVERY_FAILED"}`, "error");
      await load();
    } finally { setBusy(false); }
  });

  statusButton.addEventListener("click", async () => {
    if (!current || busy || current.status === "Archived") return;
    if (!form.reportValidity()) return;
    const enabling = !current.enabled;
    setBusy(true);
    showMessage(enabling ? "正在启用 MCP Server…" : "正在停用 MCP Server…");
    try {
      current = await mcpApi.update(current.id, {
        ...values(),
        expectedLogicalRevision: current.logicalRevision,
        enabled: enabling
      });
      openEditor(current);
      await load();
      await onToolsChanged();
      showMessage(enabling ? "配置已保存，MCP Server 已启用；请同步工具后使用。" : "配置已保存，MCP Server 已停用；现在可以归档。", "success");
    } catch (error) {
      showMessage(`${error.message} · ${error.errorCode || "REQUEST_FAILED"}`, "error");
    } finally { setBusy(false); }
  });

  archiveButton.addEventListener("click", async () => {
    if (!current || busy) return;
    const restoring = current.status === "Archived";
    if (!restoring && current.enabled) {
      showMessage("请先点击“停用”，再归档 MCP Server。", "warning");
      return;
    }
    setBusy(true);
    showMessage(restoring ? "正在恢复 MCP Server…" : "正在归档 MCP Server…");
    try {
      current = await mcpApi.setArchived(
        current.id,
        current.logicalRevision,
        !restoring);
      openEditor(current);
      await load();
      await onToolsChanged();
      showMessage(restoring ? "MCP Server 已恢复为停用状态。" : "MCP Server 已归档。", "success");
    } catch (error) {
      showMessage(formatArchiveError(error), "error");
    } finally { setBusy(false); }
  });

  fields.transport.addEventListener("change", updateTransportFields);
  document.querySelector("#closeMcpDrawerButton").addEventListener("click", closeEditor);
  backdrop.addEventListener("click", closeEditor);
  document.querySelector("#createMcpButton").addEventListener("click", () => openEditor());
  document.querySelector("#emptyCreateMcpButton").addEventListener("click", () => openEditor());
  document.querySelector("#mcpSearchInput").addEventListener("input", event => {
    clearTimeout(timer);
    timer = setTimeout(() => { state.search = event.target.value.trim(); load(); }, 220);
  });
  document.querySelector("#mcpStatusFilter").addEventListener("change", event => {
    state.status = event.target.value;
    load();
  });

  return { load };
}
