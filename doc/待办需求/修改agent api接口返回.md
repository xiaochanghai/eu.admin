# REQ-202608170103 统一 Agent API 接口返回

## 基本信息

- 编号：REQ-202608170103
- 状态：待开发
- 优先级：P2
- 提出人：sah
- 负责人：sah
- 提出时间：2026-08-17
- 期望完成时间：
- 涉及模块：`EU.Core.Api.Agent`、`EU.Core.Model`、Agent 管理页面

## 背景与问题

`EU.Core.Api` 的普通 JSON 接口以 `ServiceResult<T>` 或 `ServicePageResult<T>`
作为统一返回模型，属性采用 PascalCase。`EU.Core.Api.Agent` 当前存在以下不同形式：

- 成功响应直接返回 DTO、数组或匿名对象；
- 创建接口直接返回 DTO，并使用 HTTP 201；
- 业务失败和未处理异常返回 RFC 7807 `ProblemDetails`；
- 分页或列表接口没有统一的分页及数量字段；
- Agent 自带前端直接消费当前裸数据和 `errorCode`、`traceId` 字段。

两个 API 宿主的普通 JSON 返回结构不一致，增加了前端请求封装、错误处理和后续接入成本。

## 需求内容

### 1. 统一目标

`EU.Core.Api.Agent` 的普通 JSON 接口统一使用 `EU.Core.Api` 已有的公共响应模型：

- 普通结果：`ServiceResult<T>`；
- 分页结果：`ServicePageResult<T>`；
- JSON 属性命名采用 PascalCase，与 `EU.Core.Api` 的 `DefaultContractResolver` 行为一致；
- `Status` 与实际 HTTP 状态码保持一致；
- `Success` 明确表示本次操作是否成功；
- `Message` 提供用户可理解的结果说明；
- `MessageDev` 只允许在开发环境返回诊断信息，生产环境不得暴露堆栈、连接信息或凭据；
- `Count` 用于非分页集合的结果数量，单对象或无数据结果为 `0`；
- `Data` 保存业务数据，失败时可保存结构化错误元数据。

普通成功响应示例：

```json
{
  "Status": 200,
  "Success": true,
  "Message": "查询成功！",
  "MessageDev": null,
  "Count": 1,
  "Data": {}
}
```

业务失败响应示例：

```json
{
  "Status": 409,
  "Success": false,
  "Message": "Agent 操作无法完成。",
  "MessageDev": null,
  "Count": 0,
  "Data": {
    "ErrorCode": "AGENT_CODE_CONFLICT",
    "TraceId": "请求追踪标识"
  }
}
```

分页响应示例：

```json
{
  "Status": 200,
  "Success": true,
  "Message": "查询成功！",
  "Page": 1,
  "PageCount": 1,
  "TotalCount": 1,
  "PageSize": 20,
  "Data": []
}
```

### 2. 错误处理

- 参数校验、业务冲突、未找到、未授权、限流、请求体过大和未处理异常均返回统一响应模型；
- 原 Agent API 的稳定错误码不得丢失，统一放入 `Data.ErrorCode`；
- 请求追踪标识不得丢失，统一放入 `Data.TraceId`，并继续保留现有关联 ID 响应头；
- 不以 HTTP 200 掩盖失败：HTTP 状态码与响应体 `Status` 一致；
- 生产环境不得在 `Message`、`MessageDev` 或 `Data` 中暴露异常堆栈及敏感配置。

### 3. 改造范围

- `EU.Core.Api.Agent/Controllers/**` 中的普通 JSON 接口；
- 参数校验、授权、限流、幂等、审计失败和全局异常处理中间件；
- Swagger/OpenAPI 响应声明；
- `EU.Core.Api.Agent/wwwroot/js/**` 中所有 Agent API 消费者；
- 受影响的单元测试、集成测试和接口契约测试。

改造前应形成接口清单，逐项记录原响应类型、目标响应类型、HTTP 状态码和前端调用方，避免遗漏聊天、Skill、MCP、知识库、编排及质量评估接口。

### 4. 不在统一包装范围内

以下接口保持所属协议，不使用 `ServiceResult<T>` 包装其成功响应，但失败响应仍应遵循可被统一客户端识别的错误约定：

- SSE/流式聊天响应；
- 文件内容读取、文件下载和 Agent 包导出；multipart/form-data 上传请求格式保持不变，但其 JSON 结果仍需统一包装；
- 健康检查、指标及其他基础设施协议端点；
- HTTP 204 响应。若业务需要返回统一消息，应改为 HTTP 200 和 `ServiceResult<T>`，不得在 204 中写响应体。

### 5. 兼容与上线要求

- 此变更属于破坏性 API 契约变更，后端与 Agent 自带前端必须在同一版本同步发布；
- 前端统一请求函数负责读取 `Data`，并从 `Data.ErrorCode`、`Data.TraceId` 构造现有错误对象；
- 不保留“部分接口裸数据、部分接口统一包装”的长期双轨状态；
- 如存在仓库外调用方，上线前必须通知其调整属性大小写和数据解包逻辑；
- 回滚时后端和前端必须按同一提交或同一发布版本一起回滚。

## 验收标准

- [ ] 除明确列出的特殊协议接口外，Agent API 所有 JSON 响应均使用 `ServiceResult<T>` 或 `ServicePageResult<T>`。
- [ ] 返回属性及 `Data` 内 DTO 属性统一采用 PascalCase，与 `EU.Core.Api` 保持一致。
- [ ] 成功、创建、参数错误、未授权、未找到、业务冲突、限流和未处理异常均有契约测试。
- [ ] HTTP 状态码与响应体 `Status` 一致，`Success` 值正确。
- [ ] 原有稳定错误码和追踪标识可由前端继续获取。
- [ ] Agent 管理页面的 Agent、Skill、MCP、知识库、编排、聊天和质量评估功能能够正常加载及操作。
- [ ] 文件导入导出、PDF 上传、SSE 聊天和运行事件流保持原协议并通过回归测试。
- [ ] Swagger/OpenAPI 能展示统一后的成功与失败响应模型。
- [ ] 后端构建和针对性测试通过，Agent 前端脚本测试或关键页面回归通过。

## 补充资料

- 相关页面或接口：`/api/agents`、`/api/skills`、`/api/mcp/**`、`/api/knowledge-bases`、`/api/orchestrations`、`/api/chat/**`、`/api/evaluation-*`
- 相关文件：`EU.Core.Model/ServiceResult.cs`、`EU.Core.Api/Filter/GlobalActionFilter.cs`、`EU.Core.Api.Agent/Controllers/ApiProblemResults.cs`、`EU.Core.Api.Agent/Errors/ProblemDetailsMiddleware.cs`、`EU.Core.Api.Agent/wwwroot/js/http.js`
- 截图或附件：无
- 依赖或前置条件：完成 Agent API 响应清单及前端调用方清单
- 其他说明：优先复用公共响应模型，不在 Agent 宿主内复制另一套同名模型。

## 处理记录

| 日期 | 操作人 | 状态变化 | 说明 |
|---|---|---|---|
| 2026-08-17 | sah | 新建 → 待分析 | 提出统一 Agent API 返回结构需求 |
| 2026-08-17 | Codex | 待分析 → 待开发 | 补齐目标契约、范围、例外、兼容方案和验收标准 |

## 实施与验证

- 实施说明：待开发
- 验证结果：待验证
- 关联提交：
- 遗留风险：响应包装和属性大小写变化会影响所有现有 Agent API 调用方，必须同步修改并发布前端消费者。
