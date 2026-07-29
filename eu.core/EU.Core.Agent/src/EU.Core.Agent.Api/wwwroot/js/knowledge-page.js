import { clear, element, setText } from "./dom.js";

export function createKnowledgePage({ api, toast, onReferencesChanged }) {
  const rows = document.querySelector("#knowledgeRows");
  const status = document.querySelector("#knowledgeListStatus");
  const table = document.querySelector("#knowledgeTableWrap");
  const empty = document.querySelector("#knowledgeEmptyState");
  const workbench = document.querySelector("#knowledgeWorkbench");
  const message = document.querySelector("#knowledgeMessage");
  let values = [];
  let current = null;

  async function load() {
    status.hidden = false;
    setText(status, "正在加载知识库…");
    try {
      values = await api.knowledgeBases();
      render();
    } catch (error) {
      setText(status, error.message);
    }
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
    setText(document.querySelector("#knowledgeWorkbenchMeta"),
      `${current.documents.length} 个文档 · ${current.chunks.length} 个分块 · REV ${current.logicalRevision}`);
    document.querySelector("#knowledgeNameInput").value = current.name;
    document.querySelector("#knowledgeCodeInput").value = current.code;
    document.querySelector("#knowledgeCodeInput").readOnly = true;
    document.querySelector("#knowledgeDescriptionInput").value = current.description;
    document.querySelector("#knowledgeStatusInput").value = current.status;
    setText(message, "");
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
      setText(message, "请选择 .txt 或 .md 文件。");
      return;
    }
    if (file.size > 2_000_000 || !(/\.(md|txt)$/i.test(file.name))) {
      setText(message, "仅允许不超过 2 MB 的 .txt 或 .md 文件。");
      message.dataset.tone = "error";
      return;
    }
    try {
      current = await api.importKnowledgeDocument(current.id, {
        expectedLogicalRevision: current.logicalRevision,
        fileName: file.name,
        mediaType: /\.md$/i.test(file.name) ? "text/markdown" : "text/plain",
        content: await file.text()
      });
      setText(message, `已导入 ${file.name}，生成 ${current.chunks.length} 个累计分块。`);
      message.dataset.tone = "success";
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

  document.querySelector("#createKnowledgeButton").addEventListener("click", create);
  document.querySelector("#saveKnowledgeButton").addEventListener("click", save);
  document.querySelector("#importKnowledgeButton").addEventListener("click", importDocument);
  document.querySelector("#searchKnowledgeButton").addEventListener("click", search);
  return { load };
}
