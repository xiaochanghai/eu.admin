import { clear, element, setText } from "./dom.js";
import { skillsApi } from "./skills-api.js?v=2";
import { createSkillEditor } from "./skill-editor.js?v=2";

export function createSkillsPage({ toast, onPublishedChanged }) {
  const state = { skills: [], search: "", category: "", status: "" };
  const rows = document.querySelector("#skillRows");
  const status = document.querySelector("#skillListStatus");
  const table = document.querySelector("#skillTableWrap");
  const empty = document.querySelector("#skillEmptyState");
  const count = document.querySelector("#skillCount");
  let timer;
  const editor = createSkillEditor({
    api: skillsApi,
    toast,
    onChanged: async () => {
      await load();
      await onPublishedChanged();
    }
  });

  function render() {
    clear(rows);
    setText(count, `共 ${state.skills.length} 个 Skill`);
    status.hidden = true;
    table.hidden = state.skills.length === 0;
    empty.hidden = state.skills.length !== 0;
    for (const skill of state.skills) {
      const name = element("strong");
      name.textContent = skill.name || skill.code;
      const code = element("code");
      code.textContent = skill.code;
      const statusBadge = element("span", {
        className: `badge ${skill.status === "Archived" ? "disabled" : "enabled"}`
      }, skill.status === "Archived" ? "已归档" : "Active");
      const description = element("p", { className: "row-description" });
      description.textContent = skill.description || "尚未填写说明";
      const open = element("button", { className: "row-action", type: "button" });
      open.textContent = "管理";
      open.addEventListener("click", async () => {
        open.disabled = true;
        try { await editor.open(await skillsApi.get(skill.id)); }
        catch (error) { toast(error.message, "error"); }
        finally { open.disabled = false; }
      });
      rows.append(element("tr", {},
        element("td", {}, element("div", { className: "agent-identity" },
          element("span", { className: "agent-avatar", ariaHidden: "true" }, "◇"),
          element("div", {}, name, code, statusBadge))),
        element("td", {}, description),
        element("td", {}, skill.category || "未分类"),
        element("td", {}, skill.status === "Archived" ? "已归档" : "正常"),
        element("td", {}, `rev ${skill.draftRevision}`),
        element("td", {}, skill.currentPublishedLabel ? `v${skill.currentPublishedLabel}` : "仅 Draft"),
        element("td", {}, open)));
    }
    renderCategories();
  }

  function renderCategories() {
    const select = document.querySelector("#skillCategoryFilter");
    const selected = select.value;
    const categories = [...new Set(state.skills.map(skill => skill.category).filter(Boolean))].sort();
    clear(select);
    const all = element("option");
    all.value = "";
    all.textContent = "全部分类";
    select.append(all);
    categories.forEach(category => {
      const item = element("option");
      item.value = category;
      item.textContent = category;
      select.append(item);
    });
    select.value = selected;
  }

  async function load() {
    status.hidden = false;
    setText(status, "正在加载 Skill…");
    table.hidden = true;
    empty.hidden = true;
    try {
      state.skills = await skillsApi.list({
        search: state.search,
        category: state.category,
        status: state.status
      });
      render();
    } catch (error) {
      setText(status, `${error.message}。`);
      setText(count, "读取失败");
    }
  }

  document.querySelector("#createSkillButton").addEventListener("click", () => editor.open());
  document.querySelector("#emptyCreateSkillButton").addEventListener("click", () => editor.open());
  document.querySelector("#skillSearchInput").addEventListener("input", event => {
    clearTimeout(timer);
    timer = setTimeout(() => {
      state.search = event.target.value.trim();
      load();
    }, 220);
  });
  document.querySelector("#skillCategoryFilter").addEventListener("change", event => {
    state.category = event.target.value;
    load();
  });
  document.querySelector("#skillStatusFilter").addEventListener("change", event => {
    state.status = event.target.value;
    load();
  });

  return { load, editor };
}
