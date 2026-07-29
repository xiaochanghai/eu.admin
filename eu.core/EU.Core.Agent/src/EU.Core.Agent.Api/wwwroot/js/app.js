import { agentApi } from "./api-client.js";
import { createAgentEditor } from "./agent-editor.js";
import { clear, element, setText } from "./dom.js";
import { createSkillsPage } from "./skills-page.js";
import { createMcpPage } from "./mcp-page.js";
import { createAgentRunner } from "./agent-runner.js";
import { createKnowledgePage } from "./knowledge-page.js";
import { createOrchestrationPage } from "./orchestration-page.js";

const state = { agents: [], search: "", status: "", capabilities: null };
const rows = document.querySelector("#agentRows");
const tableWrap = document.querySelector("#tableWrap");
const emptyState = document.querySelector("#emptyState");
const listStatus = document.querySelector("#listStatus");
const count = document.querySelector("#agentCount");
const searchInput = document.querySelector("#searchInput");
const statusFilter = document.querySelector("#statusFilter");
let searchTimer;

let editor;
const runner = createAgentRunner({ api: agentApi, toast });
editor = createAgentEditor({
  onCreate: async body => {
    const result = await agentApi.create(body);
    await loadAgents();
    return result;
  },
  onReload: id => agentApi.get(id),
  onSave: async (id, body) => {
    const result = await agentApi.saveDraft(id, body);
    await loadAgents();
    return result;
  },
  onPublish: async (id, revision) => {
    const result = await agentApi.publish(id, revision);
    await loadAgents();
    return result;
  },
  onStatus: async (id, status, revision) => {
    const result = await agentApi.setStatus(id, status, revision);
    await loadAgents();
    return result;
  },
  onExport: downloadPackage,
  onRun: agent => {
    editor.close();
    runner.open(agent);
  }
});

async function refreshPublishedSkills() {
  try {
    editor.setPublishedSkills(await agentApi.publishedSkills());
  } catch (error) {
    toast(`Skill 版本读取失败：${error.message}`, "error");
  }
}

async function refreshPublishedTools() {
  try {
    editor.setPublishedTools(await agentApi.publishedTools());
  } catch (error) {
    toast(`MCP 工具版本读取失败：${error.message}`, "error");
  }
}

async function refreshKnowledgeReferences() {
  try {
    editor.setKnowledgeBases(await agentApi.knowledgeReferences());
  } catch (error) {
    toast(`知识库引用读取失败：${error.message}`, "error");
  }
}

const skillsPage = createSkillsPage({
  toast,
  onPublishedChanged: refreshPublishedSkills
});

const mcpPage = createMcpPage({
  toast,
  onToolsChanged: refreshPublishedTools
});

const knowledgePage = createKnowledgePage({
  api: agentApi,
  toast,
  onReferencesChanged: refreshKnowledgeReferences
});
const orchestrationPage = createOrchestrationPage({ api: agentApi, toast });

function statusBadge(status) {
  const badge = element("span", { className: `badge ${status === "Enabled" ? "enabled" : "disabled"}` });
  badge.textContent = status === "Enabled" ? "已启用" : "已停用";
  return badge;
}

function renderAgents() {
  clear(rows);
  setText(count, `共 ${state.agents.length} 个 Agent`);
  listStatus.hidden = true;
  tableWrap.hidden = state.agents.length === 0;
  emptyState.hidden = state.agents.length !== 0;
  for (const agent of state.agents) {
    const name = element("strong");
    name.textContent = agent.name || agent.code;
    const code = element("code");
    code.textContent = agent.code;
    const description = element("p", { className: "row-description" });
    description.textContent = agent.description || "尚未填写职责说明";
    const model = element("span", { className: "muted" });
    model.textContent = agent.draftModelProfileId || "未选择";
    const version = element("span");
    version.textContent = agent.currentPublishedLabel ? `v${agent.currentPublishedLabel}` : "仅 Draft";
    const openButton = element("button", { className: "row-action", type: "button", ariaLabel: `编辑 ${agent.name || agent.code}` });
    openButton.textContent = "管理";
    openButton.addEventListener("click", () => openAgent(agent.id, openButton));
    const row = element("tr", {},
      element("td", {}, element("div", { className: "agent-identity" }, element("span", { className: "agent-avatar", ariaHidden: "true" }, (agent.name || agent.code).slice(0, 1).toUpperCase()), element("div", {}, name, code))),
      element("td", {}, description),
      element("td", {}, model),
      element("td", {}, version),
      element("td", {}, statusBadge(agent.runtimeStatus)),
      element("td", {}, openButton)
    );
    rows.append(row);
  }
}

async function loadAgents() {
  listStatus.hidden = false;
  setText(listStatus, "正在加载 Agent…");
  tableWrap.hidden = true;
  emptyState.hidden = true;
  try {
    state.agents = await agentApi.list({ search: state.search, status: state.status });
    renderAgents();
  } catch (error) {
    clear(listStatus);
    const text = element("span");
    text.textContent = `${error.message}。`;
    const retry = element("button", { className: "row-action", type: "button" });
    retry.textContent = "重试";
    retry.addEventListener("click", loadAgents);
    listStatus.append(text, retry);
    listStatus.hidden = false;
    setText(count, "读取失败");
  }
}

async function openAgent(id, trigger) {
  trigger.disabled = true;
  try {
    const agent = await agentApi.get(id);
    editor.open(agent);
  } catch (error) {
    toast(error.message, "error");
  } finally {
    trigger.disabled = false;
  }
}

async function downloadPackage(agent) {
  const blob = await agentApi.exportPackage(agent.id);
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${agent.code}.agent.json`;
  link.click();
  URL.revokeObjectURL(url);
}

function toast(text, tone = "") {
  const region = document.querySelector("#toastRegion");
  const item = element("div", { className: `toast ${tone}` });
  item.textContent = text;
  region.append(item);
  window.setTimeout(() => item.remove(), 4200);
}

async function importFile(file) {
  if (!file) return;
  if (file.size > 131072) {
    toast("配置包超过 128 KiB 上限。", "error");
    return;
  }
  try {
    await agentApi.importPackage(await file.text());
    toast("配置包已导入。", "success");
    await loadAgents();
  } catch (error) {
    toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
  }
}

document.querySelector("#createButton").addEventListener("click", () => editor.open());
document.querySelector("#emptyCreateButton").addEventListener("click", () => editor.open());
document.querySelector("#importButton").addEventListener("click", () => document.querySelector("#importFile").click());
document.querySelector("#importFile").addEventListener("change", event => {
  importFile(event.target.files?.[0]);
  event.target.value = "";
});
searchInput.addEventListener("input", () => {
  window.clearTimeout(searchTimer);
  searchTimer = window.setTimeout(() => {
    state.search = searchInput.value.trim();
    loadAgents();
  }, 220);
});
statusFilter.addEventListener("change", () => {
  state.status = statusFilter.value;
  loadAgents();
});

function showPage(page) {
  const skills = page === "skills";
  const mcp = page === "mcp";
  const knowledge = page === "knowledge";
  const orchestration = page === "orchestration";
  document.querySelectorAll(".agent-page").forEach(node => { node.hidden = skills || mcp || knowledge || orchestration; });
  document.querySelector("#skillPage").hidden = !skills;
  document.querySelector("#mcpPage").hidden = !mcp;
  document.querySelector("#knowledgePage").hidden = !knowledge;
  document.querySelector("#orchestrationPage").hidden = !orchestration;
  document.querySelector("#agentNavButton").classList.toggle("is-active", !skills && !mcp && !knowledge && !orchestration);
  document.querySelector("#skillNavButton").classList.toggle("is-active", skills);
  document.querySelector("#mcpNavButton").classList.toggle("is-active", mcp);
  document.querySelector("#knowledgeNavButton").classList.toggle("is-active", knowledge);
  document.querySelector("#orchestrationNavButton").classList.toggle("is-active", orchestration);
  document.querySelector("#agentNavButton").toggleAttribute("aria-current", !skills && !mcp && !knowledge && !orchestration);
  document.querySelector("#skillNavButton").toggleAttribute("aria-current", skills);
  document.querySelector("#mcpNavButton").toggleAttribute("aria-current", mcp);
  document.querySelector("#knowledgeNavButton").toggleAttribute("aria-current", knowledge);
  document.querySelector("#orchestrationNavButton").toggleAttribute("aria-current", orchestration);
  if (skills) skillsPage.load();
  if (mcp) mcpPage.load();
  if (knowledge) knowledgePage.load();
  if (orchestration) orchestrationPage.load();
}

document.querySelector("#agentNavButton").addEventListener("click", () => showPage("agents"));
document.querySelector("#skillNavButton").addEventListener("click", () => showPage("skills"));
document.querySelector("#mcpNavButton").addEventListener("click", () => showPage("mcp"));
document.querySelector("#knowledgeNavButton").addEventListener("click", () => showPage("knowledge"));
document.querySelector("#orchestrationNavButton").addEventListener("click", () => showPage("orchestration"));

async function initialize() {
  try {
    state.capabilities = await agentApi.capabilities();
    editor.setModelProfiles(state.capabilities.modelProfileIds ?? []);
  } catch (error) {
    toast(`能力信息读取失败：${error.message}`, "error");
  }
  await refreshPublishedSkills();
  await refreshPublishedTools();
  await refreshKnowledgeReferences();
  await loadAgents();
}

initialize();
