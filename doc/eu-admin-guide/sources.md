# EU-Admin 项目说明来源

> 本清单仅记录本次项目文档使用的本地资料。源码、类型、测试和运行配置优先于历史说明。

1. `AGENTS.md`、`eu.admin.react/AGENTS.md`、`eu.core/AGENTS.md`
   类型：仓库强制约束。用途：任务分类、拥有边界、数据库/安全要求和交付门禁。
2. `README.md`、`doc/readme.md`、`doc/frontend/README.md`、`doc/backend/README.md`
   类型：现有项目入口。用途：技术栈、启动方式和已有文档路由。
3. `eu.admin.react/package.json`、`vite.config.ts`、`src/main.tsx`
   类型：前端配置事实源。用途：依赖、脚本、构建与应用入口。
4. `eu.admin.react/src/views/**`
   类型：前端页面事实源。用途：识别系统、基础资料、采购、销售、库存、设备及示例页面。
5. `eu.admin.react/src/api/**`、`src/redux/**`、`src/routers/**`、`src/layouts/**`
   类型：前端架构事实源。用途：请求边界、状态、权限路由、布局与会话能力。
6. `eu.admin.react/src/components/**`、`src/hooks/**`
   类型：通用前端能力。用途：ProTable、动态表单、上传、图表、AI 对话、权限按钮和工具 Hook。
7. `eu.admin.react/src/workflow/**`、`src/workflow-editor/**`、`src/dsl/**`
   类型：工作流/低代码事实源。用途：流程节点、设置器、编辑器、DSL 和保存/导入导出边界。
8. `eu.core/EU.Core.sln` 与各 `.csproj`
   类型：后端解决方案事实源。用途：确认主 API、Jobs、MCP、Gateway、平台库、测试与代码生成项目。
9. `eu.core/EU.Core.Api/Controllers/**`
   类型：HTTP 契约事实源。用途：盘点 SM、BD、PO、SD、IV、EM、EC、Tenant、Authorize、DbFirst 与通用接口。
10. `eu.core/EU.Core.Services/**`、`EU.Core.IServices/**`、`EU.Core.Repository/**`、`EU.Core.Model/**`
    类型：业务分层事实源。用途：确认服务契约、业务逻辑、数据访问和模型边界。
11. `eu.core/EU.Core.Jobs/**`、`EU.Core.MCP.Api/**`、`Src/EU.Core.Gateway/**`
    类型：独立宿主事实源。用途：后台任务、MCP/AI 和网关能力。
12. `eu.core/Src/EU.Core.Common/**`、`EU.Core.DataAccess/**`、`EU.Core.Extensions/**`、`EU.Core.EventBus/**`、`EU.Core.Tasks/**`、`EU.Core.Serilog*/**`
    类型：后端平台事实源。用途：缓存、数据库、租户、认证、事件、任务和日志。
13. `doc/backend/SmModule/**`、`doc/frontend/SmModule/**`
    类型：当前专题文档。用途：动态页面三表模型、缓存和前端消费规则。
14. `eu.admin.react/src/workflow/README.md`、`src/workflow/WorkflowEditor/README.md`、`src/workflow/setters/README.md`、`src/workflow-editor/README.md`
    类型：源码旁专题文档。用途：工作流两套边界和节点配置。
15. `doc/当前项目分析与问题解决方案.html`、`doc/eu.admin.react深度分析报告.html`、`doc/EU.Core.Jobs项目分析与优化报告.html`
    类型：历史/辅助分析。用途：提供待二次核对的架构线索；不作为最高优先级事实源。
16. `E:/DressCode/porcelain-client-platform/apps/porcelain-frontend/docs/**`
    类型：用户指定的结构参考。用途：文档分层、任务路由、模块地图、状态标记与维护规范；不复制其产品内容。
17. `eu.admin.react/src/views/**` 中的 `moduleCode` 常量与专用 HTTP 调用、`eu.core/EU.Core.Api/Controllers/Base/BaseController.cs`
    类型：本轮实现级复核。用途：建立页面 → moduleCode → 标准/自定义接口链路。
18. `eu.core/EU.Core.Services/SM/SmModulesServices.cs`、`CommonServices.cs`、PO/SD/IV/EM Services
    类型：本轮实现级复核。用途：确认模块聚合、动态 SQL 查询、转单、完结、状态回写和过账行为。
19. `eu.core/EU.Core.Api/Program.cs`、`EU.Core.Jobs/Program.cs`、`EU.Core.MCP.Api/Program.cs`、`Src/EU.Core.Gateway/Program.cs`
    类型：宿主事实源。用途：确认四个独立进程的注册、依赖与生命周期。
