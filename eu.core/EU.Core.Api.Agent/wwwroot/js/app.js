import { agentApi } from "./api-client.js";
import { createAgentEditor } from "./agent-editor.js";
import { clear, element, setText } from "./dom.js";
import { createSkillsPage } from "./skills-page.js";
import { createMcpPage } from "./mcp-page.js";
import { createAgentRunner } from "./agent-runner.js";
import { createKnowledgePage } from "./knowledge-page.js";
import { createOrchestrationPage } from "./orchestration-page.js";
import { createChatPage } from "./chat-page.js";
import { createApprovalPage } from "./approval-page.js";
import { createEvaluationPage } from "./evaluation-page.js";

const state = {
  agents: [],
  search: "",
  status: "",
  capabilities: null,
  mainAssignment: null,
  mainAgentDefinition: null
};
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
    if (String(state.mainAssignment?.AgentId || "") === String(id)) {
      await refreshMainAssignment();
    }
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
  },
  onSetMain: assignMainAgent
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

async function refreshPublishedOrchestrations() {
  try {
    const references = await agentApi.orchestrations();
    editor.setPublishedOrchestrations(references.filter(reference =>
      reference.runtimeStatus === "Enabled" && Boolean(reference.currentPublishedLabel)));
  } catch (error) {
    toast(`orchestration 读取失败：${error.message}`, "error");
  }
}

async function refreshMainAssignment() {
  try {
    state.mainAssignment = await agentApi.mainAgent();
    state.mainAgentDefinition = state.mainAssignment
      ? await agentApi.get(state.mainAssignment.AgentId)
      : null;
    editor.setMainAssignment(state.mainAssignment);
    chatPage.setMainAgent(state.mainAssignment, state.mainAgentDefinition);
  } catch (error) {
    state.mainAgentDefinition = null;
    chatPage.setMainAgent(state.mainAssignment, null);
    toast(`Main Agent 配置读取失败：${error.message}`, "error");
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
let approvalPage;
const chatPage = createChatPage({
  api: agentApi,
  toast,
  onUpdateMain: assignMainAgent,
  onOpenApproval: id => {
    showPage("approvals", true);
    approvalPage?.open(id);
  }
});
approvalPage = createApprovalPage({
  api: agentApi,
  toast,
  onOpenConversation: id => {
    showPage("chat", true);
    chatPage.openConversation(id);
  }
});
const evaluationPage = createEvaluationPage({
  api: agentApi,
  toast,
  onOpenTrace: async runId => {
    try {
      const run = await agentApi.chatRun(runId);
      showPage("chat", true);
      await chatPage.openConversation(run.conversationId);
    } catch (error) {
      toast(`执行追踪读取失败：${error.message}`, "error");
    }
  }
});

async function assignMainAgent(agent) {
  let assignment;
  try {
    assignment = await agentApi.setMainAgent(
      agent.Id,
      state.mainAssignment?.LogicalRevision ?? null);
  } catch (error) {
    if (error.status !== 409) throw error;
    state.mainAssignment = await agentApi.mainAgent();
    editor.setMainAssignment(state.mainAssignment);
    assignment = await agentApi.setMainAgent(
      agent.Id,
      state.mainAssignment?.LogicalRevision ?? null);
  }
  state.mainAssignment = assignment;
  state.mainAgentDefinition = agent;
  editor.setMainAssignment(assignment);
  chatPage.setMainAgent(assignment, agent);
  return assignment;
}

function statusBadge(status) {
  const badge = element("span", { className: `badge ${status === "Enabled" ? "enabled" : "disabled"}` });
  badge.textContent = status === "Enabled" ? "已启用" : status === "Archived" ? "已归档" : "已停用";
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
    name.textContent = agent.Name || agent.Code;
    const code = element("code");
    code.textContent = agent.Code;
    const description = element("p", { className: "row-description" });
    description.textContent = agent.Description || "尚未填写职责说明";
    const model = element("span", { className: "muted" });
    model.textContent = agent.DraftModelProfileId || "未选择";
    const version = element("span");
    version.textContent = agent.CurrentPublishedLabel ? `v${agent.CurrentPublishedLabel}` : "仅 Draft";
    const openButton = element("button", { className: "row-action", type: "button", ariaLabel: `编辑 ${agent.Name || agent.Code}` });
    openButton.textContent = "管理";
    openButton.addEventListener("click", () => openAgent(agent.Id, openButton));
    const row = element("tr", {},
      element("td", {}, element("div", { className: "agent-identity" }, element("span", { className: "agent-avatar", ariaHidden: "true" }, (agent.Name || agent.Code).slice(0, 1).toUpperCase()), element("div", {}, name, code))),
      element("td", {}, description),
      element("td", {}, model),
      element("td", {}, version),
      element("td", {}, statusBadge(agent.RuntimeStatus)),
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
    const [listedAgents, enabledAgents] = await Promise.all([
      agentApi.list({ search: state.search, status: state.status }),
      agentApi.list({ status: "Enabled" })
    ]);
    state.agents = listedAgents;
    editor.setPublishedAgents(enabledAgents.filter(reference =>
      reference.RuntimeStatus === "Enabled" && Boolean(reference.CurrentPublishedLabel)));
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
  const blob = await agentApi.exportPackage(agent.Id);
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${agent.Code}.agent.json`;
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

function showPage(page, moveFocus = false) {
  const chat = page === "chat";
  const approvals = page === "approvals";
  const agents = page === "agents";
  const skills = page === "skills";
  const mcp = page === "mcp";
  const knowledge = page === "knowledge";
  const orchestration = page === "orchestration";
  const evaluation = page === "evaluation";
  document.querySelector("#chatPage").hidden = !chat;
  document.querySelector("#approvalPage").hidden = !approvals;
  document.querySelectorAll(".agent-page").forEach(node => { node.hidden = !agents; });
  document.querySelector("#skillPage").hidden = !skills;
  document.querySelector("#mcpPage").hidden = !mcp;
  document.querySelector("#knowledgePage").hidden = !knowledge;
  document.querySelector("#orchestrationPage").hidden = !orchestration;
  document.querySelector("#evaluationPage").hidden = !evaluation;
  document.querySelector("#chatNavButton").classList.toggle("is-active", chat);
  document.querySelector("#approvalNavButton").classList.toggle("is-active", approvals);
  document.querySelector("#agentNavButton").classList.toggle("is-active", agents);
  document.querySelector("#skillNavButton").classList.toggle("is-active", skills);
  document.querySelector("#mcpNavButton").classList.toggle("is-active", mcp);
  document.querySelector("#knowledgeNavButton").classList.toggle("is-active", knowledge);
  document.querySelector("#orchestrationNavButton").classList.toggle("is-active", orchestration);
  document.querySelector("#evaluationNavButton").classList.toggle("is-active", evaluation);
  const destinations = [
    ["#chatNavButton", "#chatPage", chat],
    ["#approvalNavButton", "#approvalPage", approvals],
    ["#agentNavButton", "#agentPageHeader", agents],
    ["#skillNavButton", "#skillPage", skills],
    ["#mcpNavButton", "#mcpPage", mcp],
    ["#knowledgeNavButton", "#knowledgePage", knowledge],
    ["#orchestrationNavButton", "#orchestrationPage", orchestration],
    ["#evaluationNavButton", "#evaluationPage", evaluation]
  ];
  for (const [navSelector, pageSelector, active] of destinations) {
    const nav = document.querySelector(navSelector);
    const destination = document.querySelector(pageSelector);
    if (active) nav.setAttribute("aria-current", "page");
    else nav.removeAttribute("aria-current");
    destination?.setAttribute("aria-hidden", String(!active));
  }
  if (chat) chatPage.load();
  if (approvals) approvalPage.load();
  if (agents) {
    refreshPublishedOrchestrations();
    refreshMainAssignment();
  }
  if (skills) skillsPage.load();
  if (mcp) mcpPage.load();
  if (knowledge) knowledgePage.load();
  if (orchestration) orchestrationPage.load();
  if (evaluation) evaluationPage.load();
  if (moveFocus) {
    const destination = destinations.find(([, , active]) => active)?.[1];
    requestAnimationFrame(() =>
      document.querySelector(destination)?.focus({ preventScroll: true }));
  }
}

document.querySelector("#chatNavButton").addEventListener("click", () => showPage("chat", true));
document.querySelector("#approvalNavButton").addEventListener("click", () => showPage("approvals", true));
document.querySelector("#agentNavButton").addEventListener("click", () => showPage("agents", true));
document.querySelector("#skillNavButton").addEventListener("click", () => showPage("skills", true));
document.querySelector("#mcpNavButton").addEventListener("click", () => showPage("mcp", true));
document.querySelector("#knowledgeNavButton").addEventListener("click", () => showPage("knowledge", true));
document.querySelector("#orchestrationNavButton").addEventListener("click", () => showPage("orchestration", true));
document.querySelector("#evaluationNavButton").addEventListener("click", () => showPage("evaluation", true));

function renderStorageMode(capabilities) {
  const storageMode = String(capabilities?.storageMode || "").trim().toLowerCase();
  if (!storageMode) {
    document.querySelector("#storageModeTitle").textContent = "持久化存储";
    document.querySelector("#storageModeSummary").textContent = "模式读取失败";
    document.querySelector("#storageModeBanner").textContent = "持久化模式读取失败。";
    document.querySelector("#storageModeDescription").textContent =
      "请检查 Host 能力接口后重试。";
    document.querySelector("#agentStorageMode").textContent = "读取失败";
    return;
  }

  const isSqlSugar = storageMode === "sqlsugar";
  const displayName = isSqlSugar
    ? "SqlSugar"
    : (storageMode || "统一存储");

  document.querySelector("#storageModeTitle").textContent = isSqlSugar
    ? "统一数据库模式"
    : "持久化存储";
  document.querySelector("#storageModeSummary").textContent =
    `${displayName} · Agent / Skill / MCP`;
  document.querySelector("#storageModeBanner").textContent =
    `当前使用 ${displayName} 持久化。`;
  document.querySelector("#storageModeDescription").textContent = isSqlSugar
    ? "Agent、Skill 与 MCP 数据通过统一数据库访问层持久化。"
    : "Agent、Skill 与 MCP 数据由 Host 统一持久化。";
  document.querySelector("#orchestrationStorageDescription").textContent = isSqlSugar
    ? "每个节点最多重试 3 次；SqlSugar 会持久化编排输入与输出、节点 Attempt、MCP 调用明细和完整执行追踪，敏感字段在写入前脱敏。"
    : "每个节点最多重试 3 次；Host 会持久化编排输入与输出、节点 Attempt、MCP 调用明细和完整执行追踪，敏感字段在写入前脱敏。";
  document.querySelector("#agentStorageMode").textContent = `${displayName}（持久化）`;
}

async function initialize() {
  showPage("chat");
  try {
    state.capabilities = await agentApi.capabilities();
    editor.setModelProfiles(state.capabilities.modelProfileIds ?? []);
    renderStorageMode(state.capabilities);
  } catch (error) {
    renderStorageMode(null);
    toast(`能力信息读取失败：${error.message}`, "error");
  }
  await refreshPublishedSkills();
  await refreshPublishedTools();
  await refreshKnowledgeReferences();
  await refreshPublishedOrchestrations();
  await refreshMainAssignment();
  await loadAgents();
}

initialize();
