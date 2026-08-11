import { clear, element, setText } from "./dom.js";

export function createKnowledgePage({ api, toast, onReferencesChanged }) {
  const rows = document.querySelector("#knowledgeRows");
  const status = document.querySelector("#knowledgeListStatus");
  const table = document.querySelector("#knowledgeTableWrap");
  const empty = document.querySelector("#knowledgeEmptyState");
  const workbench = document.querySelector("#knowledgeWorkbench");
  const message = document.querySelector("#knowledgeMessage");
  const contentBrowser = document.querySelector("#knowledgeContentBrowser");
  const documentsStatus = document.querySelector("#knowledgeDocumentsStatus");
  const documentList = document.querySelector("#knowledgeDocumentList");
  const chunkList = document.querySelector("#knowledgeChunkList");
  const chunkPager = document.querySelector("#knowledgeChunkPager");
  const previousChunksButton = document.querySelector("#previousKnowledgeChunksButton");
  const nextChunksButton = document.querySelector("#nextKnowledgeChunksButton");
  const saveButton = document.querySelector("#saveKnowledgeButton");
  const importButton = document.querySelector("#importKnowledgeButton");
  const statusButton = document.querySelector("#statusKnowledgeButton");
  const archiveButton = document.querySelector("#archiveKnowledgeButton");
  const statusFilter = document.querySelector("#knowledgeStatusFilter");
  let values = [];
  let current = null;
  let dirty = false;
  let documents = [];
  let selectedDocumentId = null;
  let currentChunkPage = null;
  let contentRequestSequence = 0;
  const chunkPageSize = 10;

  async function load() {
    status.hidden = false;
    setText(status, "正在加载知识库…");
    try {
      values = await api.knowledgeBases(statusFilter.value);
      render();
    } catch (error) {
      setText(status, error.message);
    }
  }

  function updateWorkbenchMeta() {
    setText(document.querySelector("#knowledgeWorkbenchMeta"),
      `${current.documentCount} 个文档 · ${current.chunkCount} 个分块 · REV ${current.logicalRevision}`);
  }

  function setArchivedState() {
    const archived = current?.status === "Archived";
    for (const id of ["knowledgeNameInput", "knowledgeDescriptionInput", "knowledgeStatusInput", "knowledgeFileInput", "knowledgeQueryInput"])
      document.querySelector(`#${id}`).disabled = archived;
    saveButton.disabled = archived;
    importButton.disabled = archived;
    statusButton.hidden = !current || archived;
    archiveButton.hidden = !current;
    document.querySelector("#searchKnowledgeButton").disabled = archived;
    if (current) setText(statusButton, current.status === "Enabled" ? "停用" : "启用");
    if (current) setText(archiveButton, archived ? "恢复" : "归档");
  }

  function formatArchiveError(error) {
    if (error.errorCode === "KNOWLEDGE_BASE_ARCHIVE_BLOCKED") {
      const marker = "Agent(s): ";
      const backendMessage = error.message || "";
      const markerIndex = backendMessage.indexOf(marker);
      const references = markerIndex >= 0
        ? `Agent“${backendMessage.slice(markerIndex + marker.length).replace(/\.$/, "").replace(/, /g, "”、Agent“")}”`
        : "已启用 Agent";
      return `暂时无法归档：${references}仍在使用该知识库。请先解除知识库绑定或停用引用方，再重新归档。· 错误码：${error.errorCode}`;
    }
    return `${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`;
  }

  function formatImportedAt(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "时间未知" : date.toLocaleString("zh-CN", { hour12: false });
  }

  function resetContentBrowser() {
    contentRequestSequence += 1;
    documents = [];
    selectedDocumentId = null;
    currentChunkPage = null;
    contentBrowser.hidden = true;
    clear(documentList);
    clear(chunkList);
    chunkList.append(element("p", { className: "binding-empty" }, "尚未选择文档。"));
    setText(document.querySelector("#knowledgeDocumentsMeta"), "选择文档查看索引分块。");
    setText(document.querySelector("#knowledgeChunksTitle"), "选择一个文档");
    setText(document.querySelector("#knowledgeChunksMeta"), "分块正文将按页加载。");
    setText(document.querySelector("#knowledgeChunkRange"), "");
    chunkPager.hidden = true;
  }

  function renderDocuments() {
    clear(documentList);
    setText(document.querySelector("#knowledgeDocumentsMeta"),
      `${documents.length} 个文档 · 选择后按页读取分块`);
    if (!documents.length) {
      documentsStatus.hidden = false;
      setText(documentsStatus, "尚未导入文档。");
      return;
    }

    documentsStatus.hidden = true;
    for (const documentValue of documents) {
      const button = element("button", {
        className: `knowledge-document-item${documentValue.id === selectedDocumentId ? " is-active" : ""}`,
        type: "button",
        ariaPressed: documentValue.id === selectedDocumentId,
        dataset: { documentId: documentValue.id }
      },
      element("span", { className: "knowledge-document-name" }, documentValue.fileName),
      element("span", { className: "knowledge-document-facts" },
        `${documentValue.chunkCount} 个分块 · ${documentValue.characterCount.toLocaleString("zh-CN")} 字符`),
      element("span", { className: "knowledge-document-facts" },
        `${formatImportedAt(documentValue.importedAtUtc)} · SHA ${documentValue.sha256.slice(0, 10)}`));
      button.addEventListener("click", () => selectDocument(documentValue.id));
      documentList.append(button);
    }
  }

  async function loadDocuments(preferredDocumentId = null) {
    if (!current) {
      resetContentBrowser();
      return;
    }

    const requestSequence = ++contentRequestSequence;
    contentBrowser.hidden = false;
    documentsStatus.hidden = false;
    setText(documentsStatus, "正在读取文档…");
    try {
      const loaded = await api.knowledgeDocuments(current.id);
      if (requestSequence !== contentRequestSequence) return;
      documents = loaded;
      const availableIds = new Set(documents.map(item => item.id));
      selectedDocumentId = preferredDocumentId && availableIds.has(preferredDocumentId)
        ? preferredDocumentId
        : selectedDocumentId && availableIds.has(selectedDocumentId)
          ? selectedDocumentId
          : documents[0]?.id ?? null;
      renderDocuments();
      if (selectedDocumentId) await loadChunks(selectedDocumentId, 0);
      else resetChunkPanel();
    } catch (error) {
      if (requestSequence !== contentRequestSequence) return;
      setText(documentsStatus, error.message);
      documentsStatus.hidden = false;
    }
  }

  function resetChunkPanel() {
    currentChunkPage = null;
    clear(chunkList);
    chunkList.append(element("p", { className: "binding-empty" }, "尚未选择文档。"));
    setText(document.querySelector("#knowledgeChunksTitle"), "选择一个文档");
    setText(document.querySelector("#knowledgeChunksMeta"), "分块正文将按页加载。");
    setText(document.querySelector("#knowledgeChunkRange"), "");
    chunkPager.hidden = true;
  }

  async function selectDocument(documentId) {
    if (selectedDocumentId === documentId && currentChunkPage) return;
    selectedDocumentId = documentId;
    renderDocuments();
    await loadChunks(documentId, 0);
  }

  async function loadChunks(documentId, skip) {
    if (!current || !documentId) return;
    const requestSequence = ++contentRequestSequence;
    currentChunkPage = null;
    const selected = documents.find(item => item.id === documentId);
    setText(document.querySelector("#knowledgeChunksTitle"), selected?.fileName ?? "文档分块");
    setText(document.querySelector("#knowledgeChunksMeta"), "正在读取分块…");
    setText(document.querySelector("#knowledgeChunkRange"), "");
    clear(chunkList);
    chunkList.append(element("p", { className: "binding-empty" }, "正在读取分块…"));
    chunkPager.hidden = true;
    try {
      const page = await api.knowledgeDocumentChunks(current.id, documentId, skip, chunkPageSize);
      if (requestSequence !== contentRequestSequence || selectedDocumentId !== documentId) return;
      currentChunkPage = page;
      renderChunks();
    } catch (error) {
      if (requestSequence !== contentRequestSequence) return;
      currentChunkPage = null;
      clear(chunkList);
      chunkList.append(element("p", { className: "binding-empty" }, error.message));
      setText(document.querySelector("#knowledgeChunksMeta"), "分块读取失败");
    }
  }

  function renderChunks() {
    const page = currentChunkPage;
    clear(chunkList);
    setText(document.querySelector("#knowledgeChunksTitle"), page.fileName);
    setText(document.querySelector("#knowledgeChunksMeta"), `${page.totalCount} 个索引分块`);
    if (!page.items.length) {
      chunkList.append(element("p", { className: "binding-empty" }, "该文档没有可显示的分块。"));
      setText(document.querySelector("#knowledgeChunkRange"), "0 / 0");
      chunkPager.hidden = true;
      return;
    }

    for (const chunk of page.items) {
      chunkList.append(element("article", { className: "knowledge-chunk-item" },
        element("div", { className: "knowledge-chunk-item-heading" },
          element("strong", {}, `#${chunk.sequence}`),
          element("small", {}, `${chunk.characterCount.toLocaleString("zh-CN")} 字符`)),
        element("pre", { className: "knowledge-chunk-content" }, chunk.content)));
    }
    const first = page.skip + 1;
    const last = Math.min(page.skip + page.items.length, page.totalCount);
    setText(document.querySelector("#knowledgeChunkRange"), `${first}–${last} / ${page.totalCount}`);
    previousChunksButton.disabled = page.skip === 0;
    nextChunksButton.disabled = page.skip + page.items.length >= page.totalCount;
    chunkPager.hidden = page.totalCount <= page.take;
  }

  function render() {
    clear(rows);
    setText(document.querySelector("#knowledgeCount"), `共 ${values.length} 个知识库`);
    status.hidden = true;
    table.hidden = values.length === 0;
    empty.hidden = values.length !== 0;
    for (const value of values) {
      const manage = element("button", { className: "row-action", type: "button" }, "管理");
      manage.addEventListener("click", () => open(value.id));
      rows.append(element("tr", {},
        element("td", {}, element("div", { className: "agent-identity" },
          element("span", { className: "agent-avatar", ariaHidden: "true" }, "K"),
          element("div", {}, element("strong", {}, value.name || value.code), element("code", {}, value.code)))),
        element("td", {}, value.description || "尚未填写说明"),
        element("td", {}, String(value.documentCount)),
        element("td", {}, String(value.chunkCount)),
        element("td", {}, element("span", { className: `badge ${value.status === "Enabled" ? "enabled" : "disabled"}` }, value.status)),
        element("td", {}, manage)));
    }
  }

  async function open(id) {
    current = await api.knowledgeBase(id);
    workbench.hidden = false;
    setText(document.querySelector("#knowledgeWorkbenchTitle"), current.name || current.code);
    updateWorkbenchMeta();
    document.querySelector("#knowledgeNameInput").value = current.name;
    document.querySelector("#knowledgeCodeInput").value = current.code;
    document.querySelector("#knowledgeCodeInput").readOnly = true;
    document.querySelector("#knowledgeDescriptionInput").value = current.description;
    document.querySelector("#knowledgeStatusInput").value = current.status;
    dirty = false;
    setArchivedState();
    setText(message, "");
    await loadDocuments();
    workbench.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function create() {
    current = null;
    workbench.hidden = false;
    setText(document.querySelector("#knowledgeWorkbenchTitle"), "创建知识库");
    setText(document.querySelector("#knowledgeWorkbenchMeta"), "先保存元数据，再导入文本并建立索引。");
    document.querySelector("#knowledgeCodeInput").value = "";
    document.querySelector("#knowledgeCodeInput").readOnly = false;
    document.querySelector("#knowledgeNameInput").value = "";
    document.querySelector("#knowledgeDescriptionInput").value = "";
    document.querySelector("#knowledgeStatusInput").value = "Enabled";
    document.querySelector("#knowledgeFileInput").value = "";
    dirty = false;
    setArchivedState();
    resetContentBrowser();
    document.querySelector("#knowledgeCodeInput").focus();
    workbench.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  async function save() {
    const code = document.querySelector("#knowledgeCodeInput").value.trim();
    try {
      if (current) {
        current = await api.updateKnowledgeBase(current.id, {
          expectedLogicalRevision: current.logicalRevision,
          name: document.querySelector("#knowledgeNameInput").value.trim(),
          description: document.querySelector("#knowledgeDescriptionInput").value,
          status: document.querySelector("#knowledgeStatusInput").value
        });
      } else {
        if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(code)) {
          setText(message, "Code 必须使用小写 kebab-case。");
          message.dataset.tone = "error";
          return;
        }
        current = await api.createKnowledgeBase({
          code,
          name: document.querySelector("#knowledgeNameInput").value.trim(),
          description: document.querySelector("#knowledgeDescriptionInput").value
        });
        document.querySelector("#knowledgeCodeInput").readOnly = true;
      }
      setText(message, "知识库元数据已保存。");
      message.dataset.tone = "success";
      updateWorkbenchMeta();
      dirty = false;
      setArchivedState();
      await loadDocuments();
      await load();
      await onReferencesChanged();
    } catch (error) {
      setText(message, `${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`);
      message.dataset.tone = "error";
    }
  }

  async function importDocument() {
    const file = document.querySelector("#knowledgeFileInput").files?.[0];
    if (!current || !file) {
      setText(message, "请选择 .txt、.md 或 .pdf 文件。");
      return;
    }
    const isPdf = /\.pdf$/i.test(file.name);
    const maximumBytes = isPdf ? 10_485_760 : 2_000_000;
    if (file.size > maximumBytes || !(/\.(md|txt|pdf)$/i.test(file.name))) {
      setText(message, "仅允许不超过 2 MB 的 .txt/.md，或不超过 10 MiB 的 .pdf 文件。");
      message.dataset.tone = "error";
      return;
    }
    try {
      current = isPdf
        ? await api.importKnowledgePdf(current.id, current.logicalRevision, file)
        : await api.importKnowledgeDocument(current.id, {
            expectedLogicalRevision: current.logicalRevision,
            fileName: file.name,
            mediaType: /\.md$/i.test(file.name) ? "text/markdown" : "text/plain",
            content: await file.text()
          });
      setText(message, `已导入 ${file.name}，当前共 ${current.chunkCount} 个分块。`);
      message.dataset.tone = "success";
      updateWorkbenchMeta();
      selectedDocumentId = null;
      await loadDocuments();
      await load();
      await onReferencesChanged();
    } catch (error) {
      setText(message, `${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`);
      message.dataset.tone = "error";
    }
  }

  async function search() {
    const query = document.querySelector("#knowledgeQueryInput").value.trim();
    if (!current || !query) return;
    const container = document.querySelector("#knowledgeSearchResults");
    try {
      const results = await api.searchKnowledge(current.id, query);
      clear(container);
      if (!results.length) {
        container.append(element("p", { className: "binding-empty" }, "没有召回相关分块。"));
        return;
      }
      for (const result of results) {
        container.append(element("div", { className: "run-tool-event tool-succeeded" },
          element("div", { className: "run-tool-heading" },
            element("strong", {}, `${result.fileName} #${result.chunkSequence}`),
            element("small", {}, `score ${result.score.toFixed(3)}`)),
          element("pre", { className: "run-tool-result" }, result.content)));
      }
    } catch (error) {
      toast(error.message, "error");
    }
  }

  async function setStatus() {
    if (!current || current.status === "Archived") return;
    const target = current.status === "Enabled" ? "Disabled" : "Enabled";
    try {
      current = await api.updateKnowledgeBase(current.id, {
        expectedLogicalRevision: current.logicalRevision,
        name: document.querySelector("#knowledgeNameInput").value.trim(),
        description: document.querySelector("#knowledgeDescriptionInput").value,
        status: target
      });
      document.querySelector("#knowledgeStatusInput").value = current.status;
      dirty = false;
      updateWorkbenchMeta();
      setArchivedState();
      setText(message, target === "Enabled"
        ? "配置已保存，知识库已启用。"
        : "配置已保存，知识库已停用；现在可以归档。");
      message.dataset.tone = "success";
      await load();
      await onReferencesChanged();
    } catch (error) {
      setText(message, `${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`);
      message.dataset.tone = "error";
    }
  }

  async function setArchived() {
    if (!current) return;
    if (dirty) {
      setText(message, "存在未保存修改，请先保存后再变更归档状态。");
      message.dataset.tone = "warning";
      return;
    }
    const restoring = current.status === "Archived";
    if (!restoring && current.status !== "Disabled") {
      setText(message, "请先点击“停用”，再归档知识库。");
      message.dataset.tone = "warning";
      return;
    }
    try {
      current = await api.setKnowledgeBaseArchived(current.id, {
        expectedLogicalRevision: current.logicalRevision,
        archived: !restoring
      });
      document.querySelector("#knowledgeStatusInput").value = current.status;
      updateWorkbenchMeta();
      setArchivedState();
      setText(message, restoring ? "知识库已恢复为停用状态。" : "知识库已归档。");
      message.dataset.tone = "success";
      await load();
      await onReferencesChanged();
    } catch (error) {
      setText(message, formatArchiveError(error));
      message.dataset.tone = "error";
    }
  }

  document.querySelector("#createKnowledgeButton").addEventListener("click", create);
  saveButton.addEventListener("click", save);
  importButton.addEventListener("click", importDocument);
  statusButton.addEventListener("click", setStatus);
  archiveButton.addEventListener("click", setArchived);
  document.querySelector("#searchKnowledgeButton").addEventListener("click", search);
  statusFilter.addEventListener("change", load);
  for (const id of ["knowledgeNameInput", "knowledgeDescriptionInput", "knowledgeStatusInput"])
    document.querySelector(`#${id}`).addEventListener("input", () => { dirty = true; });
  previousChunksButton.addEventListener("click", () => {
    if (!currentChunkPage) return;
    loadChunks(currentChunkPage.documentId, Math.max(0, currentChunkPage.skip - currentChunkPage.take));
  });
  nextChunksButton.addEventListener("click", () => {
    if (!currentChunkPage) return;
    loadChunks(currentChunkPage.documentId, currentChunkPage.skip + currentChunkPage.take);
  });
  return { load };
}
