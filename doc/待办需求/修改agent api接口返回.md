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
- 成功响应的 `Status` 固定为 `200`；Agent 业务失败使用 `600000–699999` 独立号段，不使用 4 或 5 开头的常用 HTTP 状态码；
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
  "Status": 610002,
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
- 响应体 `Status` 是业务状态码，HTTP 响应状态码负责传输语义；两者由同一错误映射同时生成，但数值不要求相同；
- 生产环境不得在 `Message`、`MessageDev` 或 `Data` 中暴露异常堆栈及敏感配置。

### 3. ErrorCode 与 Status 对照

新增公共的 Agent API 错误响应映射器，作为 `ErrorCode → Status + HTTP Status` 的唯一事实源。
Controller、参数校验、授权处理器及中间件不得继续各自维护重复的 `switch`。

`ErrorCode` 继续作为稳定的机器可读错误标识返回在 `Data.ErrorCode`；`Status` 是供
EU.Core 客户端统一判断的数字业务状态码。HTTP 状态码仍按 HTTP 语义返回，例如未找到为
HTTP 404，但响应体可以是 `Status=610001`。

#### Status 号段

| Status 范围 | 所属类别 | ErrorCode 示例 |
|---:|---|---|
| 200 | 所有成功响应 | 成功响应不返回 `ErrorCode`，即使实际 HTTP 状态码为 201，响应体仍为 `Status=200` |
| 600000–609999 | Agent API 通用请求、认证和限流 | `REQUEST_INVALID`、`REQUEST_BODY_TOO_LARGE`、`AUTHENTICATION_REQUIRED`、`AUTHORIZATION_DENIED`、`AGENT_RATE_LIMIT_EXCEEDED` |
| 610000–619999 | Agent 定义和主 Agent | `AGENT_NOT_FOUND`、`AGENT_CODE_CONFLICT`、`AGENT_ROW_VERSION_CONFLICT`、`MAIN_AGENT_NOT_CONFIGURED` |
| 620000–629999 | Skill | `SKILL_NOT_FOUND`、`SKILL_CODE_INVALID`、`SKILL_DRAFT_REVISION_CONFLICT`、`SKILL_ARCHIVE_BLOCKED` |
| 630000–639999 | MCP、工具和审批 | `MCP_SERVER_NOT_FOUND`、`MCP_DISCOVERY_FAILED`、`MCP_DISABLE_BLOCKED`、`TOOL_APPROVAL_INVALID_STATE` |
| 640000–649999 | 知识库 | `KNOWLEDGE_BASE_NOT_FOUND`、`KNOWLEDGE_DOCUMENT_INVALID`、`KNOWLEDGE_SERVICE_UNAVAILABLE` |
| 650000–659999 | 编排 | `ORCHESTRATION_NOT_FOUND`、`ORCHESTRATION_ROW_VERSION_CONFLICT`、`ORCHESTRATION_RUN_INPUT_INVALID` |
| 660000–669999 | Agent 运行、聊天和统一入口 | `AGENT_RUN_INPUT_INVALID`、`MODEL_INVOCATION_FAILED`、`MCP_TOOL_CALL_FAILED`、`UNIFIED_ENTRY_INVALID_STATE` |
| 670000–679999 | 质量评估和模型评审 | `EVALUATION_SUITE_NOT_FOUND`、`EVALUATION_BATCH_ASSERTION_FAILED`、`MODEL_JUDGE_EXECUTION_FAILED` |
| 680000–689999 | 审计、幂等、准入及宿主依赖 | `AGENT_AUDIT_UNAVAILABLE`、幂等冲突、昂贵请求准入失败 |
| 690000–699999 | 未知错误和兜底异常 | `UNEXPECTED_ERROR`、尚未登记的 `ErrorCode` |

#### 完整固定对照

当前代码中的全部固定错误码及其业务 `Status`、默认 HTTP 状态码，统一维护在
[`Agent API ErrorCode 固定清单`](Agent%20API%20ErrorCode固定清单.md)。该文件是本需求的错误码
注册表，实施时必须整体落入集中映射器，不得只实现部分示例。

映射规则：

- 每个 `ErrorCode` 必须对应唯一的六位 `Status`，同一个 `Status` 不得分配给多个错误码；
- 同一个 `ErrorCode` 在不同 Controller 中必须得到相同的业务 `Status` 和 HTTP 状态码；
- 新增错误码时必须先更新固定清单，在所属号段顺序分配新 `Status`，不得复用已经废弃的号码；
- 映射缺失时响应体使用 `Status=699999`、HTTP 500，并记录包含原 `ErrorCode` 的告警日志；
- 不允许仅根据错误消息文本推断状态码；
- SSE 已开始、后台运行或编排节点内部产生的错误码无法改变已经发送的 HTTP 状态码，应保留在事件或运行详情的 `Data.ErrorCode` 中；流开始前发生的失败仍使用本表映射；
- 现有错误码如与上表分类冲突，实施前在接口清单中记录兼容结论，不直接重命名稳定错误码。

### 4. 改造范围

- `EU.Core.Api.Agent/Controllers/**` 中的普通 JSON 接口；
- 参数校验、授权、限流、幂等、审计失败和全局异常处理中间件；
- Swagger/OpenAPI 响应声明；
- `EU.Core.Api.Agent/wwwroot/js/**` 中所有 Agent API 消费者；
- 受影响的单元测试、集成测试和接口契约测试。

改造前应形成接口清单，逐项记录原响应类型、目标响应类型、HTTP 状态码和前端调用方，避免遗漏聊天、Skill、MCP、知识库、编排及质量评估接口。

### 5. 不在统一包装范围内

以下接口保持所属协议，不使用 `ServiceResult<T>` 包装其成功响应，但失败响应仍应遵循可被统一客户端识别的错误约定：

- SSE/流式聊天响应；
- 文件内容读取、文件下载和 Agent 包导出；multipart/form-data 上传请求格式保持不变，但其 JSON 结果仍需统一包装；
- 健康检查、指标及其他基础设施协议端点；
- HTTP 204 响应。若业务需要返回统一消息，应改为 HTTP 200 和 `ServiceResult<T>`，不得在 204 中写响应体。

### 6. 兼容与上线要求

- 此变更属于破坏性 API 契约变更，后端与 Agent 自带前端必须在同一版本同步发布；
- 前端统一请求函数负责读取 `Data`，并从 `Data.ErrorCode`、`Data.TraceId` 构造现有错误对象；
- 不保留“部分接口裸数据、部分接口统一包装”的长期双轨状态；
- 如存在仓库外调用方，上线前必须通知其调整属性大小写和数据解包逻辑；
- 回滚时后端和前端必须按同一提交或同一发布版本一起回滚。

## 验收标准

- [ ] 除明确列出的特殊协议接口外，Agent API 所有 JSON 响应均使用 `ServiceResult<T>` 或 `ServicePageResult<T>`。
- [ ] 返回属性及 `Data` 内 DTO 属性统一采用 PascalCase，与 `EU.Core.Api` 保持一致。
- [ ] 成功、创建、参数错误、未授权、未找到、业务冲突、限流和未处理异常均有契约测试。
- [ ] 成功响应体 `Status=200`；Agent 业务失败只使用 `600000–699999`，不使用 4 或 5 开头的业务状态码。
- [ ] HTTP 状态码保持标准 HTTP 语义，响应体 `Status` 按 ErrorCode 对照表返回，`Success` 值正确。
- [ ] 所有现有 `ErrorCode` 已登记到集中映射器，同一错误码在不同接口中返回相同的业务 `Status` 和 HTTP 状态码。
- [ ] 未登记错误码返回业务 `Status=699999`、HTTP 500，保留原错误码并产生告警日志。
- [ ] 原有稳定错误码和追踪标识可由前端继续获取。
- [ ] Agent 管理页面的 Agent、Skill、MCP、知识库、编排、聊天和质量评估功能能够正常加载及操作。
- [ ] 文件导入导出、PDF 上传、SSE 聊天和运行事件流保持原协议并通过回归测试。
- [ ] Swagger/OpenAPI 能展示统一后的成功与失败响应模型。
- [ ] 后端构建和针对性测试通过，Agent 前端脚本测试或关键页面回归通过。

## 补充资料

- 相关页面或接口：`/api/agents`、`/api/skills`、`/api/mcp/**`、`/api/knowledge-bases`、`/api/orchestrations`、`/api/chat/**`、`/api/evaluation-*`
- 相关文件：`EU.Core.Model/ServiceResult.cs`、`EU.Core.Api/Filter/GlobalActionFilter.cs`、`EU.Core.Api.Agent/Controllers/ApiProblemResults.cs`、`EU.Core.Api.Agent/Errors/ProblemDetailsMiddleware.cs`、`EU.Core.Api.Agent/wwwroot/js/http.js`
- 截图或附件：无
- 依赖或前置条件：完成 Agent API 响应清单及前端调用方清单；以 [`Agent API ErrorCode 固定清单`](Agent%20API%20ErrorCode固定清单.md) 为映射事实源
- 其他说明：优先复用公共响应模型，不在 Agent 宿主内复制另一套同名模型。

## 处理记录

| 日期 | 操作人 | 状态变化 | 说明 |
|---|---|---|---|
| 2026-08-17 | sah | 新建 → 待分析 | 提出统一 Agent API 返回结构需求 |
| 2026-08-17 | Codex | 待分析 → 待开发 | 补齐目标契约、范围、例外、兼容方案和验收标准 |
| 2026-08-17 | Codex | 待开发 | 补充 ErrorCode 与 Status 集中映射规则及对照表 |
| 2026-08-17 | Codex | 待开发 | Agent 业务 Status 调整为 600000–699999 独立号段，HTTP 状态码单独保留 |
| 2026-08-17 | Codex | 待开发 | 完成当前 Agent ErrorCode 全量盘点并建立固定注册表 |

## 实施与验证

- 实施说明：待开发
- 验证结果：待验证
- 关联提交：
- 遗留风险：响应包装和属性大小写变化会影响所有现有 Agent API 调用方，必须同步修改并发布前端消费者。
