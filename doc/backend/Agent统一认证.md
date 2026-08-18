# Agent 统一认证

`EU.Core.Api`、`EU.Core.MCP.Api` 与 `EU.Core.Api.Agent` 共用
`EU.Core.Extensions` 中的认证提供方选择和 JWT Bearer 验证实现。Agent 在关闭开发绕过后，
不再维护独立的 `Authority`、`Audience` 或签名验证代码。

## 配置要求

三个宿主必须使用同一组认证提供方配置：

- 本地 JWT：`Audience:Secret`（或 `Audience:SecretFile`）、`Audience:Issuer`、
  `Audience:Audience` 必须一致。
- IdentityServer4：`Startup:IdentityServer4` 配置必须一致。
- Authing：`Startup:Authing` 配置必须一致。

敏感值应由环境变量、密钥文件或部署平台注入，不要复制真实密钥到仓库。例如本地 JWT
可以通过 `Audience__Secret`、`Audience__Issuer`、`Audience__Audience` 注入。

生产或联调验证共用 Token 时，将 Agent 的
`AgentAuthentication:DevelopmentBypassEnabled` 设置为 `false`。开发绕过只允许在
`Development` 环境启用。

## 声明兼容与授权

Agent 在 Token 完成签名、签发方、受众和有效期验证后，才执行以下兼容转换：

1. 将 API 的用户标识声明转换为 Agent 的 `sub` 声明。
2. 仅当 API 租户声明精确匹配 `SharedTokenTenantId` 时，映射到 Agent 固定租户。
3. 用户和租户声明均通过检查后，授予 `SharedTokenPermissions` 中配置的 Agent 权限。

当前默认配置包含 `agent.admin`，因此所有通过同一认证提供方验证、且租户匹配的 API/MCP
用户都可以使用完整的 Agent 管理和运行能力，不限制为管理员角色。若后续需要区分只读、
对话、调试和管理权限，可缩小 `SharedTokenPermissions`，或改为外部身份提供方直接签发
`permission` 声明。

原生包含 `sub`、`tenant_id` 和 `permission` 的 Token 保持原声明，不会被兼容转换覆盖。
