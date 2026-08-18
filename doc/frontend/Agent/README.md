# Agent Definition React 管理页

React 页面位于 `eu.admin.react/src/views/agent/agentDefinition`，负责 Agent Definition 的列表和自定义编辑。

## 页面边界

- 列表使用 `TableList`，模块代码固定为 `AG_AGENT_DEFINITION_MNG`。
- 列表数据由 `SmModules`、`SmModuleSql`、`SmModuleColumn` 驱动，不在 React 页面重复实现分页和筛选。
- 编辑抽屉使用 Agent API，覆盖创建、Draft 保存、发布、启停、归档/恢复、能力绑定、Main Agent 和导出。
- 编排及评估中的历史 Agent 版本仍是不可变引用，不会因编辑页发布新版本而被改写。

## 部署步骤

1. 在 SQL Server 执行 `eu.core/EU.Core.Api.Agent/Database/Migrations/SqlServer/065_add_agent_definition_admin_module.sql`。
2. 清理服务端 ModuleInfo、ModuleSql、ModuleSqlColumn 和用户权限缓存，或重启对应 API 实例。
3. 在模块管理中将 `AG_AGENT_DEFINITION_MNG` 分配给目标角色，并授予 `Query`、`Add`、`Update`、`View` 权限。
4. 默认情况下，React 管理端与 Agent API 使用同一站点入口；若 Agent API 独立部署，设置 `VITE_AGENT_API_URL` 为 Agent API Origin，例如 `http://localhost:62844`，不要包含 `/api` 后缀。
5. Agent API 的 CORS `AllowedOrigins` 必须包含 React 管理端 Origin，JWT 验证配置必须与主 API 保持一致。

## 验证重点

- 菜单可见、直接访问及刷新恢复。
- 列表分页、Code/名称/职责/状态筛选和状态标签。
- 新建后 Draft 保存；若 Draft 保存失败，已创建的 Agent 仍保留在编辑抽屉中，可修正后重试。
- 发布后版本历史更新；当前 Main Agent 发布后自动切换到最新版本。
- 停用后归档、归档引用阻止、恢复，以及乐观并发冲突。
- Skill、MCP、知识库、子 Agent、编排绑定与导出。
