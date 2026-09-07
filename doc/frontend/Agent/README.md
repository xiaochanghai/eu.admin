# Agent React 管理页

React 页面位于 `eu.admin.react/src/views/agent`，目前负责 Agent Definition、MCP Server 与 Skill 管理。

## 页面边界

- 列表使用 `TableList`，模块代码固定为 `AG_AGENT_DEFINITION_MNG`。
- 列表数据由 `SmModules`、`SmModuleSql`、`SmModuleColumn` 驱动，不在 React 页面重复实现分页和筛选。
- 编辑抽屉使用 Agent API，覆盖创建、Draft 保存、发布、启停、归档/恢复、能力绑定、Main Agent 和导出。
- 编排及评估中的历史 Agent 版本仍是不可变引用，不会因编辑页发布新版本而被改写。
- 操作审计模块代码为 `AG_OPERATION_AUDIT_MNG`，页面为 `/agent/audit/index`；它只读取当前租户可见的 Agent API 操作记录。
- Unified Chat 会保留 `BusinessQueryResult` 消息类型；在服务端允许返回展示数据时，历史会话中的业务查询结果按服务端返回的列和显示值渲染为只读表格，缺少列定义时回退显示服务端 Markdown 文本。
- 实时聊天消费服务端 `business-query-result` 事件，将 presentation 交给同一结果组件，不依赖模型 Delta。按 queryId 去重，断线终态恢复时从结果事件补取；不会把 `tool-succeeded` 调试原始数据直接渲染为业务答案。
- Unified Chat 的 MCP 审批会在对话内展示状态卡；它会轮询审批结果，并在服务端允许后恢复或收敛原会话，审批详情与人工决定仍以审批中心为准。
- Agent Definition 的“运行 Agent”抽屉会显示本次流式输出、MCP 工具调用和知识库引用，并保留最近运行审计记录。
- MCP Server 模块代码固定为 `AG_MCP_SERVER_MNG`，页面组件为 `/agent/mcpServer/index`。
- MCP Server 列表与 Agent Definition 一样使用 `TableList`，由 `SmModules`、`SmModuleSql`、`SmModuleColumn` 驱动；自定义 `FormPage` 负责配置、同步、启停、归档和工具风险操作。
- Skill 模块代码为 `AG_SKILL_MNG`，列表同样使用 `TableList`；自定义 `FormPage` 负责基础信息、Draft 文件、发布版本和归档/恢复。
- 三个页面均复用 `@/api` 请求实例，并通过同源 `/Agent/*` 路径访问 Agent API。

## 部署步骤

1. 按需在 SQL Server 执行对应 Agent 管理模块脚本；操作审计页需额外执行 `072_add_operation_audit_admin_module.sql`。所有脚本均须由部署人员审查、单独执行，前端不会自动执行。
2. 清理服务端 ModuleInfo、ModuleSql、ModuleSqlColumn、用户菜单和权限缓存，或重启对应 API 实例。
3. 在模块管理中将 `AG_AGENT_DEFINITION_MNG`、`AG_MCP_SERVER_MNG`、`AG_SKILL_MNG` 分配给目标角色。Agent API 后端接口受管理员策略保护，只应授权给对应管理员角色。
4. 确认 Vite、网关或反向代理把同源 `/Agent/*` 请求转发到 Agent API；页面不需要单独的 `VITE_AGENT_API_URL`。
5. Agent API 的 JWT 验证配置必须与主 API 保持一致；跨源部署时还需配置允许的管理端 Origin。不要在前端变量中保存 Agent 模型密钥。

## 验证重点

- 评测 Suite 发布前自动保存当前表单；保存失败不得继续发布，发布失败后保留已保存草稿的最新修订号。
- 评测的三类多行断言允许逐行输入，编辑时保留空行与空格，保存时再去除空规则和首尾空格。
- 质量门禁展示逐 Case 状态、工具调用增量、新增失败、路由和事件类型差异。
- 模型裁判的 Relevance、Coherence 最低分范围为 1～5，默认 4；仅校验已选指标，仍需明确确认额外模型调用。
- MCP 工具风险分类成功后保留未保存的配置表单，并使用服务端返回的新修订号进行后续保存。
- 菜单可见、直接访问及刷新恢复。
- 列表分页、Code/名称/职责/状态筛选和状态标签。
- 新建后 Draft 保存；若 Draft 保存失败，已创建的 Agent 仍保留在编辑抽屉中，可修正后重试。
- 发布后版本历史更新；当前 Main Agent 发布后自动切换到最新版本。
- 停用后归档、归档引用阻止、恢复，以及乐观并发冲突。
- Skill、MCP、知识库、子 Agent、编排绑定与导出。
- Agent Definition 编辑时取消 MCP 工具绑定后，保存按钮状态变化不得触发 Draft 重新加载、恢复旧选择；全部取消时提交 `toolVersionIds: []`。保存后重新打开应保持空绑定；运行版本需另行发布才会更新，历史发布版本不变。
- MCP Server 列表、创建、编辑、启停、发现同步、归档、恢复及并发冲突提示。
- Skill 列表筛选、新建与编辑、Draft 文件维护、版本发布、Agent 引用展示、归档阻止及乐观并发冲突。
