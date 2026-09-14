# Agent 请求体与日志边界

本次分类：BACKEND-HOST。仅调整 EU.Core.Api.Agent 的中间件顺序，不修改公共日志组件或其他宿主。

在 [Program.cs](../../eu.core/EU.Core.Api.Agent/Program.cs) 中，API 请求按以下顺序处理：操作审计 → 限流 → ProblemDetailsMiddleware → RequestBodyLimitMiddleware → 请求响应正文日志 → 授权 → 幂等/容量控制 → Controller。

- 请求日志会 EnableBuffering、读取正文并回卷，因此必须位于 RequestBodyLimitMiddleware 之后。
- 已知 Content-Length 超限时，在日志读取之前拒绝；无长度请求由有界流在读取过程中拒绝。超限统一返回 HTTP 413、业务状态 600002、错误码 REQUEST_BODY_TOO_LARGE，不写入完整正文日志。
- 审计仍在异常处理外层，可记录拒绝结果；异常处理覆盖日志读取，正常请求仍能回卷供后续接口读取。
- 普通请求、Skill 请求、知识库 PDF 上传原有大小上限保持不变；SSE 与 multipart 的现有日志跳过规则不变。
- 此变更不等于完成日志脱敏，也不取消审计存储依赖。未调整身份认证和授权策略。

[离线测试](../../eu.core/Src/EU.Core.Tests/AgentRequestBodyLoggingOrderTests.cs) 模拟日志的读取/缓冲/回卷流程，不写真实日志、不连接数据库，覆盖已知长度、无长度、普通边界以及 Skill/PDF 路径限额。实际部署时重启 Agent 宿主后生效，无配置或数据库迁移。
