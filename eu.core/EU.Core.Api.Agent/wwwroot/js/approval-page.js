import { clear, element, setText } from "./dom.js";

const STATUS_LABELS = Object.freeze({
  Pending: "待审批",
  Approved: "已批准，待恢复",
  Rejected: "已拒绝",
  Cancelled: "已取消",
  Expired: "已过期",
  Consuming: "执行中",
  Consumed: "已执行",
  Failed: "执行失败",
  Invalidated: "已失效"
});

const ACTION_COPY = Object.freeze({
  approve: ["批准此次调用？", "批准后仍会重新校验工具版本、Schema 和申请人权限。", "确认批准"],
  reject: ["拒绝此次调用？", "拒绝后不会调用 MCP，原会话将进入失败终态。", "确认拒绝"],
  cancel: ["取消此次申请？", "只有原申请人可以取消，且不会调用 MCP。", "确认取消"]
});

export function approvalPresentation(value, now = Date.now()) {
  const expiresAt = new Date(value?.ExpiresAtUtc).getTime();
  const remaining = Number.isFinite(expiresAt)
    ? Math.max(0, expiresAt - now)
    : 0;
  const minutes = Math.floor(remaining / 60000);
  const seconds = Math.floor((remaining % 60000) / 1000);
  return {
    status: STATUS_LABELS[value?.Status] || String(value?.Status || "未知"),
    risk: value?.Risk === "HighRisk" ? "高风险" : "变更操作",
    countdown: value?.Status === "Pending"
      ? remaining > 0
        ? `${minutes}:${String(seconds).padStart(2, "0")} 后过期`
        : "已到期，等待服务端确认"
      : "",
    isHighRisk: value?.Risk === "HighRisk"
  };
}

export function shouldRefreshExpiredApproval(value, now = Date.now()) {
  const expiresAt = new Date(value?.ExpiresAtUtc).getTime();
  return ["Pending", "Approved"].includes(value?.Status) &&
    Number.isFinite(expiresAt) &&
    expiresAt <= now;
}

export function createApprovalPage({ api, toast, onOpenConversation }) {
  const page = document.querySelector("#approvalPage");
  const filter = document.querySelector("#approvalStatusFilter");
  const refreshButton = document.querySelector("#refreshApprovalsButton");
  const list = document.querySelector("#approvalQueueList");
  const empty = document.querySelector("#approvalQueueEmpty");
  const count = document.querySelector("#approvalQueueCount");
  const detail = document.querySelector("#approvalDetail");
  const state = {
    initialized: false,
    items: [],
    selectedId: null,
    selected: null,
    revision: 0,
    countdownTimer: null,
    expiryRefreshInFlight: false
  };

  function formatTime(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "—" : new Intl.DateTimeFormat("zh-CN", {
      year: "numeric", month: "2-digit", day: "2-digit",
      hour: "2-digit", minute: "2-digit", second: "2-digit"
    }).format(date);
  }

  function shortId(value) {
    const text = String(value || "");
    return text.length > 16 ? `${text.slice(0, 12)}…` : text;
  }

  function renderList() {
    clear(list);
    empty.hidden = state.items.length !== 0;
    setText(count, `${state.items.length} 条`);
    for (const approval of state.items) {
      const view = approvalPresentation(approval);
      const button = element("button", {
        className: `approval-queue-item${String(approval.Id) === String(state.selectedId) ? " is-active" : ""}`,
        type: "button"
      });
      button.dataset.approvalId = String(approval.Id);
      button.setAttribute("aria-pressed", String(String(approval.Id) === String(state.selectedId)));
      const heading = element("span", { className: "approval-queue-title" });
      const tool = element("strong");
      tool.textContent = approval.ToolName || "未命名工具";
      const risk = element("span", {
        className: `approval-risk ${view.isHighRisk ? "is-high" : ""}`
      });
      risk.textContent = view.risk;
      heading.append(tool, risk);
      const status = element("span", { className: `approval-status is-${String(approval.Status).toLowerCase()}` });
      status.textContent = [view.status, view.countdown].filter(Boolean).join(" · ");
      const meta = element("small");
      meta.textContent = `${approval.RequesterUserId} · ${formatTime(approval.RequestedAtUtc)}`;
      button.append(heading, status, meta);
      button.addEventListener("click", () => select(approval.Id));
      list.append(button);
    }
  }

  function field(label, value, monospace = false) {
    const group = element("div", { className: "approval-field" });
    const name = element("dt");
    name.textContent = label;
    const content = element("dd", { className: monospace ? "is-monospace" : "" });
    content.textContent = String(value ?? "—");
    group.append(name, content);
    return group;
  }

  function safeSummary(value) {
    try { return JSON.stringify(JSON.parse(value || "{}"), null, 2); }
    catch { return "{}"; }
  }

  function renderConfirmation(approval, action, container) {
    clear(container);
    const [title, description, confirmLabel] = ACTION_COPY[action];
    const heading = element("strong");
    heading.textContent = title;
    const copy = element("p");
    copy.textContent = description;
    const reason = element("textarea", {
      rows: "3",
      maxLength: "512",
      placeholder: action === "approve" ? "审批说明（可选）" : "请填写原因（可选）"
    });
    const actions = element("div", { className: "approval-confirm-actions" });
    const back = element("button", { className: "button secondary", type: "button" });
    back.textContent = "返回";
    const confirm = element("button", {
      className: `button ${action === "approve" ? "primary" : "danger-button"}`,
      type: "button"
    });
    confirm.textContent = confirmLabel;
    back.addEventListener("click", renderDetail);
    confirm.addEventListener("click", async () => {
      confirm.disabled = true;
      back.disabled = true;
      try {
        if (action === "approve") await api.approveToolApproval(approval.Id, reason.value.trim());
        if (action === "reject") await api.rejectToolApproval(approval.Id, reason.value.trim());
        if (action === "cancel") await api.cancelToolApproval(approval.Id, reason.value.trim());
        toast(action === "approve"
          ? "审批已批准。原申请人可安全恢复执行。"
          : "审批决定已保存，外部工具未执行。", "success");
        await load(approval.Id);
      } catch (error) {
        confirm.disabled = false;
        back.disabled = false;
        toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
      }
    });
    actions.append(back, confirm);
    container.append(heading, copy, reason, actions);
    reason.focus();
  }

  function renderDetail() {
    clear(detail);
    const record = state.selected?.Approval;
    if (!record) {
      detail.append(element("div", { className: "approval-detail-empty" },
        element("span", { ariaHidden: "true" }, "✓"),
        element("h2", {}, "选择一条审批"),
        element("p", {}, "查看工具版本、风险、参数摘要、申请人和关联运行。")));
      return;
    }
    const view = approvalPresentation(record);
    const header = element("header", { className: "approval-detail-head" });
    const titleWrap = element("div");
    const eyebrow = element("p", { className: "eyebrow" });
    eyebrow.textContent = view.isHighRisk ? "HIGH RISK" : "MUTATING TOOL";
    const title = element("h2");
    title.textContent = record.ToolName || "未命名工具";
    const status = element("span", { className: `approval-status is-${String(record.Status).toLowerCase()}` });
    status.textContent = [view.status, view.countdown].filter(Boolean).join(" · ");
    titleWrap.append(eyebrow, title);
    header.append(titleWrap, status);

    const warning = element("p", {
      className: `approval-warning${view.isHighRisk ? " is-high" : ""}`
    });
    warning.textContent = view.isHighRisk
      ? "高风险调用：申请人不能自批；批准前请核对工具版本、过期时间和参数摘要。"
      : "批准只针对本次冻结调用；任何工具版本、Schema 或权限变化都会使批准失效。";
    const facts = element("dl", { className: "approval-facts" });
    facts.append(
      field("申请人", record.RequesterUserId),
      field("申请时间", formatTime(record.RequestedAtUtc)),
      field("过期时间", formatTime(record.ExpiresAtUtc)),
      field("工具版本", record.ToolVersionId, true),
      field("Agent 版本", record.AgentVersionId, true),
      field("会话", record.ConversationId, true),
      field("运行", record.EntryRunId, true),
      field("参数 Hash", record.ArgumentsSha256, true),
      field("Schema Hash", record.ToolSchemaSha256, true));
    const summary = element("section", { className: "approval-summary" });
    summary.append(element("h3", {}, "安全参数摘要"));
    const pre = element("pre");
    pre.textContent = safeSummary(record.SafeArgumentsSummaryJson);
    summary.append(pre);

    const history = element("section", { className: "approval-history" });
    history.append(element("h3", {}, "决定记录"));
    const decisions = state.selected.Decisions || [];
    if (!decisions.length) history.append(element("p", {}, "尚未作出决定。"));
    for (const decision of decisions) {
      history.append(element("div", { className: "approval-history-row" },
        element("strong", {}, STATUS_LABELS[decision.ToStatus] || decision.ToStatus),
        element("span", {}, decision.DecisionUserId),
        element("time", {}, formatTime(decision.DecidedAtUtc)),
        element("p", {}, decision.DecisionReason || "未填写说明")));
    }

    const actions = element("div", { className: "approval-detail-actions" });
    const openConversation = element("button", { className: "button secondary", type: "button" });
    openConversation.textContent = "查看原会话";
    openConversation.addEventListener("click", () => onOpenConversation?.(record.ConversationId));
    actions.append(openConversation);
    if (record.Status === "Pending") {
      const approve = element("button", { className: "button primary", type: "button" });
      approve.textContent = "批准";
      const reject = element("button", { className: "button secondary", type: "button" });
      reject.textContent = "拒绝";
      const cancel = element("button", { className: "button ghost", type: "button" });
      cancel.textContent = "取消申请";
      const confirmation = element("div", { className: "approval-confirm", hidden: true });
      approve.addEventListener("click", () => {
        confirmation.hidden = false;
        renderConfirmation(record, "approve", confirmation);
      });
      reject.addEventListener("click", () => {
        confirmation.hidden = false;
        renderConfirmation(record, "reject", confirmation);
      });
      cancel.addEventListener("click", () => {
        confirmation.hidden = false;
        renderConfirmation(record, "cancel", confirmation);
      });
      actions.append(cancel, reject, approve);
      detail.append(header, warning, facts, summary, history, actions, confirmation);
      return;
    }
    if (record.Status === "Approved") {
      const resume = element("button", { className: "button primary", type: "button" });
      resume.textContent = "恢复原会话";
      resume.addEventListener("click", async () => {
        resume.disabled = true;
        try {
          const result = await api.resumeToolApproval(record.Id);
          toast(result.Status === "Completed" ? "工具已执行，原会话已完成。" : "原会话已同步终态。", "success");
          await load(record.Id);
        } catch (error) {
          resume.disabled = false;
          toast(`${error.message}${error.errorCode ? ` · ${error.errorCode}` : ""}`, "error");
        }
      });
      actions.append(resume);
    }
    detail.append(header, warning, facts, summary, history, actions);
  }

  async function select(id) {
    state.selectedId = id;
    renderList();
    const revision = ++state.revision;
    try {
      const value = await api.toolApproval(id);
      if (revision !== state.revision) return;
      state.selected = value;
      renderDetail();
    } catch (error) {
      if (revision !== state.revision) return;
      toast(`审批详情读取失败：${error.message}`, "error");
    }
  }

  async function load(preferredId = state.selectedId) {
    refreshButton.disabled = true;
    const revision = ++state.revision;
    try {
      state.items = await api.toolApprovals({ status: filter.value, take: 200 });
      if (revision !== state.revision) return;
      const selected = state.items.find(value => String(value.Id) === String(preferredId));
      state.selectedId = selected?.Id || state.items[0]?.Id || null;
      renderList();
      if (state.selectedId) await select(state.selectedId);
      else {
        state.selected = null;
        renderDetail();
      }
    } catch (error) {
      if (revision !== state.revision) return;
      toast(`审批队列读取失败：${error.message}`, "error");
    } finally {
      refreshButton.disabled = false;
    }
  }

  filter.addEventListener("change", () => load(null));
  refreshButton.addEventListener("click", () => load());

  return {
    async load() {
      if (!state.initialized) {
        state.initialized = true;
        state.countdownTimer = window.setInterval(() => {
          if (!page.hidden) {
            renderList();
            if (state.selected && !detail.querySelector(".approval-confirm:not([hidden])")) renderDetail();
            const approval = state.selected?.Approval;
            if (shouldRefreshExpiredApproval(approval) && !state.expiryRefreshInFlight) {
              state.expiryRefreshInFlight = true;
              load(approval.Id).finally(() => { state.expiryRefreshInFlight = false; });
            }
          }
        }, 1000);
      }
      await load();
    },
    async open(id) {
      await load(id);
    }
  };
}
