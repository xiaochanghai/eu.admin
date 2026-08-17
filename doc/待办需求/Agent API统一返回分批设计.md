# Agent API 统一返回分批设计

## 1. 目标

将 `EU.Core.Api.Agent` 的普通 JSON 接口分批改造成与 `EU.Core.Api` 一致的
`ServiceResult<T>` / `ServicePageResult<T>` 返回结构，同时满足以下约束：

- 每个 Controller 显式返回统一结果，不使用全局结果过滤器自动包装；
- Agent 业务失败使用 `600000–699999` 的业务 `Status`；
- HTTP 状态码继续表达标准 HTTP 语义；
- 后端与 `EU.Core.Api.Agent/wwwroot` 内置前端按批次同步切换；
- 最终删除 `ApiProblemResults`，不新增承担相同职责的 `AgentApiResults`；
- SSE、文件内容和基础设施协议端点保持原协议。

需求事实源：

- [统一 Agent API 接口返回](修改agent%20api接口返回.md)
- [Agent API ErrorCode 固定清单](Agent%20API%20ErrorCode固定清单.md)
- [Agent API 统一返回实施计划](Agent%20API统一返回实施计划.md)

## 2. 核心设计

### 2.1 Controller 显式返回

普通 JSON Action 显式构造 `ServiceResult<T>` 或 `ServicePageResult<T>`，并通过 MVC
结果设置实际 HTTP 状态码。Controller 返回类型应尽量使用明确 DTO，不新增匿名返回结构。
分批期间，已迁移 Action 使用共享的 PascalCase `JsonSerializerOptions` 创建 `JsonResult`；这样
不会提前改变尚未迁移接口的裸数据序列化格式。

成功示意：

```csharp
ServiceResult<AgAgentDefinitionDetailDto> response =
    ServiceResult<AgAgentDefinitionDetailDto>.QuerySuccess(value);
return new JsonResult(response, AgentJsonSerialization.PascalCase)
{
    StatusCode = StatusCodes.Status200OK
};
```

创建成功保留 HTTP 201，但响应体业务状态仍为 200：

```csharp
ServiceResult<AgAgentDefinitionDetailDto> response =
    ServiceResult<AgAgentDefinitionDetailDto>.OprateSuccess(value, "创建成功");
Response.Headers.Location = $"/api/agents/{value.ID}";
return new JsonResult(response, AgentJsonSerialization.PascalCase)
{
    StatusCode = StatusCodes.Status201Created
};
```

失败示意：

```csharp
AgentApiErrorDescriptor descriptor = AgentApiErrorCatalog.Resolve(error.Code);
var response = new ServiceResult<AgentApiErrorData>
{
    Status = descriptor.Status,
    Success = false,
    Message = error.Message,
    Count = 0,
    Data = new AgentApiErrorData(error.Code, HttpContext.TraceIdentifier)
};
return new JsonResult(response, AgentJsonSerialization.PascalCase)
{
    StatusCode = descriptor.HttpStatus
};
```

### 2.2 错误映射只负责查询

新增 `AgentApiErrorCatalog`，职责仅限于：

- 保存完整固定的 `ErrorCode → Status + HTTP Status` 映射；
- 根据 ErrorCode 返回不可变的 `AgentApiErrorDescriptor`；
- 未登记错误码返回业务 `Status=699999`、HTTP 500，并允许调用方记录告警。

它不创建 `IActionResult`、不访问 `HttpContext`、不序列化响应，也不承担 Controller 逻辑。
这样可以集中固化错误码，同时满足最终移除 `AgentApiResults` 类结果帮助器的目标。

### 2.3 前端分批切换

分批期间保留两个职责明确的请求函数：

```js
requestJson(path, options)        // 未改造接口：返回原始 JSON
requestServiceJson(path, options) // 已改造接口：校验统一结构并返回 Data
```

`requestServiceJson` 必须：

- 要求响应包含 `Status`、`Success`、`Message` 和 `Data`；
- 成功时只向页面返回 `Data`；
- 失败时读取业务 `Status`、`Data.ErrorCode` 和 `Data.TraceId`；
- 原样返回 PascalCase 的 `Data`，由同批页面消费者显式使用 PascalCase 属性；
- 不接受裸数据，不通过响应形状猜测两种格式。

每个批次同时切换对应的前端 API 方法和页面属性访问。不得递归转换 `Data` 的键名，因为
JSON Schema、工具参数、模型输出和业务查询字段中的键属于业务数据，不能被大小写转换。
全部普通 JSON 接口迁移完成后，删除旧的裸数据 `requestJson` 路径，并将统一请求函数恢复为
项目内唯一入口。

### 2.4 中间件显式序列化

授权、限流、幂等、审计、请求体限制和异常处理中间件不经过 Controller，因此直接创建并
序列化 `ServiceResult<AgentApiErrorData>`。它们复用 `AgentApiErrorCatalog`，但不复用
Controller 结果帮助器。

中间件必须保留：

- 实际 HTTP 状态码；
- `X-Correlation-ID` 等现有关联 ID 响应头；
- 业务 Status、ErrorCode 和 TraceId；
- 生产环境敏感信息保护；
- 幂等缓存对响应状态、Content-Type 和响应体的完整回放。

### 2.5 JSON 与特殊协议

已迁移的普通 MVC JSON 使用显式 PascalCase 序列化，与 `EU.Core.Api` 保持一致。全部迁移完成
后才将 Agent 宿主的 MVC 全局命名策略切换为 PascalCase，并删除分批期间的显式序列化选项。
以下成功响应不包装：

- SSE 聊天和运行事件流；
- Agent 包、文件和文档内容下载；
- 指标、健康检查及其他基础设施协议响应；
- HTTP 204。

特殊协议在响应开始前失败时仍返回统一错误结构；响应开始后的失败保留在事件或运行记录中。

## 3. 分批顺序

每批均执行“失败契约测试 → 后端显式改造 → 前端 API 切换 → 针对性回归 → 独立提交”。

### 批次 1：公共契约和映射基础

- 补充 `ServiceResult<T>` 所需的非破坏性构造方法；
- 新增 `AgentApiErrorData`、`AgentApiErrorDescriptor` 和 `AgentApiErrorCatalog`；
- 新增只保存 PascalCase `JsonSerializerOptions` 的 `AgentJsonSerialization`，不得包含结果包装逻辑；
- 将固定清单中的 186 个现有错误码和 1 个目标新增错误码（共 187 个专属映射）写入映射；
- 添加映射完整性、唯一性、号段和兜底测试；
- 不改变任何现有接口返回。

### 批次 2：前端统一请求入口

- 新增严格的 `requestServiceJson`；
- 添加 ServiceResult 校验和错误构造测试，确认 `Data` 键名保持不变；
- 保留现有 `requestJson`，本批不切换业务接口。

### 批次 3：Agent、主 Agent 和 Skill

- 改造 Agents、MainAgent、Skills、SkillVersions；
- 同步切换 `api-client.js`、`skills-api.js` 对应方法及页面属性访问；
- 覆盖查询、创建、保存草稿、发布、归档、导入和文件管理。

### 批次 4：MCP 和工具审批

- 改造 McpServers、McpToolVersions、ToolApprovals；
- 同步切换 `mcp-api.js`、审批相关前端方法及页面属性访问；
- 验证发现失败、禁用阻止、审批冲突和恢复执行。

### 批次 5：知识库

- 改造 KnowledgeBases、KnowledgeBaseReferences；
- 同步切换知识库前端 API 及页面属性访问；
- JSON 查询和上传结果包装，PDF multipart 请求格式保持不变；
- 文档内容流保持原协议。

### 批次 6：编排

- 改造 Orchestrations 的定义、运行列表、详情、取消和输出 JSON 接口；
- 保留运行事件中的 ErrorCode；
- 同步切换编排前端 API 及页面属性访问。

### 批次 7：质量评估

- 改造 EvaluationSuites、EvaluationBatches、RunEvaluations 和模型评审报告；
- 同步切换质量评估前端 API 及页面属性访问；
- 验证批次失败、断言失败、模型评审失败和历史报告查询。

### 批次 8：Chat、Agent Runs、Audit 和平台 JSON 接口

- 改造 Chat 的会话、运行、详情和取消等非 SSE 接口；
- 改造 AgentRuns、Audit、Capabilities 等普通 JSON 接口；
- SSE 成功响应保持原协议，仅统一流开始前的错误；
- 同步切换剩余前端 API 方法及页面属性访问。

### 批次 9：宿主错误边界

- 改造参数校验、授权、限流、请求准入、幂等、操作审计和异常处理中间件；
- 统一 `application/json` 错误体；
- 验证 HTTP 状态、业务 Status、ErrorCode、TraceId、关联响应头和幂等回放。

### 批次 10：收口

- 删除 `ApiProblemResults` 及所有调用；
- 删除裸数据 `requestJson` 路径，统一前端请求入口；
- 将 MVC 全局 JSON 命名策略切换为 PascalCase，并移除各 Action 的临时显式序列化选项；
- 清理临时兼容代码和不再使用的 ProblemDetails 断言；
- 补齐 Swagger/OpenAPI 响应模型；
- 运行后端、前端和关键交互全量回归；
- 更新需求状态和实施记录。

## 4. 测试设计

每批至少覆盖：

- HTTP 200/201 成功响应体业务 `Status=200`；
- 单对象、集合、空集合和分页结果；
- 400、401、403、404、409、413、415、422、429、500、502、503、504；
- ErrorCode 到业务 Status 和 HTTP 状态的固定映射；
- 未登记错误码映射到 699999/500；
- 前端严格拒绝裸数据或格式不完整的已迁移接口；
- 已迁移页面显式消费 PascalCase DTO，JSON Schema、工具参数和业务数据键名保持原值；
- SSE、文件下载、PDF 上传、幂等回放不被破坏。

测试优先使用纯单元和内存 HTTP 契约测试，不连接共享数据库或外部模型、MCP、Redis、RabbitMQ。

## 5. 发布与回滚

没有仓库外客户端，因此不提供旧接口版本。开发阶段允许两条明确的前端请求路径，但同一接口
只能属于其中一条。生产发布必须在批次 10 完成后进行，后端和内置前端作为同一版本发布。

回滚以批次提交为单位；若已经发布，则后端和内置前端必须回滚到同一提交，不能单独回滚一端。
