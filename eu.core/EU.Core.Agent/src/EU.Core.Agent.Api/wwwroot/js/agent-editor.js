import { clear, element, option, setText } from "./dom.js";

export function createAgentEditor({ onCreate, onReload, onSave, onPublish, onStatus, onExport, onRun }) {
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
  const exportButton = document.querySelector("#exportButton");
  const runButton = document.querySelector("#runButton");
  const reloadButton = document.querySelector("#reloadButton");
  const saveButton = document.querySelector("#saveButton");
  let current = null;
  let lastFocused = null;
  let busy = false;
  let dirty = false;
  let publishedSkills = [];
  let publishedTools = [];
  let publishedKnowledge = [];

  function setBusy(value) {
    busy = value;
    for (const button of [saveButton, publishButton, statusButton, exportButton, reloadButton]) button.disabled = value;
    runButton.disabled = value || !current || current.runtimeStatus !== "Enabled" || !(current.publishedVersions?.length);
  }

  function showMessage(text, tone = "") {
    setText(message, text);
    message.dataset.tone = tone;
  }

  function fill(agent) {
    fields.code.value = agent?.code ?? "";
    fields.name.value = agent?.name ?? "";
    fields.description.value = agent?.description ?? "";
    fields.instructions.value = agent?.draft?.instructions ?? "";
    fields.modelProfileId.value = agent?.draft?.modelProfileId ?? "";
    fields.outputMode.value = agent?.draft?.outputMode ?? "Text";
    fields.outputJsonSchema.value = agent?.draft?.outputJsonSchema ?? "";
    fields.code.readOnly = Boolean(agent);
    document.querySelector("#schemaField").hidden = fields.outputMode.value !== "Structured";
    setText(document.querySelector("#drawerTitle"), agent ? agent.name || agent.code : "创建 Agent");
    setText(document.querySelector("#drawerEyebrow"), agent ? `DRAFT ${agent.draft.label} · REV ${agent.logicalRevision}` : "NEW AGENT");
    publishButton.hidden = !agent;
    statusButton.hidden = !agent;
    exportButton.hidden = !agent;
    runButton.hidden = !agent;
    runButton.disabled = !agent || agent.runtimeStatus !== "Enabled" || !(agent.publishedVersions?.length);
    runButton.title = runButton.disabled ? "需要已启用且至少发布一个版本" : "";
    reloadButton.hidden = true;
    if (agent) setText(statusButton, agent.runtimeStatus === "Enabled" ? "停用" : "启用");
    renderVersions(agent?.publishedVersions ?? []);
    renderSkillBindings(agent?.draft?.skillVersionIds ?? []);
    renderToolBindings(agent?.draft?.toolVersionIds ?? []);
    renderKnowledgeBindings(agent?.draft?.knowledgeBaseIds ?? []);
    dirty = false;
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
      checkbox.value = reference.versionId;
      checkbox.checked = selected.has(String(reference.versionId));
      checkbox.addEventListener("change", () => { dirty = true; });
      const title = element("strong");
      title.textContent = reference.skillName || reference.skillCode;
      const detail = element("small");
      detail.textContent = `${reference.skillCode} · v${reference.versionLabel} · ${reference.manifestSha256.slice(0, 10)}`;
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
      const checkbox = element("input", { type: "checkbox" });
      checkbox.value = reference.toolVersionId;
      checkbox.checked = selected.has(String(reference.toolVersionId));
      checkbox.addEventListener("change", () => { dirty = true; });
      const title = element("strong");
      title.textContent = reference.toolName;
      const detail = element("small");
      detail.textContent = `${reference.serverCode} · ${reference.risk} · ${reference.sha256.slice(0, 10)}`;
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
      const checkbox = element("input", { type: "checkbox" });
      checkbox.value = reference.knowledgeBaseId;
      checkbox.checked = selected.has(String(reference.knowledgeBaseId));
      checkbox.addEventListener("change", () => { dirty = true; });
      container.append(element("label", { className: "binding-option" }, checkbox,
        element("span", {}, element("strong", {}, reference.name || reference.code),
          element("small", {}, `${reference.code} · REV ${reference.logicalRevision}`))));
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
      title.textContent = `v${version.label}`;
      const mode = element("span", { className: "version-mode" });
      mode.textContent = version.outputMode;
      const detail = element("p");
      detail.textContent = `${version.modelProfileId} · ${index === 0 ? "当前最新发布" : "不可变历史"}`;
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
    requestAnimationFrame(() => fields[agent ? "name" : "code"].focus());
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
      knowledgeBaseIds: [...document.querySelectorAll("#agentKnowledgeBindings input:checked")].map(input => input.value)
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
      if (error.status === 409) {
        showMessage("数据已被其他操作更新。你的输入仍保留，请复制确认后重新加载。", "warning");
        reloadButton.hidden = !current;
      } else {
        showMessage(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
      }
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
        setText(document.querySelector("#drawerTitle"), value.name || created.code);
        setText(document.querySelector("#drawerEyebrow"), `DRAFT ${created.draft.label} · REV ${created.logicalRevision}`);
        publishButton.hidden = false;
        statusButton.hidden = false;
        exportButton.hidden = false;
        setText(statusButton, created.runtimeStatus === "Enabled" ? "停用" : "启用");
        dirty = true;
        return onSave(created.id, {
          expectedLogicalRevision: created.logicalRevision,
          name: value.name,
          description: value.description,
          instructions: value.instructions,
          modelProfileId: value.modelProfileId,
          outputMode: value.outputMode,
          outputJsonSchema: value.outputJsonSchema,
          skillVersionIds: value.skillVersionIds,
          toolVersionIds: value.toolVersionIds,
          knowledgeBaseIds: value.knowledgeBaseIds
        });
      }, "Agent 已创建，Draft 已保存。");
    } else {
      execute(() => onSave(current.id, {
        expectedLogicalRevision: current.logicalRevision,
        name: value.name,
        description: value.description,
        instructions: value.instructions,
        modelProfileId: value.modelProfileId,
        outputMode: value.outputMode,
        outputJsonSchema: value.outputJsonSchema,
        skillVersionIds: value.skillVersionIds,
        toolVersionIds: value.toolVersionIds,
        knowledgeBaseIds: value.knowledgeBaseIds
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
    execute(() => onPublish(current.id, current.logicalRevision), "新版本已发布，历史版本保持不变。");
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
    const next = current.runtimeStatus === "Enabled" ? "Disabled" : "Enabled";
    execute(() => onStatus(current.id, next, current.logicalRevision), next === "Enabled" ? "Agent 已启用。" : "Agent 已停用。");
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
      showMessage(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
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

  reloadButton.addEventListener("click", async () => {
    if (!current || busy) return;
    setBusy(true);
    showMessage("正在重新加载服务端最新 Draft…");
    try {
      const latest = await onReload(current.id);
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
      renderSkillBindings(current?.draft?.skillVersionIds ?? []);
    },
    setPublishedTools(references) {
      publishedTools = [...references];
      renderToolBindings(current?.draft?.toolVersionIds ?? []);
    },
    setKnowledgeBases(references) {
      publishedKnowledge = [...references];
      renderKnowledgeBindings(current?.draft?.knowledgeBaseIds ?? []);
    }
  };
}
