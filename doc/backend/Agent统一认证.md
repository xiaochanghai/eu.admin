# Agent 统一认证

`EU.Core.Api`、`EU.Core.MCP.Api` 与 `EU.Core.Api.Agent` 共用
`EU.Core.Extensions` 中的认证提供方选择和 JWT Bearer 验证实现。Agent 不再维护独立的
`Authority`、`Audience`、签名验证代码或开发免认证方案。

## 配置要求

三个宿主必须使用同一组认证提供方配置：

- 本地 JWT：`Audience:Secret`（或 `Audience:SecretFile`）、`Audience:Issuer`、
  `Audience:Audience` 必须一致。
- IdentityServer4：`Startup:IdentityServer4` 配置必须一致。
- Authing：`Startup:Authing` 配置必须一致。

敏感值应由环境变量、密钥文件或部署平台注入，不要复制真实密钥到仓库。例如本地 JWT
可以通过 `Audience__Secret`、`Audience__Issuer`、`Audience__Audience` 注入。

所有环境（包括 `Development`）访问 Agent API 时都必须携带由同一认证提供方签发的
Token。前端可以直接复用登录 `EU.Core.Api` 后取得的 Token，无需单独登录 Agent API。

## 声明与授权

Agent 与 `EU.Core.Api` 共用 `IUser`/`AspNetUser`（即 `App.User` 的请求作用域实现），
不再定义或解析 Agent 专用声明。用户标识来自 `jti`，租户标识来自
`TenantId`；具体解析行为以 `AspNetUser` 为唯一事实源。

当前 Agent 的回退策略和所有命名策略只验证 Token 是否有效，不要求 Agent
的 `permission` 声明。调用上下文中的权限集合保持为空；细粒度 Agent 权限将在后续单独实现。
