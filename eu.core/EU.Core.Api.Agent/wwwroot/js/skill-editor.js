import { clear, element, setText } from "./dom.js";

export function createSkillEditor({ api, onChanged, toast }) {
  const drawer = document.querySelector("#skillDrawer");
  const backdrop = document.querySelector("#skillDrawerBackdrop");
  const fields = {
    code: document.querySelector("#skillCodeInput"),
    name: document.querySelector("#skillNameInput"),
    category: document.querySelector("#skillCategoryInput"),
    description: document.querySelector("#skillDescriptionInput")
  };
  const metadataButton = document.querySelector("#saveSkillMetadataButton");
  const fileButton = document.querySelector("#saveSkillFileButton");
  const deleteFileButton = document.querySelector("#deleteSkillFileButton");
  const publishButton = document.querySelector("#publishSkillButton");
  const archiveButton = document.querySelector("#archiveSkillButton");
  const newFilePath = document.querySelector("#newSkillFilePath");
  const newFileButton = document.querySelector("#newSkillFileButton");
  const fileWorkspace = document.querySelector("#skillFileWorkspace");
  const fileList = document.querySelector("#skillFileList");
  const content = document.querySelector("#skillContentInput");
  const message = document.querySelector("#skillEditorMessage");
  let current = null;
  let currentPath = "";
  let currentFilePersisted = false;
  let fileDirty = false;
  let metadataDirty = false;
  let busy = false;

  function showMessage(text, tone = "") {
    setText(message, text);
    message.dataset.tone = tone;
  }

  function setBusy(value) {
    busy = value;
    const archived = current?.status === "Archived";
    for (const button of [metadataButton, fileButton, publishButton]) button.disabled = value || archived;
    archiveButton.disabled = value;
    newFilePath.disabled = value || archived;
    newFileButton.disabled = value || archived;
    syncDeleteFileButton();
  }

  function syncDeleteFileButton() {
    const archived = current?.status === "Archived";
    const isProtectedFile = !currentPath || currentPath.toUpperCase() === "SKILL.MD";
    deleteFileButton.hidden = !current;
    deleteFileButton.disabled = busy || archived || isProtectedFile;
    setText(deleteFileButton, currentPath && !currentFilePersisted ? "取消新建" : "删除文件");
    deleteFileButton.title = isProtectedFile && currentPath
      ? "SKILL.md 是必需入口文件，不能删除"
      : "";
  }

  function formatArchiveError(error) {
    if (error.errorCode === "SKILL_ARCHIVE_BLOCKED") {
      const marker = "Agent(s): ";
      const backendMessage = error.message || "";
      const markerIndex = backendMessage.indexOf(marker);
      const references = markerIndex >= 0
        ? `Agent“${backendMessage.slice(markerIndex + marker.length).replace(/\.$/, "").replace(/, /g, "”、Agent“")}”`
        : "已启用 Agent";
      return `暂时无法归档：${references}仍在使用该 Skill。请先解除 Skill 绑定或停用引用方，再重新归档。· 错误码：${error.errorCode}`;
    }
    return `${error.message} · ${error.errorCode ?? "ARCHIVE_FAILED"}`;
  }

  function nextVersion() {
    const majors = (current?.publishedVersions ?? [])
      .map(version => Number.parseInt(version.label.split(".")[0], 10))
      .filter(Number.isFinite);
    return `${(majors.length ? Math.max(...majors) : 0) + 1}.0.0`;
  }

  function fill(skill) {
    current = skill;
    const archived = skill?.status === "Archived";
    fields.code.value = skill?.code ?? "";
    fields.name.value = skill?.name ?? "";
    fields.category.value = skill?.category ?? "";
    fields.description.value = skill?.description ?? "";
    fields.code.readOnly = Boolean(skill);
    setText(document.querySelector("#skillDrawerTitle"), skill ? skill.name || skill.code : "创建 Skill");
    setText(document.querySelector("#skillDrawerEyebrow"), skill ? `DRAFT REV ${skill.draftRevision}` : "NEW SKILL");
    setText(metadataButton, skill ? "保存信息" : "创建 Skill");
    fileWorkspace.hidden = !skill;
    document.querySelector("#skillVersionSection").hidden = !skill;
    fileButton.hidden = !skill;
    deleteFileButton.hidden = !skill;
    publishButton.hidden = !skill;
    archiveButton.hidden = !skill;
    if (skill) setText(publishButton, `发布 v${nextVersion()}`);
    if (skill) setText(archiveButton, archived ? "恢复" : "归档");
    Object.values(fields).forEach(field => { field.disabled = archived; });
    metadataButton.disabled = archived;
    fileButton.disabled = archived;
    publishButton.disabled = archived;
    newFilePath.disabled = archived;
    newFileButton.disabled = archived;
    syncDeleteFileButton();
    renderVersions(skill?.publishedVersions ?? []);
    metadataDirty = false;
  }

  function renderVersions(versions) {
    const list = document.querySelector("#skillVersionList");
    clear(list);
    if (!versions.length) {
      list.append(element("li", { className: "version-empty" }, "尚未发布版本。"));
      return;
    }
    [...versions].reverse().forEach(version => {
      const title = element("strong");
      title.textContent = `v${version.label}`;
      const hash = element("code");
      hash.textContent = version.manifestSha256.slice(0, 12);
      const detail = element("p");
      const bound = version.boundAgents?.length
        ? ` · 绑定 ${version.boundAgents.map(agent => agent.name || agent.code).join("、")}`
        : " · 尚未绑定 Agent";
      detail.textContent = `${version.files.length} 个文件 · ${new Date(version.publishedAtUtc).toLocaleString()}${bound}`;
      list.append(element("li", {}, element("div", {}, title, hash), detail));
    });
  }

  async function loadFiles(preferredPath = "") {
    if (!current) return;
    const files = await api.files(current.id);
    clear(fileList);
    setText(document.querySelector("#skillFileCount"), `${files.length} 个`);
    for (const file of files) {
      const button = element("button", {
        className: `skill-file-item ${file.path === currentPath ? "is-active" : ""}`,
        type: "button"
      });
      const label = element("span");
      label.textContent = file.path;
      const size = element("small");
      size.textContent = `${file.size} B`;
      button.append(label, size);
      button.addEventListener("click", () => selectFile(file.path));
      fileList.append(button);
    }
    const target = preferredPath || currentPath || files[0]?.path;
    if (target && files.some(file => file.path === target)) await selectFile(target, true);
  }

  async function selectFile(path, force = false) {
    if (!current || (busy && !force) || (path === currentPath && !force)) return;
    if (fileDirty && !force) {
      showMessage("当前文件有未保存修改，请先保存再切换。", "warning");
      return;
    }
    const wasBusy = busy;
    setBusy(true);
    try {
      const result = await api.readFile(current.id, path);
      currentPath = path;
      currentFilePersisted = true;
      content.value = result.content;
      content.disabled = current.status === "Archived";
      fileDirty = false;
      setText(document.querySelector("#currentSkillFile"), path);
      setText(document.querySelector("#skillFileState"), "已同步");
      syncDeleteFileButton();
      [...fileList.children].forEach(button =>
        button.classList.toggle("is-active", button.querySelector("span")?.textContent === path));
    } catch (error) {
      showMessage(`${error.message} · ${error.errorCode ?? "READ_FAILED"}`, "error");
    } finally {
      setBusy(wasBusy);
    }
  }

  async function open(skill = null) {
    fileDirty = false;
    metadataDirty = false;
    currentPath = "";
    currentFilePersisted = false;
    fill(skill);
    content.value = "";
    content.disabled = !skill || skill.status === "Archived";
    showMessage("");
    drawer.setAttribute("aria-hidden", "false");
    backdrop.hidden = false;
    document.body.classList.add("drawer-open");
    if (skill) {
      try { await loadFiles("SKILL.md"); }
      catch (error) { showMessage(`${error.message} · ${error.errorCode ?? "LIST_FAILED"}`, "error"); }
    }
    requestAnimationFrame(() => (skill?.status === "Archived"
      ? archiveButton
      : fields[skill ? "name" : "code"]).focus());
  }

  function close() {
    if (busy) return;
    if (fileDirty || metadataDirty) {
      showMessage("存在未保存修改，请先保存。", "warning");
      return;
    }
    drawer.setAttribute("aria-hidden", "true");
    backdrop.hidden = true;
    document.body.classList.remove("drawer-open");
  }

  metadataButton.addEventListener("click", async () => {
    if (busy || !fields.code.reportValidity() || !fields.name.reportValidity()) return;
    if (fileDirty) {
      showMessage("请先保存当前文件，再保存 Skill 信息。", "warning");
      return;
    }
    setBusy(true);
    try {
      current = current
        ? await api.update(current.id, {
            expectedDraftRevision: current.draftRevision,
            name: fields.name.value.trim(),
            description: fields.description.value,
            category: fields.category.value.trim()
          })
        : await api.create({
            code: fields.code.value.trim(),
            name: fields.name.value.trim(),
            description: fields.description.value,
            category: fields.category.value.trim()
          });
      fill(current);
      await loadFiles("SKILL.md");
      showMessage("Skill 信息已保存。", "success");
      await onChanged();
    } catch (error) {
      showMessage(
        error.status === 409
          ? "Draft 已被其他编辑器更新，请关闭后重新打开。"
          : `${error.message} · ${error.errorCode ?? "SAVE_FAILED"}`,
        error.status === 409 ? "warning" : "error");
    } finally {
      setBusy(false);
    }
  });

  fileButton.addEventListener("click", async () => {
    if (!current || !currentPath || busy) return;
    if (metadataDirty) {
      showMessage("请先保存 Skill 信息，再保存文件。", "warning");
      return;
    }
    setBusy(true);
    try {
      current = await api.saveFile(current.id, {
        expectedDraftRevision: current.draftRevision,
        path: currentPath,
        content: content.value
      });
      fileDirty = false;
      currentFilePersisted = true;
      fill(current);
      setText(document.querySelector("#skillFileState"), "已同步");
      showMessage("文件已原子保存。", "success");
      await loadFiles(currentPath);
      await onChanged();
    } catch (error) {
      showMessage(
        error.status === 409
          ? "文件未覆盖：Draft revision 已变化，请保留正文并重新打开。"
          : `${error.message} · ${error.errorCode ?? "SAVE_FAILED"}`,
        error.status === 409 ? "warning" : "error");
    } finally {
      setBusy(false);
    }
  });

  async function deleteFile(path) {
    if (!current || busy || current.status === "Archived") return;
    if (fileDirty || metadataDirty) {
      showMessage("存在未保存修改，请先保存后再删除文件。", "warning");
      return;
    }
    if (!window.confirm(`确定删除 Draft 文件“${path}”吗？`)) return;

    setBusy(true);
    try {
      current = await api.deleteFile(current.id, {
        expectedDraftRevision: current.draftRevision,
        path
      });
      const deletedCurrentFile = currentPath === path;
      if (deletedCurrentFile) {
        currentPath = "";
        currentFilePersisted = false;
        content.value = "";
        setText(document.querySelector("#currentSkillFile"), "未选择文件");
        setText(document.querySelector("#skillFileState"), "已同步");
      }
      fill(current);
      await loadFiles(deletedCurrentFile ? "SKILL.md" : currentPath);
      showMessage("Draft 文件已删除。", "success");
      await onChanged();
    } catch (error) {
      showMessage(
        error.status === 409
          ? "文件未删除：Draft revision 已变化，请关闭后重新打开。"
          : `${error.message} · ${error.errorCode ?? "DELETE_FAILED"}`,
        error.status === 409 ? "warning" : "error");
    } finally {
      setBusy(false);
    }
  }

  deleteFileButton.addEventListener("click", async () => {
    if (!current || !currentPath || busy || current.status === "Archived") return;
    if (!currentFilePersisted) {
      currentPath = "";
      currentFilePersisted = false;
      fileDirty = false;
      content.value = "";
      syncDeleteFileButton();
      await loadFiles("SKILL.md");
      showMessage("已取消新建 Draft 文件。", "success");
      return;
    }
    await deleteFile(currentPath);
  });

  publishButton.addEventListener("click", async () => {
    if (!current || busy) return;
    if (fileDirty || metadataDirty) {
      showMessage("请先保存所有修改再发布。", "warning");
      return;
    }
    setBusy(true);
    try {
      current = await api.publish(current.id, {
        expectedDraftRevision: current.draftRevision,
        versionLabel: nextVersion()
      });
      fill(current);
      showMessage("Skill 已发布为不可变版本。", "success");
      toast("Skill 发布成功，现可绑定到 Agent。", "success");
      await onChanged();
    } catch (error) {
      showMessage(`${error.message} · ${error.errorCode ?? "PUBLISH_FAILED"}`, "error");
    } finally {
      setBusy(false);
    }
  });

  archiveButton.addEventListener("click", async () => {
    if (!current || busy) return;
    if (fileDirty || metadataDirty) {
      showMessage("存在未保存修改，请先保存后再变更归档状态。", "warning");
      return;
    }
    const restoring = current.status === "Archived";
    setBusy(true);
    showMessage(restoring ? "正在恢复 Skill…" : "正在归档 Skill…");
    try {
      current = await api.setArchived(current.id, {
        expectedDraftRevision: current.draftRevision,
        archived: !restoring
      });
      fill(current);
      await loadFiles(currentPath || "SKILL.md");
      await onChanged();
      showMessage(restoring ? "Skill 已恢复。" : "Skill 已归档。", "success");
    } catch (error) {
      showMessage(formatArchiveError(error), "error");
    } finally {
      setBusy(false);
    }
  });

  newFileButton.addEventListener("click", async () => {
    if (!current || busy || current.status === "Archived") return;
    if (fileDirty) {
      showMessage("请先保存当前文件。", "warning");
      return;
    }
    const input = newFilePath;
    const path = input.value.trim();
    if (!path) return;
    currentPath = path;
    currentFilePersisted = false;
    content.value = "";
    content.disabled = false;
    fileDirty = true;
    setText(document.querySelector("#currentSkillFile"), path);
    setText(document.querySelector("#skillFileState"), "新文件未保存");
    syncDeleteFileButton();
    input.value = "";
  });

  content.addEventListener("input", () => {
    fileDirty = true;
    setText(document.querySelector("#skillFileState"), "有未保存修改");
  });
  Object.values(fields).forEach(field => {
    field.addEventListener("input", () => { metadataDirty = true; });
  });
  document.querySelector("#closeSkillDrawerButton").addEventListener("click", close);
  backdrop.addEventListener("click", close);
  document.addEventListener("keydown", event => {
    if (drawer.getAttribute("aria-hidden") !== "false") return;
    if (event.key === "Escape") {
      close();
      return;
    }
    if (event.key !== "Tab") return;
    const focusable = [...drawer.querySelectorAll(
      "button:not([disabled]), input:not([disabled]), textarea:not([disabled])")]
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
  });

  return { open, close };
}
