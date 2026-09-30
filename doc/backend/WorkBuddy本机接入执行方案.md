# WorkBuddy 本机接入执行方案

## 最新权限调整（2026-09-23，覆盖下文旧权限说明）

项目所有者确认：Supplier 查询和删除均不校验登录身份、用户 ID、租户、角色、模块、操作权限及公司范围。已移除 `GetSupplierCompaniesAsync` 和无用的 IUser 依赖。查询仍只返回有效、未删除供应商的最小业务字段；删除仍保留 ID/编号必填、GUID 格式、唯一匹配、取消及目标 ID 写入条件。

安全影响：任何可访问 `/Supplier/mcp` 的客户端都可能跨公司读取和物理删除供应商；不存在服务端身份/权限防线，不能暴露公网。此行为只按本次明确授权用于受控环境，未改变全局认证配置或其他业务模块。恢复权限必须重新接入身份、操作授权和公司范围，不能只修改工具描述。验证仅在隔离内存数据库进行，不删除真实业务数据。

## 当前生效方案（2026-09-20，取代下方历史方案）

### Supplier 删除安全补丁

2026-09-22 经项目所有者明确确认：`query_suppliers` 允许匿名查询当前业务库全部公司供应商，不再校验用户 ID、登录状态、租户、用户/角色、模块或公司授权。仍过滤停用和已删除数据，保留分页上限、参数校验、取消和最小字段返回。删除完整保留身份、租户、模块、Delete 操作及公司范围校验。安全影响：任何能够访问该 MCP 入口的客户端均可分页读取供应商编号、名称和税务字段，不应将此接口暴露公网；恢复权限时重新为查询接入身份、模块和公司范围校验。

- `delete_supplier` 保留现有路由、工具名和参数结构，但改为调用 `IBdSupplierServices.DeleteSupplierForMcpAsync`，不再在 MCP 适配器中直接查第一条并删除。
- 服务端要求有效 ID 或非空编号；两者同时提供时必须同时匹配，编号命中多条时拒绝。必须通过登录身份、默认租户、有效用户/角色、模块、公司范围及 `Delete` 操作授权；模块明确关闭删除时拒绝。
- 写入仍沿用旧工具的物理删除语义，写入条件再次限制公司范围和编号。不要用真实业务数据做删除测试；关联业务阻止删除、独立操作审计和审批不属于本次已完成能力。
- 此补丁只覆盖删除工具；其他旧工具并不因此获得同等安全保证。新增接口参数不变，但无身份/无权限和非法参数原先可能执行的调用现在会被拒绝；外部客户端须携带有效身份。回滚安全补丁会重新暴露误删风险，不建议回滚。

按项目所有者要求，保留现有 MCP 控制层和 `/Supplier/mcp` 路由，只增加真实数据工具：

`SupplierController → SupplierService.query_suppliers → IBdSupplierServices.QuerySuppliersAsync → BdSupplierServices.QuerySuppliersAsync → 现有 Repository / SqlSugar`。

- `query_suppliers` 使用现有 `McpTool` 特性参与工具发现；不新增 Controller 或另一套协议分发。公共 BaseService 兼容 `Tool(object arguments)` 和 `Tool(object arguments, CancellationToken)`，统一传递取消令牌；查询与删除在各自 Tool 中解析参数，SupplierService 不再重写 HandleToolCallAsync。工具名、HTTP 路由及 JSON Schema 不变，CancellationToken 不暴露为客户端参数。同步反射异常和异步异常保留原始类型，参数错误仍由协议边界映射，取消不会被包装为内部故障。
- 参数为 `SupplierNo`、`Keyword`、`PageIndex`、`PageSize`，同时兼容 camelCase 输入；返回分页数据及最小供应商字段。未知参数拒绝，不允许传入公司权限范围。
- 查询按上述确认开放匿名、全部公司范围；删除保留登录身份、默认租户、有效用户/角色、供应商模块及公司权限校验。其余旧工具未在本补丁中改动，不宣称整个端点为只读或已完成安全验收。
- 已撤掉本任务新增的 `SupplierQueryController`、`SupplierQueryBoundaryFilter`、`SupplierQueryExecutions` 及其专属测试/注册。独立审计、并发、滚动配额和主动取消通知功能随这套独立入口撤除，不再作为交付能力；业务查询测试保留并转为验证原入口的工具发现/服务分发链路。
- 不再使用 `/mcp/supplier-query/controller` 或 `SupplierMcp:LocalQueryEnabled`。WorkBuddy 的 `eu-supplier-local` 应改为 `http://127.0.0.1:8020/Supplier/mcp`，类型保持 `streamable-http`，通过 `headers.Authorization` 提供项目登录 JWT；凭据由用户本地填写，不写入本文。
- WorkBuddy 客户端配置尚未随本次代码调整自动修改，真实 JWT、真实数据查询及旧工具访问范围仍需验收。BusinessQuery 外部身份适配未完成，不在本次 Supplier 调整中放宽原签名校验。
- 本轮为 BACKEND-BUSINESS / API-CONTRACT / TESTS；不连接业务数据库，不执行数据迁移，不修改原 SupplierController。

## 以下为历史方案与测试记录（独立入口相关内容已废止）

更新日期：2026-09-19。状态：Supplier 查询及默认关闭的只读入口已部分实现；WorkBuddy 模拟端点调用已通过，真实业务联调未完成，不能作为正式接入交付。

### 实施进度（2026-09-19）

- 已补充 `SupplierQueryInput` / `SupplierQueryItem`，以及 `IBdSupplierServices` / `BdSupplierServices.QuerySuppliersAsync`。支持编号精确查询、全称/简称关键字、最多 100 条分页；使用固定字段投影及编号/ID 排序，不包含联系和银行字段。
- 已补充 `ISupplierService` / `SupplierService.QuerySuppliersAsync` 数据结果转换方法，保留原导航方法。未添加旧反射目录的 McpTool 特性，旧匿名 tools/list 不会发现该方法。
- 新增 [SupplierQueryController](../../eu.core/EU.Core.MCP.Api/Controllers/BD/SupplierQueryController.cs)，路由为 `POST /mcp/supplier-query/controller`。该入口单独声明 query_suppliers schema 并仅分发此工具，不调用旧工具分发器。`SupplierMcp:LocalQueryEnabled` 缺失/false 时关闭；本次未修改 appsettings、未启用入口。认证由宿主完成，本机来源、Host/Origin 与转发头由资源过滤器检查，请求体上限 16 KiB；Program 排除此路径的原始请求正文日志，避免在限制执行前缓存正文。
- 新查询要求认证用户、有效用户/角色/模块授权和非空公司授权；没有管理员全库旁路，始终过滤授权公司以及有效、未删除供应商，不回填 Redis。此规则只作用于新增方法，不改变旧查询或 BusinessQuery 的临时权限状态。
- 当前 `BdSupplier` 未声明租户列/租户库映射，新增方法仅允许现有默认租户 `0`，非默认租户拒绝而非回落主库。扩展多租户前需完成实际库映射，不宣称已支持多租户数据查询。
- 已增加 `SupplierQueryTests` 和 `SupplierQueryEndpointTests`，共 21 项定向离线用例通过，包含内存 SQLite 查询及控制器/过滤器测试，不读取宿主配置、不连接真实数据库。工具名称白名单、未知参数拒绝、错误脱敏和默认关闭已验证；这不等同完整 HTTP 管道或实际 JWT 验签测试。
- 补充 `SupplierQueryHttpTests`：隔离 Kestrel 回环宿主、随机测试签名密钥及业务替身，验证 MVC 路由发现、JWT 签名/有效期/受众、入口关闭、非法 Origin 及 16 KiB 请求体限制。测试发现并修正了原资源过滤方法被误识别为 Action、且资源过滤器未实际挂载的问题；现由独立 `SupplierQueryBoundaryFilter` 通过 `TypeFilter` 显式挂载。不读取生产配置，也不代表生产 JWT 配置和全局中间件已完成联调。
- 当前入口支持 initialize、ping、tools/list、tools/call 和 `notifications/cancelled`。新增宿主单例 `SupplierQueryExecutions` 保存活动调用，取消按已认证 JWT 的用户 jti、TenantId、Bearer 凭据 SHA-256 指纹和请求 ID 匹配；不保存或记录原始 Token。刷新凭据后不能取消旧凭据的活动请求，由原调用的 20 秒预算或宿主停止结束。同一凭据被多个客户端共享时属于同一取消域，当前没有独立 MCP 会话隔离，不能宣传为客户端会话级隔离。
- 活动请求 ID 仅接受 1–128 字符字符串或 Int64 整数，字符串和数字 ID 分开匹配；重复活动 ID 返回 `SUPPLIER_QUERY_DUPLICATE_ID`。全局最多 8 个、同一用户/租户最多 2 个活动调用，超限返回工具错误 `SUPPLIER_QUERY_BUSY`，不排队。取消请求不会提前释放槽位，执行退出并清理后才释放；凭据刷新不能绕开身份并发限制。
- 补充滚动一分钟准入限额：全局 120 次、同一用户/租户 20 次，超限返回 `SUPPLIER_QUERY_RATE_LIMITED`。准入后的失败、取消及审计失败均不退还额度；通过单调时钟清理过期记录，队列最多 120 项，不保存原始 Token。限额为单实例内存保护，重启清空，不是持久化业务配额或跨副本协调。
- 执行令牌结合主动取消、宿主停止与 20 秒预算，不绑定 HTTP 断开；数据库驱动能否及时响应取消尚未实测。查询准入后复用 `IAgAgentOperationAuditServices` / `AgAgentOperationAuditServices`，向现有操作审计表写入 `Started`，再以同一 ID 更新为 `Succeeded`、`Failed` 或 `Cancelled`，策略为 `supplier.mcp.query`。不新增 Store、表或密钥；现有 Autofac 扫描负责注册，单例活动表不持有数据库服务。
- 审计不记录查询正文、结果或 Token，使用独立于请求/执行取消的令牌。开始审计失败不执行查询，终态审计失败不返回数据，均返回 `SUPPLIER_AUDIT_UNAVAILABLE`；不自动重跑查询。20 秒预算不覆盖审计 I/O，现有审计服务并未将取消令牌传递到所有数据库调用；真实数据库命令超时仍需验收。进程被强杀或终态写入失败可留下 `Started`，自动恢复/告警及准入前拒绝审计尚未补齐，不宣称所有路径均有持久化终态。
- 本轮验证增加审计成功/失败/取消、开始与终态写入失败拒绝、内存 SQLite 真实审计服务更新、身份/全局滚动限额及窗口过期测试；具体执行结果见本轮交付。React 类型检查通过；依赖漏洞及既有编译警告仍存在。未运行全解决方案构建、生产宿主启动或真实数据库联调。
- **启用前仍需完成**：真实 WorkBuddy 协议验证（含主动取消）、审计故障/重启恢复边界、生产宿主认证/中间件联调，以及 4.7 的旧入口处置或隔离环境确认。BusinessQuery 外部身份适配尚未实现，不能因新增只读路由就宣布两个连接已接入。
- 修改的业务接口/服务带生成标记，后续重新生成时需保留这些手写扩展。计数与分页为两次读取，不保证并发数据变动时的事务一致快照。

## 1. 目标与范围

用户已安装并登录本机 WorkBuddy。首批同时接入 Supplier 与 BusinessQuery：它们是同一个 `eu.core/EU.Core.MCP.Api` 宿主中的两个 MCP 端点，不是两个独立启动的服务。WorkBuddy 分别配置两个连接。Supplier 在现有 Service 中补充返回实际数据的查询方法，BusinessQuery 负责通用汇总报表；不新增生产宿主、不复制业务服务、不建设统一聚合入口。

- 首期仅开放核实过的只读/导航能力，不开放供应商或订单新增、导入、修改、审核、删除工具；导航不等于数据查询成功。
- 不开放公网，不新增第二套签名密钥，不在客户端保存项目 JWT 签名密钥。
- 保留现有 React Agent 调用链路；WorkBuddy 登录不等于 EU 项目登录或业务授权。
- 本文为 DOCS；实施涉及 BACKEND-HOST / API-CONTRACT，提取共享执行边界时再评估 BACKEND-PLATFORM。若改动持久化模型或 SQL，再追加 DATABASE。
- 不自动恢复或调整此前暂停的模块、公司权限；正式多人使用必须先取得恢复授权并完成权限验收。

### 核心要求与补充约定

- **核心要求**：同一 MCP 宿主中的 Supplier、BusinessQuery 都接入本机 WorkBuddy；Supplier 直接在现有 Service 链路新增查询方法，返回真实供应商数据，保留原 React 导航行为，不新增重复业务服务。
- **首版建议默认值**：采用下文的 `query_suppliers` 工具名、编号/名称筛选、分页及最小返回字段作为实施起点。这些是本计划补充的设计，不是既有接口事实；实施时核对相邻契约和命名，确需调整则同步本文与测试，不扩大为任意查询。
- **工具选择建议**：Supplier 负责普通供应商列表，BusinessQuery 负责目录支持的汇总报表。该分工用于帮助模型选择，不删除 BusinessQuery 已有 supplier 能力，也不限制原消费者继续使用。
- **范围与授权**：安全检查是接入验收要求，不意味着已授权重构、禁用原接口、恢复公司权限或修改共享数据。发现需要这些操作时，说明具体影响并取得确认；确认前可继续不触及旧接口的实现和离线测试，但不得跳过真实接入安全门禁。
- **交付优先级**：先完成“现有 Services 补查询方法 → Supplier MCP 暴露受控工具 → WorkBuddy 获取真实数据”，并完成 BusinessQuery 的必要适配。新增 WorkBuddy 页面导航与模板适配是可选项，不作为核心查询交付的前置条件；原 React 已有能力仍需回归。

## 2. 已确认现状

### 同一宿主的两个端点

| 端点 | 当前事实 | 接入决策 |
|---|---|---|
| Supplier：`POST /Supplier/mcp` | Controller 标记 `AllowAnonymous`，不能推断已强制项目 JWT；工具包含导航、模板和删除能力 | 先确认全链路鉴权、服务端工具白名单及返回兼容性；符合要求才直接复用，否则在同一宿主作最小适配 |
| BusinessQuery：`POST /mcp/business-query/controller` | 传输认证之外还要求签名执行上下文 | 保留旧 Agent 入口，在同一宿主适配外部客户端可信身份；不放宽原签名规则 |

同一进程不代表两个端点具有相同认证与授权。当前 BusinessQuery 专用认证中间件只匹配 `/mcp/business-query`，不能据此认定 Supplier 受到同样保护。历史本机基地址为 `http://localhost:8020`，实施前核实实际监听地址。

### BusinessQuery 细节

| 项目 | 当前情况 |
|---|---|
| MCP 入口 | `POST /mcp/business-query/controller`，历史本机地址为 `http://localhost:8020`；执行前核实实际监听端口 |
| 协议入口 | 已处理 initialize、notifications/initialized、tools/list、tools/call；尚未验证 WorkBuddy 客户端兼容性 |
| 传输认证 | 接受宿主验证通过的项目登录 JWT，或已有专用服务令牌 |
| 调用认证 | tools/call 还需要 `_meta["eu.core.agent/executionContext"]`，仅有 Bearer 不足以执行查询 |
| 执行上下文 | 签名绑定用户、租户、运行、工具版本、目录和 schema，包含短有效期及防重放机制 |
| 密钥 | 上下文签名使用项目 JWT 配置派生密钥；不应将其交给 WorkBuddy |
| 查询目录 | 共用 `project.json`，不为 WorkBuddy 复制销售目录或创建任意 SQL 工具 |
| 权限现状 | 模块及公司权限存在 `TEMP-PROJECT-AUTH` 临时旁路；当前不能宣称公司级数据隔离有效 |
| 运行记录 | 业务表只读不等于全链路无写入；审计、配额、防重放及可能的执行记录会写入基础设施 |

事实源：

- [Supplier Controller](../../eu.core/EU.Core.MCP.Api/Controllers/BD/SupplierController.cs)
- [Supplier 工具实现](../../eu.core/EU.Core.MCP.Api/Services/BD/SupplierService.cs)
- [供应商业务接口](../../eu.core/EU.Core.IServices/BD/IBdSupplierServices.cs)
- [供应商业务服务](../../eu.core/EU.Core.Services/BD/BdSupplierServices.cs)
- [MCP Controller](../../eu.core/EU.Core.MCP.Api/Controllers/BusinessQueryController.cs)
- [传输认证](../../eu.core/EU.Core.MCP.Api/Services/BusinessQuery/Security/BusinessQueryAuthentication.cs)
- [执行上下文校验](../../eu.core/EU.Core.MCP.Api/Services/BusinessQuery/Security/BusinessQueryExecutionContextVerifier.cs)
- [查询服务](../../eu.core/EU.Core.MCP.Api/Services/BusinessQuery/BusinessQueryService.cs)
- [项目身份及权限映射](../../eu.core/EU.Core.MCP.Api/Services/BusinessQuery/Security/BusinessProjectCallerResolver.cs)
- [统一目录](../../eu.core/EU.Core.MCP.Api/BusinessQuery/catalog/project.json)
- [项目适配说明](BusinessQuery项目适配.md)

## 3. 实施前确认

- [ ] 确认 WorkBuddy 版本及自定义 MCP 配置入口，记录它是否支持本地 HTTP Streamable HTTP；不能仅依据支持远程 HTTPS 就认定本地 HTTP 可用。
- [ ] 确认 MCP 实际运行地址及监听范围。拟新增入口仅用于本机试验，服务端校验回环来源；浏览器 CORS 不是网络隔离。
- [ ] 确认测试 EU 用户、租户与允许查询的数据范围，不将管理员 Token 作为默认方案。
- [ ] 取得业务数据交给 WorkBuddy 及其模型处理的许可。本机 MCP 并不保证查询结果只留在本机；未经确认先使用脱敏测试数据。
- [ ] 确认测试对审计、配额、防重放等记录的写入授权；没有授权时只做离线测试。
- [ ] 记录 React Agent 基线：供应商页面导航及模板、客户/币别、年月/币别、空结果；业务查询结果均能正常展示与恢复历史。

## 4. 最小改造设计（待实现）

### 4.1 一个宿主、两个连接，按端点决定适配

WorkBuddy 配置 `eu-supplier-local` 和 `eu-business-query-local` 两个连接，指向同一宿主中各自验收通过的端点。Supplier 不预设新增路由，先检查是否能安全复用；BusinessQuery 建议增加独立、默认关闭的外部客户端入口，最终路由在实施时确定。不要在旧入口增加“没有签名就自动放行”的降级分支。

调用链：

```text
一个 EU.Core.MCP.Api 宿主
├─ Supplier 端点 ← WorkBuddy 的 Supplier 连接
│  └─ 已验收的认证/工具白名单 → 现有供应商能力
├─ BusinessQuery 外部入口（待适配）← WorkBuddy 的 BusinessQuery 连接
│  └─ 项目 JWT / 可信业务身份 → 现有目录、策略、查询与审计
└─ 原 BusinessQuery 签名入口 ← React Agent（保持兼容）
```

以下为 BusinessQuery 适配要求；Supplier 的独立门禁见 4.6：

1. 外部入口只接受可定位项目用户的认证方式。首版本机试验复用项目登录 JWT；不能把无用户绑定的专用服务令牌直接映射成管理员。
2. 用户、租户及业务能力从服务端验证后的身份和授权结果获得；模型参数不能提供或覆盖这些信息。
3. 提取最小必要的可信执行边界，复用现有查询逻辑，不另建 Store、另一套目录或第二次模型调用。
4. 共享查询核心接收服务端构造的可信业务身份（用户、租户、能力、调用来源、关联标识），并绑定当前生效的目录/schema。旧入口验证签名后转换为该身份，新入口验证项目 JWT 并完成服务端授权解析后转换；核心不按“缺少签名”决定信任外部请求。
5. 原 AgentRunId/ToolVersionId 保留在旧入口的校验和追溯链路；外部调用使用独立请求关联标识，不生成虚假 Agent 运行或工具发布记录。实施前核实回执、审计及其消费者，优先复用现有 queryId 和日志关联能力，不预设新增表。确需持久化字段时，先明确兼容、迁移及回滚方案再实施，不以随机 GUID 或放宽旧校验替代。
6. 旧签名入口继续校验签名、有效期、目录绑定与重放。新入口根据自己的身份及调用模型实施限流、配额、取消和终态审计，不能声称直接继承了旧入口的全部保障。
7. 外部入口开关只能控制该入口，关闭时不影响 React Agent。新增组件生命周期按其实际职责注册，不改变已有协调器单例。
8. 保留结果、presentation 与 receipt；外部客户端不得用模型生成金额替代查询结果。不修改现有 `business-query-result` 实时与历史恢复契约。

### 4.2 首版认证和凭据维护

- 在 EU 项目正常登录获取有效 JWT，经 WorkBuddy 的凭据设置提供；不得复制到仓库、执行文档、Skill、聊天记录或截图。
- JWT 到期应明确提示重新登录/更新凭据；首版不伪称具备自动刷新或单点登录。
- 不将 JWT 签名密钥、ModelConfig 加密主密钥或数据库连接串传给 WorkBuddy。
- 正式多人部署再单独设计 OAuth/凭据生命周期，不在本机验证中顺手引入一整套认证平台。

### 4.3 模块和公司权限门禁

当前临时旁路意味着登录成功不等于已经限定公司范围。首版仅允许项目所有者批准的数据范围和测试身份，不增加匿名访问或公开入口。

正式开放前，经批准同时恢复调用者解析与查询策略中的 `TEMP-PROJECT-AUTH` 旁路，核对名称补全查询范围，并验证无模块权限、无公司范围、跨公司、跨租户均被拒绝。不能仅恢复一处或依靠提示词约束权限。

### 4.4 本机网络与来源限制

- 新入口仅允许回环直连；优先使用仅绑定 `127.0.0.1` / `::1` 的监听地址。若宿主已有其他网络消费者，不直接修改整个宿主监听配置，应明确新增入口的专属监听或访问限制并验证原入口不受影响。
- 校验 Host 与 Origin：Host 仅允许实际批准的本机地址和端口；Origin 存在时按明确允许列表验证，非法值返回 403。不使用通配符，也不直接反射请求头。桌面客户端可能不带 Origin，此时仍必须通过 JWT、Host 和直连来源检查。
- 首版本机入口不支持反向代理或隧道访问。不能因代理连接来自回环地址就认定请求来自本机，也不信任任意 `Forwarded` / `X-Forwarded-*` 头；检查宿主现有转发配置，确保无法借转发头改变身份或来源判断。确需代理时重新设计可信代理边界。
- 增加非回环来源、非法 Host/Origin、伪造转发头、无 Origin 且无 JWT 等负向测试。保留请求体大小限制、并发限制和凭据脱敏；CORS 不代替这些检查。

### 4.5 取消、超时与重复调用

- 外部入口遵守 MCP 的取消语义，不直接复制 React 聊天的断连取消规则。HTTP/SSE 断开不等同用户主动取消；服务端执行令牌由执行超时、宿主停止及有效的 MCP `notifications/cancelled` 控制，不只绑定 `RequestAborted`。
- 主动取消按协议请求 ID 关联活动调用，同时绑定已验证用户、租户及适用的会话边界，防止同名请求 ID 取消其他用户调用。阶段 0 必须确认客户端实际发送方式；若无法可靠关联，先解决协议实现，不宣称主动取消已支持。
- 首版不提供断线结果恢复或后台任务能力。断线后的已启动查询在有限时长内完成或超时并记录真实终态，不能无限存活；释放活动调用记录和执行作用域。取消与成功并发时只保留一个真实终态，终态审计不受传输断开或执行取消令牌影响。
- 客户端超时只是等待结束，不证明服务端未执行或已取消。分别设置 SQL 命令超时、整体执行超时和客户端等待时间，客户端等待预算应覆盖正常执行及序列化余量；下文 30000 毫秒仅为配置示意，不作为直接上线值。
- 首版不承诺跨请求幂等或结果缓存；每个接受的新查询独立计入配额并审计。禁止服务端静默重试查询；客户端重试可能再次执行，JSON-RPC 请求 ID 不是全局幂等键。活动会话内重复 ID 不得覆盖原调用状态。
- 不将旧 Agent 的“每个 run 最多一次”直接套到 WorkBuddy，也不伪造 run 来限流。外部入口使用按可信用户/租户的配额与并发限制；超时后的再次尝试不得绕过限制。

### 4.6 Supplier 接入门禁与工具分工

按当前方法实现盘点，不依据工具名称或描述推定行为：

| 工具 | 当前实际行为 | 首期处理 |
|---|---|---|
| `get_supplier` | 返回 `BD_SUPPLIER_MNG` / `module_list`，不是供应商记录 | 保留原 React 导航契约；不用于 WorkBuddy 数据查询，新查询工具见下文 |
| `get_supplier_import_template` | 查询模板并调用后台下载接口，返回附件信息 | 条件开放：先核对下载接口副作用、身份传递、URL 可达性和文件权限，再验证真实文件可用 |
| `create_supplier` | 返回编辑页面标识 | 首期不开放创建流程 |
| `update_supplier` | 查询供应商并返回编辑页面标识 | 首期不开放，不能因为未直接保存就认定可安全暴露 |
| `create_supplier_from_file` | 当前返回固定 ID 的编辑页面，与描述中的导入能力不一致 | 首期不开放，不声称已实现导入 |
| `delete_supplier` | 调用 `_supplierService.DeleteById`，属于真实业务写入 | 首期必须在服务端拒绝，不执行真实删除测试 |

- 分别核对 Supplier 的 initialize、通知响应、工具发现和调用格式。不能用 BusinessQuery 协议通过替代 Supplier 验收。
- WorkBuddy 接入前必须保证 Supplier 的认证和只读工具范围由服务端执行；仅配置客户端隐藏工具、模型提示词或 ReadOnly 标签不构成保护。tools/list 与 tools/call 使用一致白名单，直接猜测被禁工具名也必须拒绝。
- 由于当前入口声明 AllowAnonymous 且含写工具，未验证有效保护前，不把原 `/Supplier/mcp` 直接作为安全接入地址。若不能在原端点兼容实现访问策略，可在同一宿主增加受控视图；不得复制 Supplier 业务实现。旧匿名路径风险不能因新增受控视图就宣称已解决，正式开放前需另行处置并回归原消费者。
- Supplier 不自动继承 BusinessQuery 的配额、审计和公司范围规则，逐项核对并记录缺口；不为接入而关闭既有权限。模板下载可能生成文件，需单独确认副作用和清理方式。
- 供应商普通列表查询由 Supplier 新增数据工具提供；分组和税率等目录允许的聚合统计由 BusinessQuery 提供。保留 BusinessQuery 已有 supplier 实体以兼容原消费者，不删除目录能力。工具描述明确分工，避免把页面导航当作数据查询或重复调用两个端点。
- 两端点使用相同登录身份不意味着权限相同；混合使用时验证租户、用户和数据范围一致，不新增模型可指定的权限参数。

#### Supplier 真实数据查询扩展（待实现）

按项目所有者要求直接在现有 Service 链路补充方法，不另建 Store、查询宿主或聚合层：

```text
SupplierController（现有协议入口）
  → SupplierService.QuerySuppliersAsync（拟新增 query_suppliers 工具）
  → IBdSupplierServices.QuerySuppliersAsync（拟新增业务接口）
  → BdSupplierServices.QuerySuppliersAsync（查询规则与结果投影）
  → 现有 BaseServices / Repository / SqlSugar
```

- `SupplierService` 已注入 `IBdSupplierServices`，继续使用该依赖，不另加手动注册；MCP 方法仅转换参数与结果，不重复实现数据库查询。
- 首版增加一个分页列表工具 `query_suppliers`。建议支持供应商编号精确查询、名称/简称关键字、pageIndex、pageSize；默认每页 20，范围 1–100，pageIndex 为正整数，关键字限制长度。参数使用明确 DTO，不接受 SQL、表名、任意字段、自由排序或客户端授权范围。
- 固定按供应商编号、ID 排序，返回分页信息和明确的业务字段：ID、编号、全称、简称、税别、税率。先不暴露联系人、电话、地址、银行账户和其他整实体字段；空结果返回空数组，不用模块标识或假数据代替。
- 查询沿用有效/未删除过滤，并核对当前登录身份、模块、租户和公司范围的既有业务约束；不得因为普通 BaseServices 查询可用就假定已自动完成授权，也不得将 BusinessQuery 临时停用权限的行为复制到 Supplier。
- 输入校验、参数化查询、取消传播及受支持数据库兼容在现有分层中处理。错误继续按所属 MCP 协议返回，不伪装成空列表；数据库返回的名称等是数据，不是模型指令。
- 不修改原 `get_supplier` 的名称和返回类型，避免破坏 React 页面导航；WorkBuddy 受控工具列表以 `query_suppliers` 为核心，原导航工具可以不向它暴露。新增查询同样受 4.7 的安全门禁约束，不能因 Service 新增带工具特性的方法就直接匿名发布。
- 接口与服务文件带代码生成标记，实施时限制改动范围并记录再次生成的覆盖风险，不批量生成无关文件。新增工具是 API-CONTRACT；补充 MCP schema、业务查询及拒绝场景测试，检查原消费者兼容性。

### 4.7 Supplier 旧入口风险与最低验收条件

进入真实数据联调前，必须明确旧 `/Supplier/mcp` 的处置方式，不能将新受控入口的白名单当成整个宿主的只读保障：

1. **真实业务环境**：经所有者批准，限制旧写入口的访问并回归原消费者。验证匿名调用及 WorkBuddy 使用的身份不能通过旧路由绕过只读限制；不能只按 User-Agent、客户端自报名称或 Referer 区分权限。同一个 JWT 同时用于两端时，新增路由本身不能实现权限隔离。若需要让原消费者继续写入，必须先设计可验证的调用方/授权边界，不能默认已有隔离。
2. **旧入口暂不调整**：只能在隔离测试环境继续业务联调，使用合成或脱敏数据、隔离凭据和受控外部依赖，不连接共享业务库。两个端点仍由同一 MCP 宿主提供，不因此新增生产服务。该结果仅证明隔离环境接入可行，不代表真实环境安全验收完成。

如果两条均不满足，停留在阶段 0 的固定结果协议测试。本文不授权修改旧入口权限，也不授权在真实数据上试调用删除来验证防护；负向验证使用替身并断言写入方法未执行。

Supplier 业务接入以新增真实数据查询为必验项，导航和模板不再替代该验收：

- **数据查询可用（必须）**：WorkBuddy 能调用 `query_suppliers`，得到授权范围内的真实供应商字段和分页结果；编号查询、名称查询、空结果、非法分页和无权限场景均验证，不返回页面标识冒充数据。
- **页面导航（可选）**：若向 WorkBuddy 开放，返回经核实、不携带 Token 的链接，并由 EU 页面继续完成登录授权。不要求 WorkBuddy 内嵌 React 页面。
- **模板获取（可选）**：经批准开放后，测试用户可取得、打开正确模板，下载身份与权限有效，失败不会返回虚假成功。

若仅完成 initialize/tools/list、页面导航或模板下载，状态必须为“Supplier 数据查询接入未完成”。BusinessQuery 查询供应商成功不能替代 Supplier 的验收。真实查询能力与服务端安全门禁必须同时满足。

## 5. 执行步骤

执行顺序：盘点两端点 → 客户端最小验证 → Supplier 最小安全接入与 BusinessQuery 身份适配 → 分别验收 → 同时启用两个连接进行联合验收。

### 阶段 0：客户端最小验证（先于业务改造）

- [ ] 记录 WorkBuddy 实际版本、MCP 设置入口及凭据保存方式。
- [ ] 记录两个端点各自的工具清单、认证、返回结构和风险；明确最终使用原路由还是同宿主的受控适配路由。
- [ ] 使用隔离的本机协议测试端点，只返回固定合成数据，不连接数据库、不加载生产配置，不注册到正式业务目录。可用一次性测试凭据验证请求头传递，不使用真实用户 Token。
- [ ] 验证本地 HTTP、Authorization 请求头、initialize 协商、initialized 通知、tools/list 和 tools/call，以及 GET SSE 或 405、错误响应、超时和主动取消行为。
- [ ] 验证桌面客户端能分别保存并同时启用两个 MCP 连接，确认工具名冲突和选择行为；不把连接器市场单包配置格式等同于桌面全局配置格式。
- [ ] 验证 Origin/Host、请求 ID、会话和取消通知的实际形态；测试结果只保留脱敏协议信息。
- [ ] 若本地 HTTP 不支持，先验证本机 HTTPS 的证书信任；禁止关闭证书验证。需要代理或 stdio 桥接时先确认新范围，不先修改业务查询核心。
- [ ] 通过后记录选定协议方式和凭据配置步骤，关闭/移除临时连接，测试工具不得随生产入口发布。未通过则停在此阶段。

### 阶段 A：离线实现与验证

- [ ] 核对实际协议协商、响应格式、通知处理及 HTTP 状态码；验证 WorkBuddy 所需的 Streamable HTTP 行为，不仅测试普通 POST。
- [ ] Supplier：在现有 SupplierService / IBdSupplierServices / BdSupplierServices 补充 query_suppliers 数据查询，完成必要鉴权与只读白名单；保留原导航工具。路由仅在安全或协议兼容确有需要时适配。
- [ ] BusinessQuery：完成独立入口、身份转换、执行记录关联、开关和依赖注入。
- [ ] 验证 Supplier 未认证调用和被禁工具的直接调用均被拒绝；删除/导入等负向测试使用替身，断言业务写入方法未被调用。
- [ ] 覆盖无凭据、过期/伪造 JWT、错误租户、伪造用户/公司参数、审计失败、取消、配额超限和非法字段。
- [ ] 覆盖非法 Origin/Host、非回环来源和伪造转发头；验证反向代理不能把远程访问伪装成本机访问。
- [ ] 覆盖跨用户同名请求 ID、重复活动 ID、客户端断线、执行超时、主动取消及取消/完成竞争；检查唯一终态、作用域释放和无静默重试。
- [ ] 回归旧签名入口：合法调用成功，缺失/过期/重放上下文失败；目录/schema 不匹配失败。
- [ ] 构建受影响项目，运行经确认无外部连接的定向测试；修改前端契约时再运行前端类型检查。

参考构建命令（在仓库根目录执行）：

```powershell
dotnet build eu.core/EU.Core.MCP.Api/EU.Core.Api.MCP.csproj -c Release
git diff --check
git status --short
```

新增测试的具体过滤器在实施后补充；不得把未存在的测试名称当作可执行验收命令。

### 阶段 B：本机连接

- [ ] 先通过 4.7 的真实环境旧入口访问限制，或明确选择隔离数据联调；未满足时不得接入真实业务数据。
- [ ] 启动所需 EU 认证服务，只启动一份 MCP 宿主，核对两个端点的可用性；不分别启动 Supplier 和 BusinessQuery 进程。直接业务查询不应为了取结果再调用 Agent 模型。
- [ ] 在 WorkBuddy 分别新增 Supplier、BusinessQuery 两个连接。Supplier 使用安全验收通过的路由，BusinessQuery 使用适配后的外部入口，不能直接沿用原签名入口。
- [ ] 配置 Streamable HTTP、入口 URL、Authorization Bearer 凭据及超时，具体字段以安装版本界面为准。
- [ ] 依次验证 initialize → tools/list → tools/call，记录脱敏错误码、关联 ID 和耗时，不记录 Token。
- [ ] 若客户端拒绝本地 HTTP，暂停并选择本机 HTTPS；需要新增本地代理/stdio 桥接时另行确认，不为连通直接开放公网或关闭认证。

下面分别给出两个连接的配置形状，每份只描述一个连接，不代表安装版本的全局配置文件格式。两份 URL 使用同一个宿主基地址；占位符必须替换为验收过的路由，Token 引用方式需核对安装版本，不能直接复制使用。

Supplier 连接：

```json
{
  "mcpServers": {
    "eu-supplier-local": {
      "type": "streamableHttp",
      "url": "<VERIFIED_SUPPLIER_ENDPOINT>",
      "headers": {
        "Authorization": "Bearer ${EU_PROJECT_ACCESS_TOKEN}"
      },
      "timeout": 30000
    }
  }
}
```

BusinessQuery 连接：

```json
{
  "mcpServers": {
    "eu-business-query-local": {
      "type": "streamableHttp",
      "url": "<IMPLEMENTED_BUSINESS_QUERY_ENDPOINT>",
      "headers": {
        "Authorization": "Bearer ${EU_PROJECT_ACCESS_TOKEN}"
      },
      "timeout": 30000
    }
  }
}
```

该占位符不要求创建 `.env`、设置系统环境变量或修改 Agent 配置加载方式；使用 WorkBuddy 支持的凭据输入机制。首版不发布连接器市场包。

### 阶段 C：业务验收

| 用例 | 预期 |
|---|---|
| Supplier：查询供应商，按名称筛选或按编号精确查询 | query_suppliers 返回实际供应商字段，不返回 module_list，不调用 BusinessQuery 冒充 Supplier 成功 |
| Supplier：翻页、空结果、非法分页参数、无权访问 | 分页排序稳定、无敏感字段泄露；空数组与拒绝/错误明确区分，范围由服务端决定 |
| Supplier：打开供应商管理页面（可选） | 仅在决定向 WorkBuddy 开放时验收；返回经验证的导航信息，不能把模块代码说成数据查询结果或已打开页面 |
| Supplier：获取供应商导入模板（可选） | 仅在批准开放时验收；返回可下载且权限正确的真实模板，失败时不伪造链接 |
| Supplier：直接调用 delete_supplier 等被禁工具 | 服务端拒绝，业务写入方法未执行，不以 tools/list 隐藏作为唯一保护 |
| BusinessQuery：按税别分组统计供应商税率 | 返回目录支持的聚合结果；普通列表查询选择 Supplier 的 query_suppliers |
| 查询销售订单，按客户、币别汇总未税金额、税额和含税金额 | 与同条件 React 查询分组和金额一致，不跨币种合计 |
| 查询 2024 年销售订单，按月份、币别汇总三项金额，按月份升序 | 时间边界、时区和排序一致 |
| 查询销售订单，按年份、币别汇总含税金额 | 与相同过滤下月度结果按年/币别汇总一致 |
| 查询确定无订单的区间 | 明确无数据，不编造金额；WorkBuddy 展示可不同于 React 表格 |
| 历史订单的客户已软删除 | 沿用目录允许的历史名称补全；未匹配时回退 ID |
| 无有效明细或明细金额为空 | 按现有明细预汇总口径补 0，不读取主表空金额 |
| 请求未开放的采购实体、任意 SQL 或未知字段 | 拒绝，不猜测物理表或越过目录 |
| JWT 失效、取消、依赖异常、审计不可用 | 明确失败，不伪报成功；取消和失败具有相应终态记录 |
| 客户端超时或断线后重试 | 不假定原查询未执行；各次查询可追溯，配额有效，无重复 ID 状态覆盖 |
| 其他用户发送同名请求的取消通知 | 不得取消原用户的查询 |
| 非法 Origin/Host、非回环连接或伪造转发头 | 按本机入口边界拒绝，不能仅靠回环代理地址放行 |
| 外部入口关闭后再访问 | 拒绝外部调用；React 原签名入口保持正常 |
| 同时启用两个连接后交替询问供应商数据和销售报表 | 选用对应工具，不串身份、结果或错误；分别禁用一个连接后另一个仍可用。页面/模板仅在本次开放时追加联合用例 |

核对时固定同一用户、租户、目录版本、时间范围、币别、状态过滤和数据时点，不能用历史截图中的金额当作当前数据库断言。业务金额来自有效且未删除明细按订单预汇总；明细 JOIN 不得放大订单金额。

## 6. 回滚与完成标准

回滚先分别禁用 WorkBuddy 的两个连接，再关闭本次新增的入口/受控视图；必要时回退本次代码，不停止仍服务 React 的共同 MCP 宿主。若 Supplier 复用原路由，不为撤销 WorkBuddy 连接而关闭原消费者入口。移除客户端连接不是服务端凭据撤销或风险修复。不要关闭原签名校验、删除审计或修改业务数据以恢复可用性。真实 EU 凭据如泄露，应由所有者按现有流程撤销或更新。

完成标准：

- [ ] 同一 MCP 宿主下两个连接均完成各自验收及联合验收；Supplier 真实数据查询与 BusinessQuery 报表结果分别记录，不互相替代成功标准。
- [ ] Supplier 的 query_suppliers 通过真实数据、过滤、分页、空结果和权限验收；仅协议连通、导航或模板下载不算完成。
- [ ] 记录旧 Supplier 入口处置与验证结果；若只在隔离数据环境验证，必须明确标为“真实环境接入未验收”，不能用本机运行或新路由只读代替隔离证明。
- [ ] WorkBuddy 真实业务查询成功，结果与 React 同口径一致。
- [ ] 凭据失效、越权输入、取消及审计异常有明确结果。
- [ ] 原 React 实时结果与历史展示无回归。
- [ ] 外部入口可独立禁用，凭据未进入 Git 或日志。
- [ ] Supplier 未认证及禁用工具调用在服务端被拒绝；不存在仅靠客户端隐藏写工具的交付。
- [ ] 保存测试日期、客户端版本、代码版本、目录/schema 哈希、脱敏关联 ID、各阶段耗时和未通过项。
- [ ] 若模块/公司权限仍暂停，交付必须注明“受控本机试验”，不得标记为可正式多人使用。

## 7. 后续路线

第一阶段验收后再推进两条路线：

1. WorkBuddy：复用查询工具制作月报操作说明，后续通过同一目录接入采购等已授权业务，不增加任意 SQL 能力。
2. EU React Agent：补充报表快捷入口、保存条件、导出与结果核对；定时任务另行处理运行恢复、取消和审计，不默认启用现有后台执行器。

写操作、自动审批、多副本和公网接入均不属于本方案。

## 8. 外部参考

### 本机协议验证记录（2026-09-19）

- 客户端：WorkBuddy 5.5.6；通过桌面界面保存两个独立模拟连接 `eu-supplier-probe`、`eu-business-query-probe`，首次信任由用户手动完成。
- 两个连接指向同一临时回环 HTTP 测试进程的不同路径，不连接 EU 数据库，不使用真实 JWT，不启动业务宿主。
- 两者均完成 `initialize` → `notifications/initialized` → `tools/list`，协商版本为 `2025-11-25`；界面均显示绿色且 1/1 工具已启用。
- 同一对话分别调用 `probe_supplier`、`probe_business_query`，服务端各收到一次 `tools/call`。客户端正确展示各自 `source`、`synthetic: true` 及固定模拟行 `TEST ONLY / 12.34`，未串结果。
- 客户端显示本轮总耗时 38 秒；这是包含模型处理的对话耗时，不是数据库或 MCP 单次调用耗时。
- 本次仅证明无认证模拟端点的连接、工具发现和调用展示可用。真实 JWT、权限、取消、超时、审计、业务结果核对及 React 回归仍未完成，不据此启用正式外部入口。
- 模拟连接依赖临时测试进程，不属于正式部署配置；测试运行时退出后需重新启动探针或移除测试连接，不能将其误认为真实业务服务。

[WorkBuddy 官方连接器说明](https://open.workbuddy.cn/docs/connector)说明 MCP 配置、凭据占位符和生产远程 HTTPS 要求；不代表当前安装版本已通过本项目协议验证。

[MCP Streamable HTTP 规范](https://modelcontextprotocol.io/specification/2025-11-25/basic/transports)明确 Origin 校验、本地监听建议及断连与主动取消的区别；实施时根据客户端协商版本核对，不强制客户端使用本文引用版本。
