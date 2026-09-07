# Agent 统一认证

`EU.Core.Api`、`EU.Core.MCP.Api` 与 `EU.Core.Api.Agent` 共用
`EU.Core.Extensions` 中的认证提供方选择和 JWT Bearer 验证实现。Agent 不再维护独立的
`Authority`、`Audience`、签名验证代码或开发免认证方案。

## 配置要求

### BusinessQuery 复用项目 JWT 配置

Agent 和 MCP 的查询上下文签名都从已注册的 `Bearer` 认证方案读取本地 JWT 对称签名配置，
即现有 `Audience:Secret`（或 `Audience:SecretFile`）的生效值。两端必须一致且至少为 256 位。
不新增第二套密钥，不读取查询签名环境变量，不加载 `appsettings.Local.json`，所有环境行为一致。
无需修改系统环境变量，也无需复制真实凭据到另一个配置节点。

内部仍保留短时有效的查询上下文令牌：使用固定用途字符串从现有 JWT 密钥派生签名材料，
仅保存在内存中，用完清零，不直接使用登录 JWT 的签名材料签署另一种协议。
用户、租户来自 Agent 已认证的调用身份；MCP 继续验证签名、租户、目录和工具哈希、时效及防重放。
这不是把未签名元数据当作可信身份，也不是取消验签。HTTP 仍接受登录 JWT 或专用服务令牌。

旧 `DevelopmentSigningKey`、`DevelopmentExecutionContextSigningKey` 和 MCP 的当前／上一密钥别名字段
仅为配置兼容保留，不再生效；Agent 的 `SigningKeyAlias` 为兼容策略契约保留，默认 `alias:project-jwt`，不再解析外部密钥。
现有历史凭据未被轮换或清理。此前新增的本地密钥副本和加载逻辑已移除；旧构建目录里残留文件也不会被读取。

上线须同时重新编译、重启 Agent 和 MCP；旧独立密钥签发的上下文不再接受，不能混合部署。
回滚必须成对恢复两端解析器和旧配置。JWT 密钥轮换也须协调两端，进行中的短期上下文会失效。
仅支持宿主持有本地 JWT 对称签名配置的模式；仅有外部 IdentityServer/Authing 公钥时会拒绝查询，不能据此绕过认证。

### MCP 工具同步的 Token 传递

`POST /api/mcp/servers/{id}/sync` 的请求体仍只需 `expectedLogicalRevision`。
HTTP（StreamableHttp/SSE）工具发现优先使用 `CredentialAlias` 对应的服务凭据；
别名为空时，转发当前已认证请求的 `Authorization: Bearer ...`。别名解析失败不回退到登录 Token。
Token 仅作为本次方法调用参数传递，不加入命令 DTO、不持久化到 MCP 定义；stdio 不注入该 Token。
目标地址仍受现有主机、端口和 HTTP/HTTPS 白名单约束，禁止自动重定向。
只有可信的 MCP 目标才应配置为登录 Token 接收方。

BusinessQuery 接受专用服务令牌或由 MCP 宿主现有认证方案验证通过的项目登录 JWT。
配置凭据别名时继续使用服务令牌；别名为空时可使用转发的登录 JWT。
JWT 沿用项目的签名、发行方、受众和有效期验证配置，不信任仅解码得到的声明；
两个认证分支均不通过时仍记录拒绝审计并返回 401，审计失败返回 503。
同步发现不需要业务查询的签名执行上下文，但实际 `tools/call` 仍需要有效签名上下文，
其租户、目录/工具哈希、防重放、配额和审计校验保持不变。
服务令牌配置及 readiness 检查仍保留，JWT 支持不意味着可以删除原配置。
部署时需重新编译并重启 Agent 和 MCP 宿主；回滚双认证只需恢复 BusinessQuery 专用中间件的服务令牌校验。
本次未修改 HTTP 请求/响应结构；内部服务和发现接口新增可选 Token 参数，外部自定义实现需同步签名并重新编译。

三个宿主必须使用同一组认证提供方配置：

- 本地 JWT：`Audience:Secret`（或 `Audience:SecretFile`）、`Audience:Issuer`、
  `Audience:Audience` 必须一致。
- IdentityServer4：`Startup:IdentityServer4` 配置必须一致。
- Authing：`Startup:Authing` 配置必须一致。

沿用项目已有 appsettings 认证配置及现有安全配置来源，不要求新增环境变量；示例和文档不得包含真实密钥。

所有环境（包括 `Development`）访问 Agent API 时都必须携带由同一认证提供方签发的
Token。前端可以直接复用登录 `EU.Core.Api` 后取得的 Token，无需单独登录 Agent API。

## 声明与授权

Agent 与 `EU.Core.Api` 共用 `IUser`/`AspNetUser`（即 `App.User` 的请求作用域实现），
不再定义或解析 Agent 专用声明。用户标识来自 `jti`，租户标识来自
`TenantId`；具体解析行为以 `AspNetUser` 为唯一事实源。

当前 Agent 的回退策略和所有命名策略只验证 Token 是否有效，不要求 Agent
的 `permission` 声明。调用上下文为已认证身份提供 `business.project.query` 能力标记，仅允许发起
项目 BusinessQuery 请求。当前按用户要求，MCP 的模块、角色和公司范围数据库校验已在所有环境暂时停用，
原实现以注释保留；可调用工具的用户可查询目录内全部公司数据，仍受状态、字段和聚合规则限制。
租户匹配、签名、防重放与审计继续启用。
目录未绑定项目模块时不得使用该标记。其他细粒度 Agent 权限仍需独立实现。
详见 [通用 BusinessQuery 项目适配](BusinessQuery项目适配.md)。
