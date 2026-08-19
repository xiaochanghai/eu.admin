# Agent React 管理页

React 页面位于 `eu.admin.react/src/views/agent`，负责 Agent Definition 与 MCP Server 管理。

## 页面边界

- 列表使用 `TableList`，模块代码固定为 `AG_AGENT_DEFINITION_MNG`。
- 列表数据由 `SmModules`、`SmModuleSql`、`SmModuleColumn` 驱动，不在 React 页面重复实现分页和筛选。
- 编辑抽屉使用 Agent API，覆盖创建、Draft 保存、发布、启停、归档/恢复、能力绑定、Main Agent 和导出。
- 编排及评估中的历史 Agent 版本仍是不可变引用，不会因编辑页发布新版本而被改写。
- MCP Server 模块代码固定为 `AG_MCP_SERVER_MNG`，页面组件为 `/agent/mcpServer/index`。
- MCP Server 列表与 Agent Definition 一样使用 `TableList`，由 `SmModules`、`SmModuleSql`、`SmModuleColumn` 驱动；自定义 `FormPage` 负责配置、同步、启停、归档和工具风险操作。
- 两个页面均复用 `@/api` 请求实例，并通过同源 `/Agent/*` 路径访问 Agent API。

## 部署步骤

1. 按需在 SQL Server 执行 `065_add_agent_definition_admin_module.sql` 和 `066_add_mcp_server_admin_module.sql`。
2. 清理服务端 ModuleInfo、ModuleSql、ModuleSqlColumn、用户菜单和权限缓存，或重启对应 API 实例。
3. 在模块管理中将 `AG_AGENT_DEFINITION_MNG`、`AG_MCP_SERVER_MNG` 分配给目标角色。MCP Server 后端接口受 Agent 管理员策略保护，只应授权给对应管理员角色。
4. 确认 Vite、网关或反向代理把同源 `/Agent/*` 请求转发到 Agent API；页面不需要单独的 `VITE_AGENT_API_URL`。
5. Agent API 的 JWT 验证配置必须与主 API 保持一致；跨源部署时还需配置允许的管理端 Origin。不要在前端变量中保存 Agent 模型密钥。

## 验证重点

- 菜单可见、直接访问及刷新恢复。
- 列表分页、Code/名称/职责/状态筛选和状态标签。
- 新建后 Draft 保存；若 Draft 保存失败，已创建的 Agent 仍保留在编辑抽屉中，可修正后重试。
- 发布后版本历史更新；当前 Main Agent 发布后自动切换到最新版本。
- 停用后归档、归档引用阻止、恢复，以及乐观并发冲突。
- Skill、MCP、知识库、子 Agent、编排绑定与导出。
- MCP Server 列表、创建、编辑、启停、发现同步、归档、恢复及并发冲突提示。
