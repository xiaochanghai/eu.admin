# Agent API 统一返回实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**目标：** 将 `EU.Core.Api.Agent` 的普通 JSON 接口按 Controller 分批改为显式返回 `ServiceResult<T>` / `ServicePageResult<T>`，同步切换内置前端，最终删除 `ApiProblemResults` 和裸数据请求路径。

**架构：** Controller 负责显式构造响应并设置 HTTP 状态；`AgentApiErrorCatalog` 只保存 `ErrorCode → 业务 Status + HTTP Status`；迁移期间已改造 Action 使用共享 PascalCase 序列化选项，前端通过严格的 `requestServiceJson` 解包，未改造接口继续使用 `requestJson`。全部完成后再切换宿主全局 PascalCase 并清理临时双入口。

**技术栈：** .NET 10、ASP.NET Core MVC、`EU.Core.Model.ServiceResult<T>`、xUnit、原生 ES Module、Node.js `node:test`。

## 全局约束

- 任务分类：`BACKEND-BUSINESS`、`BACKEND-HOST`、`API-CONTRACT`、`CROSS-END-CONTRACT`、`TESTS`；不涉及数据库结构。
- 每批严格执行：失败测试（RED）→ 最小实现（GREEN）→ 后端构建/前端脚本测试 → `git diff --check` → 独立提交。
- 不新增全局 Action 结果过滤器，不新增承担响应创建职责的 `AgentApiResults`。
- `AgentApiErrorCatalog` 不依赖 `HttpContext`、`IActionResult` 或 JSON 序列化。
- Controller 使用真实 HTTP 状态码；响应体成功 `Status=200`，失败使用固定的 `600000–699999` 业务状态。
- 失败数据统一为 `AgentApiErrorData { ErrorCode, TraceId }`；生产环境不向 `MessageDev` 或 `Data` 写入异常堆栈、配置和凭据。
- `requestServiceJson` 只接受 PascalCase 统一结构，不自动探测或兼容裸数据。
- `Data` 只解包、不递归改键名；JSON Schema、工具参数、模型输出、查询结果等动态键保持原值。
- SSE、文件内容/下载、指标、健康检查和 HTTP 204 的成功响应不包装；在响应开始前产生的错误仍统一。
- 测试不连接 SQL Server、Redis、RabbitMQ、外部模型或 MCP；需要真实交互的部分集中到最终手工验收。
- 每次暂存仅包含当前批次文件，保护 `eu.admin.react/.env.development` 和现有未跟踪报告。

## 统一实现模板

普通成功 Action 使用明确泛型：

```csharp
ServiceResult<AgAgentDefinitionDetailDto> response =
    ServiceResult<AgAgentDefinitionDetailDto>.QuerySuccess(value);

return new JsonResult(response, AgentJsonSerialization.PascalCase)
{
    StatusCode = StatusCodes.Status200OK
};
```

创建成功保留 HTTP 201 和 `Location`，响应体仍为业务 `Status=200`：

```csharp
Response.Headers.Location = $"/api/agents/{value.ID}";
return new JsonResult(
    ServiceResult<AgAgentDefinitionDetailDto>.OprateSuccess(value, "创建成功"),
    AgentJsonSerialization.PascalCase)
{
    StatusCode = StatusCodes.Status201Created
};
```

失败由 Controller 显式构造，不通过结果帮助器：

```csharp
AgentApiErrorDescriptor descriptor = AgentApiErrorCatalog.Resolve(errorCode);
var response = ServiceResult<AgentApiErrorData>.Failure(
    descriptor.Status,
    message,
    new AgentApiErrorData(errorCode, HttpContext.TraceIdentifier));

return new JsonResult(response, AgentJsonSerialization.PascalCase)
{
    StatusCode = descriptor.HttpStatus
};
```

## Task 1：公共响应契约与错误映射

**Files:**

- Modify: `eu.core/EU.Core.Model/ServiceResult.cs`
- Create: `eu.core/EU.Core.Model/ViewModels/Extend/AgentApiResponseContracts.cs`
- Create: `eu.core/EU.Core.Api.Agent/Errors/AgentApiErrorCatalog.cs`
- Create: `eu.core/EU.Core.Api.Agent/Configuration/AgentJsonSerialization.cs`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgAgentApiResponseFoundation_Should.cs`
- Reference: `doc/待办需求/Agent API ErrorCode固定清单.md`

- [ ] 1.1 新增失败契约测试，逐项断言固定清单中的 187 个专属 ErrorCode（186 个现有、1 个目标新增）均已登记、ErrorCode 和 Status 均唯一、Status 位于所属号段。
- [ ] 1.2 增加代表性映射断言：`REQUEST_INVALID`、`AGENT_NOT_FOUND`、`SKILL_ARCHIVE_BLOCKED`、`MCP_DISABLE_BLOCKED`、`KNOWLEDGE_SERVICE_UNAVAILABLE`、`ORCHESTRATION_RUN_INPUT_INVALID`、`MODEL_INVOCATION_FAILED`、`MODEL_JUDGE_EXECUTION_FAILED`、`AGENT_AUDIT_UNAVAILABLE`、`UNEXPECTED_ERROR`。
- [ ] 1.3 增加未知错误断言：任意未登记 ErrorCode 必须解析为业务 `699999` 和 HTTP 500，且保留原始 ErrorCode 供调用方记录。
- [ ] 1.4 增加 JSON 测试：外层及 DTO 属性为 PascalCase，`Dictionary<string, object>` 中的 `json_schema_key` 原样保留。
- [ ] 1.5 运行 RED：

```powershell
cd E:\EU\EU.Admin\eu.core
dotnet test Src\EU.Core.Tests\EU.Core.Tests.csproj -c Release -p:GenerateDocumentationFile=false --filter FullyQualifiedName~AgAgentApiResponseFoundation_Should
```

预期：因契约、目录和序列化选项尚不存在而失败。

- [ ] 1.6 在 `ServiceResult<T>` 新增非破坏性 `Failure(int status, string message, T data = default, string messageDev = null)` 工厂；不改变现有工厂行为。
- [ ] 1.7 新增不可变 `AgentApiErrorData` 和 `AgentApiErrorDescriptor`，字段分别覆盖 ErrorCode/TraceId 与 Status/HttpStatus。
- [ ] 1.8 将固定清单整体录入 `AgentApiErrorCatalog`；公开只读枚举用于完整性测试，`Resolve` 提供 699999/500 兜底。
- [ ] 1.9 新增 `AgentJsonSerialization.PascalCase`，仅配置命名和项目已有 JSON 行为，不承载响应包装。
- [ ] 1.10 重跑测试至 GREEN，并执行：

```powershell
dotnet build EU.Core.Api.Agent\EU.Core.Api.Agent.csproj -c Release -p:GenerateDocumentationFile=false
git diff --check
```

- [ ] 1.11 提交：`feat(agent): add service response contracts and error catalog`

## Task 2：前端严格统一请求入口

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/http.js`
- Create: `eu.core/EU.Core.Api.Agent/wwwroot/js/tests/http.test.js`
- Create: `eu.core/EU.Core.Api.Agent/package.json`

- [ ] 2.1 先写 `node:test`：完整成功结构只返回 `Data`；HTTP 201 + `Status=200` 成功；`Success=false` 抛出带 `businessStatus`、`errorCode`、`traceId` 的 Error；裸数组、裸对象和缺失字段必须拒绝。
- [ ] 2.2 增加动态数据测试，确认 `Data.schema.required_field` 和任意业务键不会被改名。
- [ ] 2.3 运行 RED：

```powershell
cd E:\EU\EU.Admin\eu.core\EU.Core.Api.Agent
node --test wwwroot\js\tests\http.test.js
```

预期：`requestServiceJson` 或可测试的解析函数尚不存在而失败。

- [ ] 2.4 在 `http.js` 提取纯函数 `parseServiceResponse(payload, httpStatus, fallbackMessage)`，新增 `requestServiceJson`；保留现有 `requestJson` 和 `createApiError`。
- [ ] 2.5 `requestServiceJson` 对非 2xx 和 `Success=false` 使用同一 PascalCase 错误结构；不回退读取旧 ProblemDetails 字段。
- [ ] 2.6 在 `package.json` 仅声明 ES Module 和 `test` 脚本，不引入第三方依赖或锁文件。
- [ ] 2.7 重跑 Node 测试至 GREEN，并运行 Task 1 测试和 `git diff --check`。
- [ ] 2.8 提交：`feat(agent-ui): add strict service response client`

## Task 3：Agent、主 Agent 与 Skill

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/Controllers/AgentsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/MainAgentController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/SkillsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/SkillVersionsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/api-client.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/skills-api.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/agent-editor.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/agent-runner.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/skill-editor.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/skills-page.js`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgAgentAndSkillApiResponse_Should.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/tests/http.test.js`

- [ ] 3.1 写 Controller 契约测试，覆盖 Agents 列表/详情/创建/保存草稿/发布/状态/导入，MainAgent 查询/设置，Skills 列表/详情/创建/保存/文件列表/文件写入/删除/发布/归档，SkillVersions 列表。
- [ ] 3.2 RED 断言至少包括：查询体为 `ServiceResult<T>`、创建 HTTP 201 且 `Status=200`、冲突和未找到返回固定业务 Status、文件内容成功响应保持原协议。
- [ ] 3.3 逐 Action 显式返回统一模型；仅保留 Agent 包导出和 Skill 文件内容读取的原始文件/文本响应，上传、导入及写入结果仍包装。
- [ ] 3.4 将本批前端 API 方法改用 `requestServiceJson`；页面将已迁移 DTO 属性访问显式改为 PascalCase。
- [ ] 3.5 扩展前端测试，使用本批代表性响应验证列表、单项和失败解包；不得添加两种格式自动识别。
- [ ] 3.6 运行：

```powershell
cd E:\EU\EU.Admin\eu.core
dotnet test Src\EU.Core.Tests\EU.Core.Tests.csproj -c Release -p:GenerateDocumentationFile=false --filter "FullyQualifiedName~AgAgentAndSkillApiResponse_Should|FullyQualifiedName~AgAgentApiResponseFoundation_Should"
cd EU.Core.Api.Agent
npm test
cd ..
dotnet build EU.Core.Api.Agent\EU.Core.Api.Agent.csproj -c Release -p:GenerateDocumentationFile=false
git diff --check
```

- [ ] 3.7 手工冒烟：Agent 列表/编辑/发布/导入导出、主 Agent 设置、Skill 文件增删和发布归档。
- [ ] 3.8 提交：`refactor(agent): standardize agent and skill responses`

## Task 4：MCP 与工具审批

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/Controllers/McpServersController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/McpToolVersionsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/ToolApprovalsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/mcp-api.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/mcp-page.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/approval-page.js`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgMcpApiResponse_Should.cs`

- [ ] 4.1 写 RED 测试覆盖 MCP 列表/详情/创建/更新/同步/归档/风险更新、工具版本列表、审批查询/批准/拒绝/取消/恢复。
- [ ] 4.2 断言 `MCP_DISCOVERY_FAILED`、`MCP_DISABLE_BLOCKED`、审批状态冲突映射一致，并保留恢复运行所需 ID。
- [ ] 4.3 显式改造三个 Controller，禁止继续调用 `ApiProblemResults`。
- [ ] 4.4 将 `mcp-api.js` 及审批调用改用 `requestServiceJson`，同步页面 PascalCase 属性。
- [ ] 4.5 运行本批 xUnit、`npm test`、Agent API Release build 和 `git diff --check`。
- [ ] 4.6 手工冒烟：同步工具、调整风险、被 Agent 引用时停用受阻、审批全状态流转。
- [ ] 4.7 提交：`refactor(agent): standardize mcp and approval responses`

## Task 5：知识库

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/Controllers/KnowledgeBasesController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/api-client.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/knowledge-page.js`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgKnowledgeApiResponse_Should.cs`

- [ ] 5.1 写 RED 测试覆盖知识库列表/详情/创建/更新/归档、文档上传 JSON 结果、PDF 上传 JSON 结果、文档列表、分块列表、检索、引用列表。
- [ ] 5.2 断言 multipart 请求格式不变，文档内容流不包装，`KNOWLEDGE_DOCUMENT_INVALID` 和 `KNOWLEDGE_SERVICE_UNAVAILABLE` 使用固定映射。
- [ ] 5.3 改造 `KnowledgeBasesController` 与同文件中的 `KnowledgeBaseReferencesController`，确保动态检索内容和 metadata 键不被改名。
- [ ] 5.4 切换知识库前端方法和页面 PascalCase 属性，保留 PDF 的 `FormData` 上传方式。
- [ ] 5.5 运行本批 xUnit、`npm test`、Agent API Release build 和 `git diff --check`。
- [ ] 5.6 手工冒烟：PDF 导入并索引、分块查看、检索命中、被 Agent 引用时归档受阻。
- [ ] 5.7 提交：`refactor(agent): standardize knowledge responses`

## Task 6：编排

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/Controllers/OrchestrationsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/api-client.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/orchestration-page.js`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgOrchestrationApiResponse_Should.cs`

- [ ] 6.1 写 RED 测试覆盖编排列表/详情/创建/草稿/发布/归档、运行创建/列表/详情/取消/执行详情/输出。
- [ ] 6.2 断言运行内部 ErrorCode 继续保留在运行 DTO，HTTP 边界错误按固定映射；输出中的动态键不改名。
- [ ] 6.3 显式改造 Controller；如输出接口是文件/纯文本则保持原协议，JSON 输出才包装。
- [ ] 6.4 切换编排前端调用和页面 PascalCase 属性。
- [ ] 6.5 运行本批 xUnit、`npm test`、Agent API Release build 和 `git diff --check`。
- [ ] 6.6 手工冒烟：保存草稿、发布、运行、节点详情、取消、归档和失败运行查看。
- [ ] 6.7 提交：`refactor(agent): standardize orchestration responses`

## Task 7：质量评估与模型评审

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/Controllers/EvaluationSuitesController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/EvaluationBatchesController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/RunEvaluationsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/api-client.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/evaluation-page.js`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgEvaluationApiResponse_Should.cs`

- [ ] 7.1 写 RED 测试覆盖评估集列表/详情/创建/草稿/发布/归档，批次运行/对比/列表/详情，模型评审创建/历史列表/详情，单次 Run 评估。
- [ ] 7.2 断言断言失败属于正常完成的评估结果而不是 HTTP 500；真正执行失败使用固定 ErrorCode/Status。
- [ ] 7.3 显式改造三个 Controller，保存历史模型评审 DTO 的完整字段。
- [ ] 7.4 切换质量评估前端调用和页面 PascalCase 属性，确保 BATCHES、对比和历史报告仍使用解包后的 `Data`。
- [ ] 7.5 运行本批 xUnit、`npm test`、Agent API Release build 和 `git diff --check`。
- [ ] 7.6 手工冒烟：发布评估集、运行批次、展开断言、查看追踪、基线对比、历史模型评审。
- [ ] 7.7 提交：`refactor(agent): standardize evaluation responses`

## Task 8：Chat、Agent Runs、Audit 与平台 JSON

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/Controllers/ChatRunsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/AgentRunsController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/AuditController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/BusinessQueryRetentionController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Controllers/PlatformController.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/api-client.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/chat-page.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/agent-runner.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/app.js`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgRuntimeApiResponse_Should.cs`

- [ ] 8.1 写 RED 测试覆盖 Chat 会话列表/详情/运行列表/运行详情/执行明细/取消，Agent run 创建/列表，审计查询、保留数据清理、平台 service/capabilities。
- [ ] 8.2 明确 `ChatRunsController` 的 SSE events 成功响应保持流协议；只改造会话及运行普通 JSON 和流开始前错误。
- [ ] 8.3 `MetricsController` 保持指标协议，不纳入统一成功包装。
- [ ] 8.4 显式改造其余普通 JSON Actions，确保知识检索事件、工具调用明细和业务查询结果中的动态载荷不改键名。
- [ ] 8.5 切换剩余业务前端调用和 PascalCase 属性；保留 SSE 解析器原事件字段约定。
- [ ] 8.6 运行本批 xUnit、`npm test`、Agent API Release build 和 `git diff --check`。
- [ ] 8.7 手工冒烟：会话加载、发送消息、SSE 事件、取消、运行详情、知识命中和 MCP 工具追踪。
- [ ] 8.8 提交：`refactor(agent): standardize runtime and platform responses`

## Task 9：宿主错误边界

**Files:**

- Modify: `eu.core/EU.Core.Api.Agent/Program.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Errors/ProblemDetailsMiddleware.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Errors/RequestBodyLimitMiddleware.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Security/AgentApiSecurityServiceCollectionExtensions.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Security/AgentAuthorizationResultHandler.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Security/AgentOperationAuditMiddleware.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Security/ExpensiveRequestAdmissionMiddleware.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Security/HttpIdempotencyMiddleware.cs`
- Create: `eu.core/EU.Core.Api.Agent/Errors/AgentApiErrorResponseWriter.cs`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgAgentHostErrorResponse_Should.cs`

- [ ] 9.1 写内存 HTTP RED 测试，覆盖模型校验 400、认证 401、授权 403、请求体 413、媒体类型 415、限流 429、审计/依赖 503、未处理异常 500。
- [ ] 9.2 对每种错误同时断言：真实 HTTP 状态、业务 Status、`Success=false`、`Data.ErrorCode`、`Data.TraceId`、`application/json` 和现有关联 ID 响应头。
- [ ] 9.3 写幂等回放测试：第一次和回放的 HTTP status、Content-Type、统一响应体逐字节相同；冲突使用固定错误映射。
- [ ] 9.4 新增仅供中间件使用的 `AgentApiErrorResponseWriter`，职责是把明确参数序列化为 `ServiceResult<AgentApiErrorData>`；它不返回 `IActionResult`，不替 Controller 决策。
- [ ] 9.5 将 `InvalidModelStateResponseFactory`、认证/授权事件和全部中间件切换为目录映射及统一 Writer；未知错误记录原 ErrorCode，向客户端输出安全消息。
- [ ] 9.6 保留 `UseResponseBodyRead` 和 `UseRequestResponseLogMidd`，本任务不移除既有请求响应日志能力。
- [ ] 9.7 运行本批 xUnit、Task 1 测试、`npm test`、Agent API Release build 和 `git diff --check`。
- [ ] 9.8 手工负向验证：无 Token、无权限、超限请求、重复幂等键、依赖不可用；检查日志不泄漏凭据。
- [ ] 9.9 提交：`refactor(agent): standardize host error responses`

## Task 10：全局收口与完整回归

**Files:**

- Delete: `eu.core/EU.Core.Api.Agent/Controllers/ApiProblemResults.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Program.cs`
- Modify: `eu.core/EU.Core.Api.Agent/Configuration/AgentJsonSerialization.cs`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/http.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/api-client.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/skills-api.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/mcp-api.js`
- Modify: `eu.core/EU.Core.Api.Agent/wwwroot/js/tests/http.test.js`
- Modify: all migrated Controller files listed in Tasks 3–8
- Modify: `doc/待办需求/修改agent api接口返回.md`
- Create: `eu.core/Src/EU.Core.Tests/Service_Test/AgAgentApiMigrationCompletion_Should.cs`

- [ ] 10.1 写收口 RED 测试：MVC 默认序列化 PascalCase；普通 JSON 路由均声明统一成功/失败响应；特殊协议路由在明确白名单；Swagger 能解析 `ServiceResult<T>` / `ServicePageResult<T>`。
- [ ] 10.2 使用 `rg` 生成并人工审查接口清单，逐个确认每个 Action 属于“统一 JSON”或“特殊协议”，不以源码字符串扫描代替行为测试。
- [ ] 10.3 将 MVC 全局 JSON 命名策略切换为 PascalCase，随后移除 Controller 中临时显式 `AgentJsonSerialization.PascalCase` 参数，Controller 仍显式构造 `ServiceResult<T>`。
- [ ] 10.4 删除全部 `ApiProblemResults` 调用及文件；确认未新增同职责 Controller 结果帮助器。
- [ ] 10.5 删除前端裸数据 `requestJson`，将唯一请求入口命名为 `requestJson` 或保留 `requestServiceJson`，但项目内只能有一种普通 JSON 请求语义；清理所有旧导入。
- [ ] 10.6 补齐 Controller 的 `ProducesResponseType` / OpenAPI 响应模型；特殊协议声明真实 Content-Type。
- [ ] 10.7 更新需求文档验收勾选、实施记录、验证结果和批次提交；记录任何未执行的外部依赖测试。
- [ ] 10.8 运行自动验证：

```powershell
cd E:\EU\EU.Admin\eu.core
dotnet test Src\EU.Core.Tests\EU.Core.Tests.csproj -c Release -p:GenerateDocumentationFile=false --filter FullyQualifiedName~AgAgent
dotnet build EU.Core.sln -c Release -p:GenerateDocumentationFile=false
cd EU.Core.Api.Agent
npm test
cd E:\EU\EU.Admin
rg -n "ApiProblemResults|requestServiceJson|requestJson" eu.core\EU.Core.Api.Agent
git diff --check
git status --short
```

- [ ] 10.9 启动本地 Agent API 后执行完整手工验收：Agent、主 Agent、Skill、MCP、审批、知识库、编排、质量评估、Chat、审计和平台能力；验证成功、400、401、403、404、409、413、415、422、429、500、502、503、504 代表路径。
- [ ] 10.10 确认文件导入导出、PDF multipart、Skill 文件内容、SSE、指标、HTTP 204 未被包装，幂等回放仍一致。
- [ ] 10.11 提交：`refactor(agent): complete unified response migration`

## 批次验收记录模板

每完成一批，在本文件对应 Task 下勾选并在需求文档追加：

```text
批次：
提交：
后端测试：命令 + 通过/失败数量
前端测试：命令 + 通过/失败数量
构建：命令 + 结果
手工验证：已验证项 / 待用户统一验证项
未运行：项目 + 原因
风险与回滚：
```

## 最终完成条件

- 所有普通 JSON Action 显式返回 `ServiceResult<T>` / `ServicePageResult<T>`。
- `ApiProblemResults` 和前端长期双入口已删除。
- 固定 ErrorCode 清单全部有唯一业务 Status/HTTP Status 映射。
- 内置前端仅消费 PascalCase 统一契约，动态 `Data` 键不被转换。
- 自动测试、解决方案构建、关键手工链路均有可核对记录。
- 后端与内置前端以同一提交发布，回滚也以同一提交执行。
