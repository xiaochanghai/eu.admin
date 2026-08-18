import { clear, element, option, setText } from "./dom.js";

export function mcpToolReferencePresentation(reference) {
  return {
    id: reference.ToolVersionId,
    title: reference.ToolName,
    detail: `${reference.ServerCode} · ${reference.Risk} · ${reference.Sha256.slice(0, 10)}`
  };
}

export function knowledgeReferencePresentation(reference) {
  return {
    id: reference.KnowledgeBaseId,
    title: reference.Name || reference.Code,
    detail: `${reference.Code} · REV ${reference.LogicalRevision}`
  };
}

export function createAgentEditor({ onCreate, onReload, onSave, onPublish, onStatus, onExport, onRun, onSetMain }) {
  const drawer = document.querySelector("#agentDrawer");
  const backdrop = document.querySelector("#drawerBackdrop");
  const form = document.querySelector("#agentForm");
  const fields = {
    code: document.querySelector("#codeInput"),
    name: document.querySelector("#nameInput"),
    description: document.querySelector("#descriptionInput"),
    instructions: document.querySelector("#instructionsInput"),
    modelProfileId: document.querySelector("#modelProfileInput"),
    outputMode: document.querySelector("#outputModeInput"),
    outputJsonSchema: document.querySelector("#schemaInput")
  };
  const message = document.querySelector("#editorMessage");
  const publishButton = document.querySelector("#publishButton");
  const statusButton = document.querySelector("#statusButton");
  const archiveButton = document.querySelector("#archiveAgentButton");
  const exportButton = document.querySelector("#exportButton");
  const runButton = document.querySelector("#runButton");
  const setMainAgentButton = document.querySelector("#setMainAgentButton");
  const reloadButton = document.querySelector("#reloadButton");
  const saveButton = document.querySelector("#saveButton");
  let current = null;
  let lastFocused = null;
  let busy = false;
  let dirty = false;
  let publishedSkills = [];
  let publishedTools = [];
  let publishedKnowledge = [];
  let publishedAgents = [];
  let publishedOrchestrations = [];
  let mainAssignment = null;

  function setBusy(value) {
    busy = value;
    for (const button of [saveButton, publishButton, statusButton, archiveButton, exportButton, reloadButton, setMainAgentButton]) button.disabled = value;
    if (!value && current?.RuntimeStatus === "Archived") {
      saveButton.disabled = true;
      publishButton.disabled = true;
    }
    runButton.disabled = value || !current || current.RuntimeStatus !== "Enabled" || !(current.PublishedVersions?.length);
    refreshMainButton();
  }

  function showMessage(text, tone = "") {
    setText(message, text);
    message.dataset.tone = tone;
  }

  function formatEditorError(error) {
    return error?.message || "Agent 操作失败。";
  }

  function setArchivedFieldState(agent) {
    const archived = agent?.RuntimeStatus === "Archived";
    Object.values(fields).forEach(field => { field.disabled = archived; });
    if (archived) {
      form.querySelectorAll(".binding-options input").forEach(input => {
        input.disabled = true;
      });
    }
  }

  function fill(agent) {
    fields.code.value = agent?.Code ?? "";
    fields.name.value = agent?.Name ?? "";
    fields.description.value = agent?.Description ?? "";
    fields.instructions.value = agent?.Draft?.Instructions ?? "";
    fields.modelProfileId.value = agent?.Draft?.ModelProfileId ?? "";
    fields.outputMode.value = agent?.Draft?.OutputMode ?? "Text";
    fields.outputJsonSchema.value = agent?.Draft?.OutputJsonSchema ?? "";
    fields.code.readOnly = Boolean(agent);
    document.querySelector("#schemaField").hidden = fields.outputMode.value !== "Structured";
    setText(document.querySelector("#drawerTitle"), agent ? agent.Name || agent.Code : "创建 Agent");
    setText(document.querySelector("#drawerEyebrow"), agent ? `DRAFT ${agent.Draft.Label} · REV ${agent.LogicalRevision}` : "NEW AGENT");
    publishButton.hidden = !agent;
    statusButton.hidden = !agent || agent.RuntimeStatus === "Archived";
    archiveButton.hidden = !agent;
    exportButton.hidden = !agent;
    runButton.hidden = !agent;
    setMainAgentButton.hidden = !agent;
    runButton.disabled = !agent || agent.RuntimeStatus !== "Enabled" || !(agent.PublishedVersions?.length);
    runButton.title = runButton.disabled ? "需要已启用且至少发布一个版本" : "";
    reloadButton.hidden = true;
    if (agent) setText(statusButton, agent.RuntimeStatus === "Enabled" ? "停用" : "启用");
    if (agent) setText(archiveButton, agent.RuntimeStatus === "Archived" ? "恢复" : "归档");
    saveButton.disabled = agent?.RuntimeStatus === "Archived";
    publishButton.disabled = agent?.RuntimeStatus === "Archived";
    renderVersions(agent?.PublishedVersions ?? []);
    renderSkillBindings(agent?.Draft?.SkillVersionIds ?? []);
    renderToolBindings(agent?.Draft?.ToolVersionIds ?? []);
    renderKnowledgeBindings(agent?.Draft?.KnowledgeBaseIds ?? []);
    renderChildBindings(agent?.Draft?.ChildAgentIds ?? []);
    renderOrchestrationBindings(agent?.Draft?.OrchestrationIds ?? []);
    setArchivedFieldState(agent);
    refreshMainButton();
    dirty = false;
  }

  function refreshMainButton() {
    if (!current) {
      setMainAgentButton.hidden = true;
      return;
    }
    const currentPublishedVersionId = current.PublishedVersions?.at(-1)?.Id;
    const isAssignedAgent = String(mainAssignment?.AgentId) === String(current.Id);
    const isCurrent = isAssignedAgent &&
      String(mainAssignment?.AgentVersionId) === String(currentPublishedVersionId);
    const canAssign = current.RuntimeStatus === "Enabled" && Boolean(current.PublishedVersions?.length);
    setMainAgentButton.hidden = false;
    setMainAgentButton.disabled = busy || isCurrent || !canAssign;
    setMainAgentButton.textContent = isCurrent
      ? "当前 Main Agent"
      : (isAssignedAgent ? "更新 Main Agent 版本" : "设为 Main Agent");
    setMainAgentButton.title = canAssign
      ? (isCurrent ? "此 Agent 已是平台 Main Agent" : "固定到当前最新发布版本")
      : "只有已启用且至少发布一个版本的 Agent 可以设为 Main Agent";
  }

  function renderSkillBindings(selectedIds) {
    const container = document.querySelector("#agentSkillBindings");
    clear(container);
    if (!publishedSkills.length) {
      container.append(element("p", { className: "binding-empty" }, "尚无已发布 SkillVersion。请先在 Skill 管理中发布。"));
      return;
    }
    const selected = new Set(selectedIds.map(String));
    for (const reference of publishedSkills) {
      const checkbox = element("input", { type: "checkbox" });
      checkbox.value = reference.VersionId;
      checkbox.checked = selected.has(String(reference.VersionId));
      checkbox.addEventListener("change", () => { dirty = true; });
      const title = element("strong");
      title.textContent = reference.SkillName || reference.SkillCode;
      const detail = element("small");
      detail.textContent = `${reference.SkillCode} · v${reference.VersionLabel} · ${reference.ManifestSha256.slice(0, 10)}`;
      container.append(element("label", { className: "binding-option" }, checkbox, element("span", {}, title, detail)));
    }
  }

  function renderToolBindings(selectedIds) {
    const container = document.querySelector("#agentToolBindings");
    clear(container);
    if (!publishedTools.length) {
      container.append(element("p", { className: "binding-empty" }, "尚无已分类 MCP 工具。请先同步工具并设置风险等级。"));
      return;
    }
    const selected = new Set(selectedIds.map(String));
    for (const reference of publishedTools) {
      const presentation = mcpToolReferencePresentation(reference);
      const checkbox = element("input", { type: "checkbox" });
      checkbox.value = presentation.id;
      checkbox.checked = selected.has(String(presentation.id));
      checkbox.addEventListener("change", () => { dirty = true; });
      const title = element("strong");
      title.textContent = presentation.title;
      const detail = element("small");
      detail.textContent = presentation.detail;
      container.append(element("label", { className: "binding-option" }, checkbox, element("span", {}, title, detail)));
    }
  }

  function renderKnowledgeBindings(selectedIds) {
    const container = document.querySelector("#agentKnowledgeBindings");
    clear(container);
    if (!publishedKnowledge.length) {
      container.append(element("p", { className: "binding-empty" }, "尚无已启用且完成索引的知识库。"));
      return;
    }
    const selected = new Set(selectedIds.map(String));
    for (const reference of publishedKnowledge) {
      const presentation = knowledgeReferencePresentation(reference);
      const checkbox = element("input", { type: "checkbox" });
      checkbox.value = presentation.id;
      checkbox.checked = selected.has(String(presentation.id));
      checkbox.addEventListener("change", () => { dirty = true; });
      container.append(element("label", { className: "binding-option" }, checkbox,
        element("span", {}, element("strong", {}, presentation.title),
          element("small", {}, presentation.detail))));
    }
  }

  function renderChildBindings(selectedIds) {
    const container = document.querySelector("#agentChildBindings");
    clear(container);
    if (!publishedAgents.length) {
      container.append(element("p", { className: "binding-empty" }, "尚无其他已启用且已发布的 Agent。"));
      return;
    }
    const selected = new Set(selectedIds.map(String));
    for (const reference of publishedAgents) {
      const checkbox = element("input", { type: "checkbox" });
      checkbox.value = reference.Id;
      checkbox.checked = selected.has(String(reference.Id));
      checkbox.disabled = Boolean(current && reference.Id === current.Id);
      checkbox.addEventListener("change", () => { dirty = true; });
      const title = element("strong");
      title.textContent = reference.Name || reference.Code;
      const detail = element("small");
      detail.textContent = checkbox.disabled
        ? `${reference.Code} · 不能选择当前 Agent 自身`
        : `${reference.Code} · v${reference.CurrentPublishedLabel}`;
      container.append(element("label", {
        className: `binding-option${checkbox.disabled ? " is-disabled" : ""}`
      }, checkbox, element("span", {}, title, detail)));
    }
  }

  function renderOrchestrationBindings(selectedIds) {
    const container = document.querySelector("#agentOrchestrationBindings");
    clear(container);
    if (!publishedOrchestrations.length) {
      container.append(element("p", { className: "binding-empty" }, "尚无已启用且已发布的 orchestration。"));
      return;
    }
    const selected = new Set(selectedIds.map(String));
    for (const reference of publishedOrchestrations) {
      const checkbox = element("input", { type: "checkbox" });
      checkbox.value = reference.id;
      checkbox.checked = selected.has(String(reference.id));
      checkbox.addEventListener("change", () => { dirty = true; });
      const title = element("strong");
      title.textContent = reference.name || reference.code;
      const detail = element("small");
      detail.textContent = `${reference.code} · v${reference.currentPublishedLabel}`;
      container.append(element("label", { className: "binding-option" },
        checkbox, element("span", {}, title, detail)));
    }
  }

  function renderVersions(versions) {
    const list = document.querySelector("#versionList");
    clear(list);
    if (!versions.length) {
      list.append(element("li", { className: "version-empty" }, "尚未发布版本。完成配置并发布后，历史将显示在这里。"));
      return;
    }
    [...versions].reverse().forEach((version, index) => {
      const title = element("strong");
      title.textContent = `v${version.Label}`;
      const mode = element("span", { className: "version-mode" });
      mode.textContent = version.OutputMode;
      const detail = element("p");
      detail.textContent = `${version.ModelProfileId} · ${index === 0 ? "当前最新发布" : "不可变历史"}`;
      list.append(element("li", {}, element("div", {}, title, mode), detail));
    });
  }

  function open(agent = null) {
    lastFocused = document.activeElement;
    current = agent;
    fill(agent);
    showMessage("");
    drawer.setAttribute("aria-hidden", "false");
    backdrop.hidden = false;
    document.body.classList.add("drawer-open");
    requestAnimationFrame(() => {
      const focusTarget = agent?.RuntimeStatus === "Archived"
        ? archiveButton
        : fields[agent ? "name" : "code"];
      focusTarget.focus();
    });
  }

  function close() {
    if (busy) return;
    drawer.setAttribute("aria-hidden", "true");
    backdrop.hidden = true;
    document.body.classList.remove("drawer-open");
    lastFocused?.focus?.();
  }

  function values() {
    return {
      code: fields.code.value.trim(),
      name: fields.name.value.trim(),
      description: fields.description.value,
      instructions: fields.instructions.value,
      modelProfileId: fields.modelProfileId.value,
      outputMode: fields.outputMode.value,
      outputJsonSchema: fields.outputMode.value === "Structured" ? fields.outputJsonSchema.value || null : null,
      skillVersionIds: [...document.querySelectorAll("#agentSkillBindings input:checked")].map(input => input.value),
      toolVersionIds: [...document.querySelectorAll("#agentToolBindings input:checked")].map(input => input.value),
      knowledgeBaseIds: [...document.querySelectorAll("#agentKnowledgeBindings input:checked")].map(input => input.value),
      childAgentIds: [...document.querySelectorAll("#agentChildBindings input:checked:not(:disabled)")].map(input => input.value),
      orchestrationIds: [...document.querySelectorAll("#agentOrchestrationBindings input:checked")].map(input => input.value)
    };
  }

  async function execute(action, successMessage) {
    if (busy) return;
    setBusy(true);
    showMessage("正在提交…");
    try {
      const updated = await action();
      if (updated) {
        current = updated;
        fill(updated);
      }
      showMessage(successMessage, "success");
    } catch (error) {
      showMessage(formatEditorError(error), "error");
    } finally {
      setBusy(false);
    }
  }

  form.addEventListener("submit", event => {
    event.preventDefault();
    if (!form.reportValidity()) return;
    const value = values();
    if (!current) {
      execute(async () => {
        const created = await onCreate({ code: value.code, name: value.name, description: value.description });
        current = created;
        fields.code.readOnly = true;
        setText(document.querySelector("#drawerTitle"), value.name || created.Code);
        setText(document.querySelector("#drawerEyebrow"), `DRAFT ${created.Draft.Label} · REV ${created.LogicalRevision}`);
        publishButton.hidden = false;
        statusButton.hidden = false;
        exportButton.hidden = false;
        setText(statusButton, created.RuntimeStatus === "Enabled" ? "停用" : "启用");
        dirty = true;
        return onSave(created.Id, {
          expectedLogicalRevision: created.LogicalRevision,
          name: value.name,
          description: value.description,
          instructions: value.instructions,
          modelProfileId: value.modelProfileId,
          outputMode: value.outputMode,
          outputJsonSchema: value.outputJsonSchema,
          skillVersionIds: value.skillVersionIds,
          toolVersionIds: value.toolVersionIds,
          knowledgeBaseIds: value.knowledgeBaseIds,
          childAgentIds: value.childAgentIds,
          orchestrationIds: value.orchestrationIds
        });
      }, "Agent 已创建，Draft 已保存。");
    } else {
      execute(() => onSave(current.Id, {
        expectedLogicalRevision: current.LogicalRevision,
        name: value.name,
        description: value.description,
        instructions: value.instructions,
        modelProfileId: value.modelProfileId,
        outputMode: value.outputMode,
        outputJsonSchema: value.outputJsonSchema,
        skillVersionIds: value.skillVersionIds,
        toolVersionIds: value.toolVersionIds,
        knowledgeBaseIds: value.knowledgeBaseIds,
        childAgentIds: value.childAgentIds,
        orchestrationIds: value.orchestrationIds
      }), "Draft 已保存。");
    }
  });

  publishButton.addEventListener("click", () => {
    if (!current) return;
    if (dirty) {
      showMessage("当前表单有未保存修改，请先保存 Draft 再发布。", "warning");
      return;
    }
    if (!fields.instructions.value.trim()) {
      showConfigurationField(
        fields.instructions,
        "发布前必须填写 Instructions；当前 Draft 已保存，但尚不满足发布条件。");
      return;
    }
    if (!fields.modelProfileId.value.trim()) {
      showConfigurationField(
        fields.modelProfileId,
        "发布前必须选择 Model Profile；当前 Draft 已保存，但尚不满足发布条件。");
      return;
    }
    if (fields.outputMode.value === "Structured" && !fields.outputJsonSchema.value.trim()) {
      showConfigurationField(
        fields.outputJsonSchema,
        "发布前必须填写 JSON Schema；请定义结构化输出格式并保存 Draft 后重试。");
      return;
    }
    execute(() => onPublish(current.Id, current.LogicalRevision), "新版本已发布，历史版本保持不变。");
  });

  function showConfigurationField(field, text) {
    const configurationTab = document.querySelector('.tab[data-panel="configuration"]');
    configurationTab?.click();
    showMessage(text, "warning");
    field.focus();
  }

  statusButton.addEventListener("click", () => {
    if (!current) return;
    if (dirty) {
      showMessage("当前表单有未保存修改，请先保存 Draft 再变更运行状态。", "warning");
      return;
    }
    const next = current.RuntimeStatus === "Enabled" ? "Disabled" : "Enabled";
    execute(() => onStatus(current.Id, next, current.LogicalRevision), next === "Enabled" ? "Agent 已启用。" : "Agent 已停用。");
  });

  archiveButton.addEventListener("click", () => {
    if (!current) return;
    if (dirty) {
      showMessage("当前表单有未保存修改，请先保存 Draft 再变更归档状态。", "warning");
      return;
    }
    if (current.RuntimeStatus === "Enabled") {
      showMessage("请先停用 Agent，再执行归档。", "warning");
      return;
    }
    const restoring = current.RuntimeStatus === "Archived";
    execute(
      () => onStatus(current.Id, restoring ? "Disabled" : "Archived", current.LogicalRevision),
      restoring ? "Agent 已恢复为停用状态。" : "Agent 已归档。可通过状态筛选恢复。");
  });

  exportButton.addEventListener("click", async () => {
    if (!current || busy) return;
    if (dirty) {
      showMessage("当前表单有未保存修改，请先保存 Draft 再导出。", "warning");
      return;
    }
    setBusy(true);
    showMessage("正在准备配置包…");
    try {
      await onExport(current);
      showMessage("配置包已导出。", "success");
    } catch (error) {
      showMessage(formatEditorError(error), "error");
    } finally {
      setBusy(false);
    }
  });

  runButton.addEventListener("click", () => {
    if (!current || dirty || runButton.disabled) {
      if (dirty) showMessage("请先保存 Draft，再运行已发布版本。", "warning");
      return;
    }
    onRun(current);
  });

  setMainAgentButton.addEventListener("click", async () => {
    if (!current || busy || setMainAgentButton.disabled) return;
    setBusy(true);
    showMessage("正在更新 Main Agent…");
    try {
      mainAssignment = await onSetMain(current);
      refreshMainButton();
      showMessage("Main Agent 已固定到当前最新发布版本。", "success");
    } catch (error) {
      showMessage(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    } finally {
      setBusy(false);
    }
  });

  reloadButton.addEventListener("click", async () => {
    if (!current || busy) return;
    setBusy(true);
    showMessage("正在重新加载服务端最新 Draft…");
    try {
      const latest = await onReload(current.Id);
      current = latest;
      fill(latest);
      showMessage("已重新加载最新 Draft。", "success");
    } catch (error) {
      showMessage(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
    } finally {
      setBusy(false);
    }
  });

  fields.outputMode.addEventListener("change", () => {
    dirty = true;
    document.querySelector("#schemaField").hidden = fields.outputMode.value !== "Structured";
  });

  fields.outputJsonSchema.addEventListener("input", () => {
    dirty = true;
    const hint = document.querySelector("#schemaHint");
    if (!fields.outputJsonSchema.value.trim()) {
      setText(hint, "发布前由服务端执行严格校验。");
      hint.dataset.tone = "";
      return;
    }
    try {
      JSON.parse(fields.outputJsonSchema.value);
      setText(hint, "JSON 语法有效；发布时还会校验 Schema 结构。");
      hint.dataset.tone = "success";
    } catch {
      setText(hint, "JSON 语法尚未完成，Draft 仍可保存。");
      hint.dataset.tone = "warning";
    }
  });

  for (const field of [fields.code, fields.name, fields.description, fields.instructions, fields.modelProfileId]) {
    field.addEventListener("input", () => { dirty = true; });
    field.addEventListener("change", () => { dirty = true; });
  }

  document.querySelector("#closeDrawerButton").addEventListener("click", close);
  backdrop.addEventListener("click", close);
  document.addEventListener("keydown", event => {
    if (drawer.getAttribute("aria-hidden") !== "false") return;
    if (event.key === "Escape") {
      close();
      return;
    }
    if (event.key === "Tab") {
      const focusable = [...drawer.querySelectorAll("button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [href]")]
        .filter(node => !node.hidden && node.getClientRects().length > 0);
      if (!focusable.length) return;
      const first = focusable[0];
      const last = focusable.at(-1);
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }
  });

  document.querySelectorAll(".tab").forEach(tab => {
    tab.addEventListener("click", () => {
      document.querySelectorAll(".tab").forEach(item => {
        const selected = item === tab;
        item.classList.toggle("is-active", selected);
        item.setAttribute("aria-selected", String(selected));
      });
      document.querySelectorAll("[data-panel-content]").forEach(panel => {
        panel.hidden = panel.dataset.panelContent !== tab.dataset.panel;
      });
    });
  });

  return {
    open,
    close,
    update(agent) { current = agent; fill(agent); },
    setModelProfiles(ids) {
      const selected = fields.modelProfileId.value;
      clear(fields.modelProfileId);
      fields.modelProfileId.append(option("", "请选择模型配置"));
      ids.forEach(id => fields.modelProfileId.append(option(id, id)));
      fields.modelProfileId.value = selected;
    },
    setPublishedSkills(references) {
      publishedSkills = [...references];
      renderSkillBindings(current?.Draft?.SkillVersionIds ?? []);
    },
    setPublishedTools(references) {
      publishedTools = [...references];
      renderToolBindings(current?.Draft?.ToolVersionIds ?? []);
    },
    setKnowledgeBases(references) {
      publishedKnowledge = [...references];
      renderKnowledgeBindings(current?.Draft?.KnowledgeBaseIds ?? []);
    },
    setPublishedAgents(references) {
      publishedAgents = [...references];
      renderChildBindings(current?.Draft?.ChildAgentIds ?? []);
    },
    setPublishedOrchestrations(references) {
      publishedOrchestrations = [...references];
      renderOrchestrationBindings(current?.Draft?.OrchestrationIds ?? []);
    },
    setMainAssignment(value) {
      mainAssignment = value;
      refreshMainButton();
    }
  };
}
