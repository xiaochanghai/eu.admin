# Agent 运行用量与监控

本次为兼容扩展：沿现有模型运行、运行审计和 `/metrics` 接入统计，补充默认关闭的单次执行、执行树共享预算和集团公司共享日/月配额，以及预算预警、余额、管理员额度设置及人工对账。周期账本复用既有业务服务及主数据库，不新增平行存储层、外部通知或费用估算。

## 统计口径

- 模型统计取自当前 SDK 的 `UsageContent`，输入、输出和总 Token 分别保存；没有报告的字段为 `null`，不能用字符数估算，也不能补零。流式统计通常在最终 usage chunk 才返回，见 [OpenAI 官方流式用量说明](https://developers.openai.com/cookbook/examples/how_to_stream_completions)。
- `Reported`：模型循环正常结束，且所有已识别响应均收到完整的三项统计。按响应 ID 合并累计快照，重复统计不重复相加；工具循环的不同模型响应相加。
- `Partial`：流取消、失败、审批暂停，或者某个响应/字段缺失、响应 ID 不明。返回数值只代表已经收到的部分，不能当作完整账单。未识别响应的累计快照保守合并，不猜测调用次数。
- `Unknown`：没有任何有效 Token 统计。历史记录和没有进入模型执行的运行也没有用量。
- `ModelDurationMilliseconds` 从模型流开始到结束，包含循环内的工具等待，不等同于纯推理耗时。`TimeToFirstTextMilliseconds` 从流开始到首段非空文本；只有角色、用量或工具事件而无文本时保持 `null`。
- 当前范围是 `MicrosoftAgentRuntimeEngine` 的 Agent 推理循环（含 Main Agent、委派 Agent 和工具往返）。独立模型评估、Embedding 等其他模型调用尚未纳入；委派的父子运行分别统计，不能将运行时长误当作互斥时长相加。

用量事件仅在运行引擎与运行服务内部传递，不成为 SSE 聊天内容，不影响 `business-query-result` 独立展示。成功、失败、取消和审批暂停都随终态审计保存已收到的统计；审计保存失败仍按失败处理。

## 单次执行 Token 预算

分类：BACKEND-HOST + BACKEND-BUSINESS + API-CONTRACT。现有 `appsettings.json` 的 `AgentExecution` 增加两个可空配置；缺省或 `null` 不改变现有行为，启用值必须大于零。不增加环境变量、Local JSON、密钥、数据库列或独立预算存储。

```json
"AgentExecution": {
  "MaximumModelOutputTokens": null,
  "MaximumRunTotalTokens": null,
  "TokenBudgetWarningPercent": 80
}
```

需要启用时由部署侧确定额度，例如把输出限制设为 `4096`、累计阈值设为 `20000`；这只是示例，不是推荐费用额度。Agent 配置不热更新，修改后整体部署并重启匹配的宿主和 Runtime 程序集。

- `MaximumModelOutputTokens`：限制每次模型响应的输出 Token，通过 [SDK `ChatOptions.MaxOutputTokens`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.chatoptions.maxoutputtokens) 传给供应商。它不是整段对话的输出上限；原有输入/输出字节、事件及工具调用次数限制仍独立生效。请求选项先克隆，保留 Qwen 思考参数、指令和工具，已有更小的输出限制不会被放宽。
- `MaximumRunTotalTokens`：限制一次 Agent 模型执行中，供应商已经报告的总 Token 累计值，涵盖该次执行的模型/工具往返。预算客户端位于 SDK 工具循环内侧，不因第二轮模型请求而重新获得额度；每轮输出限制取配置、已有请求限制、剩余总额度三者中的最小值。总 Token 使用供应商报告，不将输入和输出自行相加，不按字符数估算；单次响应重复的累计快照取最大值，不重复扣减。
- 已报告累计用量等于阈值时允许当前响应正常完成，但不再发送下一次模型请求；超过阈值、供应商报告输出超过已发送限制，或响应以 `length` 截断时，运行以 `MODEL_TOKEN_BUDGET_EXCEEDED` 失败，不伪报完成。上报的实际用量先交付审计，未知字段仍为 `null`。
- 启用累计阈值后，没有可用的总 Token（缺失、负数）返回 `MODEL_TOKEN_USAGE_UNAVAILABLE`，不继续工具循环，不把未知消耗当作零。只有输入/输出字段缺失但总 Token 合法时，仍可按已知总量执行预算，展示的完整性状态继续如实为 `Partial`。仅开启输出限制时，不要求供应商报告总 Token。
- 失败、取消或提前释放的请求不会在当前预算下重试；已有成功 BusinessQuery 结果仍独立保存、展示，不因后续预算失败丢失。审批暂停沿用当前流程；审批恢复只执行已批准工具，不在同一运行里继续推理。

边界：总 Token 往往在响应结束后才报告，预算不知道下一轮输入的精确 Token 数，因此首次或在途请求可能已经超过累计阈值；供应商是否严格执行输出限制也需真实联调。这不是请求前的精确 Token/金额预授权，不会撤销已执行工具或退还供应商消耗。单运行额度本身不包含子运行；需要执行树限制时使用下面的独立配置。新用户请求重新计量；跨运行累计由下方集团公司共享日/月配额负责。模型裁判、Embedding、按 Agent 分账和费用告警尚未接入。

两个预算错误沿现有运行失败事件及审计 `ErrorCode` 字符串返回，HTTP 路由、JSON 包装和权限不变，React 无需新增错误枚举。仓库外消费者若使用错误码白名单，需要接受这两个新增值。`AgentRuntimeOptions` 构造函数兼容源码的可选参数追加，引用 Runtime 的消费者需整体重新编译和部署。回滚先恢复两个配置为 `null` 并重启；恢复旧代码时整体回滚匹配的宿主/Runtime，不修改数据库和凭据。

## 执行树共享 Token 预算

分类仍为 BACKEND-HOST + BACKEND-BUSINESS + API-CONTRACT。在同一 `appsettings.json` 的现有 `UnifiedEntry` 配置体系增加可空字段：

```json
"UnifiedEntry": {
  "MaximumModelTotalTokens": null,
  "TokenBudgetWarningPercent": 80
}
```

缺省或 `null` 不启用，非空值必须为正整数。共享预算由每次 `UnifiedEntryExecutionScope` 拥有，不是单例账本、不新增数据库或配置源，不跨会话/新用户请求累加：

- Main Agent、`delegate_to_agent` 子 Agent，以及 `run_orchestration` 中每个 Agent 节点/重试，都传递同一个预算实例，不因创建新运行上下文而获得完整额度。单独调试 Agent 或独立启动的编排没有 UnifiedEntry 作用域，只执行原有单运行限制。
- 启用共享限制时，同一执行树的供应商模型请求逐轮排队；等待时使用调用方及执行作用域的取消令牌。前一响应完成后按实际报告的总 Token 扣减，再给下一请求剩余额度，避免并行节点同时消费同一份剩余额度。串行门仅覆盖一次模型请求，先结算、释放，再由 SDK 执行工具；工具等待不持有门，不阻塞委派子运行。
- 每轮输出上限取已有请求限制、`MaximumModelOutputTokens`、单运行剩余及执行树剩余的最小值。共享额度按供应商总 Token 收费，重复快照只结算一次，真实零值保留；不把父运行审计再相加一次。父子运行审计仍分别保存各自实际用量。
- 总用量等于额度时允许当前响应完成，后续模型请求在发出前以 `MODEL_TOKEN_BUDGET_EXCEEDED` 拒绝；超限、缺失或负用量沿现有错误路径失败。已发出请求被取消、失败或提前释放时，其未知消耗不能当作零，共享预算以 `MODEL_TOKEN_USAGE_UNAVAILABLE` 关闭后续调用。仅取消排队、释放未发出请求的租约不扣额度，也不污染其他子运行。
- 单运行/输出限制失败时，已知实际总用量仍先扣共享额度；共享预算不会因为更换子 Agent 客户端或编排重试被重置。作用域释放会取消排队者，租约可安全退出；预算对象不序列化到事件、数据库记录或前端响应。

共享额度同样是供应商事后上报的累计阈值，不是精确预付费硬上限。启用后会降低同一执行树的模型并发，排队时间纳入既有运行/节点超时，需按实际负载验证延迟。额度保持 `null`，由部署侧确定后再启用；先整体重新编译、部署 IServices/Services/Runtime/Agent 宿主并重启。回滚将 `UnifiedEntry:MaximumModelTotalTokens` 恢复为 `null` 并重启，不改数据库；代码回滚使用匹配的整套程序集。现有成功 BusinessQuery 结果的独立持久化和展示不受后续预算失败影响。

## 预算预警与监控

配置沿用上面两个现有配置节的 `TokenBudgetWarningPercent`：默认 `80`，非空值仅允许 `1–99`；`null` 只关闭接近上限预警，不关闭额度拦截或其他预算信号。`AgentExecution` 的值用于单运行和实际输出上限，`UnifiedEntry` 的值用于执行树累计额度。额度均为 `null` 时不产生预算信号，也不因新增默认百分比启用限制。

信号来自供应商响应完成后报告的真实用量，不估算输入、不把未知补零、不定时轮询。每个 Agent 模型客户端的输出及单运行监控、每个执行作用域的共享监控，分别按“范围 + 信号”去重；并发观测、后续重试或新子客户端不会重复报告同一执行树信号。新运行、新执行树会重新计数。

| 固定信号 | 含义 | 对运行的影响 |
|---|---|---|
| `Warning` | 已知消耗达到预警百分比，但未达到上限 | 仅预警，不中断 |
| `Exhausted` | 已知消耗恰好等于上限 | 累计预算允许本轮完成、拒绝下一请求；输出预算仅表示本轮达到输出上限 |
| `Exceeded` | 已报告消耗超过实际上限 | 沿现有预算错误失败 |
| `UsageUnavailable` | 启用累计预算后总用量缺失、负数，或已发出请求未完成结算 | 关闭当前累计预算的后续调用，不把未知消耗当零 |
| `OutputLimited` | 启用预算的响应被供应商以 `length` 截断 | 预算错误失败，不冒充已报告数值超限 |

一次响应可以同时影响输出、单运行及执行树范围；这些信号不是失败运行数，不能直接相加得到失败率。跳过预警区间直接达到/超过上限时，只发对应终点信号，不虚构一次较早预警。只有未发出的租约或排队被取消时，不产生未知消耗信号。总用量已知而输入/输出缺失时，可以按已知总量观察累计阈值，审计完整性仍如实为 `Partial`。

实际输出上限可能同时被已有请求选项、单运行或执行树剩余额度收紧；`ModelOutput` 以本轮最终发送的上限为准，而不是永远使用配置的原始值。该范围每个模型客户端每种信号只计一次，不是模型响应次数。

宿主复用同一个 `AgentMetrics`，通过固定 Warning 日志及现有 `/metrics` 暴露信号，日志只包含范围和状态，不包含运行标识、输入、凭据或金额。指标/日志导出器异常为尽力而为，不放宽额度、不替代终态审计、不让已有查询结果丢失；它不是可靠消息队列或通知投递确认。配置修改需整体编译、部署并重启；关闭预警可把两个百分比恢复为 `null`，关闭限制仍需将对应额度恢复为 `null`。

## 集团公司共享日/月配额

归属固定为可信执行身份的 `GroupId + CompanyId`：同一集团、公司内全部用户的主 Agent、委派子 Agent、编排节点、调试、后台任务及每轮工具往返模型请求共用额度；换用户、Agent 或重新发起运行不会重置。不同集团或不同公司分别计账。UserId 只记录实际调用人或管理操作者，不参与周期键、余额、策略或幂等所有者计算，也不是按 Agent 分账。

集团、公司由现有登录 `IUser.GroupId` / `IUser.CompanyId` 提供，在 HTTP 边界冻结进 `AgentExecutionIdentity`；委派、编排和审批恢复传递该身份。后台任务创建时显式持久化到已有 BaseEntity 字段，后台执行从任务原始归属恢复，不借用工作进程或当前操作者的公司。历史任务缺少有效归属时，启用额度后失败关闭，不自动猜测或补填。集团/公司为空或 Guid.Empty 均拒绝额度读写；客户端查询参数不能改变归属。

本次项目所有者明确确认集团 + 公司为该功能的租户边界，四个额度实体不实现 ITenantEntity、不新增 TenantId 字段，复用 BaseEntity 的可空、创建后不可普通更新的 GroupId/CompanyId；业务命令仍要求非空有效 GUID。其他模块原有 TenantId、JWT、运行/任务/操作审计隔离语义保持不变；TenantId 不参与额度周期哈希、管理授权或命令幂等哈希。

首次部署按更新后的 075 → 076（MySQL 013 → 014）执行。若已执行含 TenantId 或所有者 UserId 的旧预览脚本，先停写、备份，再执行 SQL Server 077 或 MySQL 015；转换确认所有已存在额度表全部为空（包括逻辑删除记录）后，才移除旧所有者字段、把预占记录的 UserId 重命名为 ConsumerUserId，并重建 `GroupId + CompanyId` 归属索引。随后重跑 075/076 或 013/014 校验结构。任一表有数据、归属列不匹配或存在自定义依赖都会停止，不清表、不补填归属、不重置余额；有数据必须单独设计保留原周期 ID、预占引用及幂等回执的升级。SQL Server 转换位于事务内；MySQL DDL 自动提交，必须依赖执行前备份恢复。成功转换后不能直接运行依赖旧字段的额度代码。

现有 `appsettings.json` 新增 `AgentUserTokenQuota`，三个数值默认 `null`，ManagementEnabled 默认 false；两项功能都关闭时不解析或访问配额表，不要求额外身份，不改变原调用行为。开启示例（仅示例额度，不是默认值）：

```json
"AgentUserTokenQuota": {
  "DailyTotalTokens": 100000,
  "MonthlyTotalTokens": 2000000,
  "RequestReservationTokens": 10000,
  "TimeZoneId": "Asia/Shanghai"
}
```

日/月可分别开启；开启任一项必须设置正的 `RequestReservationTokens`，且不超过每个已开启上限。只使用既有 appsettings.json 配置源，无新环境变量要求、Local.json 或第二套密钥。配置改变需要重启。时区决定自然日/月边界；默认日界为北京时间 00:00，月界为每月 1 日 00:00。不得在活跃周期随意换时区，否则周期键会改变；切换前须停写、备份并由维护者制定旧账本承接方案，不能借此清空额度。

每次真实供应商请求先在一个 Serializable 事务内同时预占日/月额度（`UsedTokens + ReservedTokens + 本次预占 <= 上限`），随后持久化开始状态，成功后才发出请求。固定预占粒度意味着剩余量大于零但小于预占量时仍会拒绝；它不是可用输出 Token 的估算。并发新周期插入冲突、事务冲突或版本条件更新失败会失败关闭，不自动重复派发网络请求，调用方可在新的运行中按明确错误决定重试。使用现有主 SqlSugar 数据源及 BaseServices，不采用第二套 Store、独立连接或进程内账本替代数据库。

- 完整非负 `total_tokens`：释放原预占并累加实际值，包括真实 0；重复相同结算不重复收费，冲突结算拒绝。
- 供应商实际总量超过本次预占：仍保存完整实际消耗，并返回 `MODEL_TOKEN_QUOTA_EXCEEDED`，不得裁剪为预占值。提示词没有本地可信计量，供应商也可能忽略输出限制，所以这是并发准入及事后限制，**不是零超支的预付费硬上限**。每次输出只收紧为预占量与已有输出限制的较小值。
- 请求取消、异常、提前释放流、缺失/负总量：未派发可以退预占；已开始但没有完整可信用量则保留预占并冻结原始日/月周期，报 `MODEL_TOKEN_USAGE_UNAVAILABLE`，不记零、不自动退款。即使中途收到部分 usage，也不冒充完整计费。结算和必要清理不使用已经取消的请求令牌。
- 账本、事务或可信集团/公司身份不可用，或实际消费用户无法用于审计：`MODEL_TOKEN_QUOTA_UNAVAILABLE`，不降级为匿名或系统账户，不继续模型调用，不输出原始数据库异常。
- 周期跨界：结束时只结算预占中的原始周期，不把旧请求记到次日/次月。进程重启保留账本及未完成预占，不重新发放相同周期额度；崩溃后未结算记录没有自动过期退还或自动对账，仍占用容量，需要维护者核对供应商账单后处理。未知标记的月周期会阻止后续日期的调用直到月界或人工核对。

模型调用层按物理请求计账，父 Agent 审计总计不再另扣一次，主/子请求在工具执行前分别完成结算，不持有数据库事务跨越模型或工具 I/O。账本保存集团、公司、用户、运行标识及用量，不保存输入、输出、模型密钥或价格。相应错误沿现有运行终态契约返回；React 错误码字段本就为字符串，无需改变 API 包装或加入新的权限旁路。成功的 BusinessQuery 独立结果不会因后续配额拒绝而丢失。

上线前由维护者执行 [`SQL Server 075`](../../eu.core/EU.Core.Api.Agent/Database/Migrations/SqlServer/075_add_user_token_quota.sql) 或 [`MySQL 013`](../../eu.core/EU.Core.Api.Agent/Database/Migrations/MySql/013_add_user_token_quota.sql)，新增 `AgUserTokenQuotaPeriod`、`AgUserTokenQuotaReservation`，包含完整 BaseEntity 字段，以已有 GroupId/CompanyId 显式隔离，才可开启额度。预占表的 ConsumerUserId 只用于追踪实际调用人。没有自动启动建表或数据修复；关闭时缺少这两表不影响旧运行。脚本可重复执行，对已有列/主键/索引的不兼容结构停止；MySQL DDL 自动提交，中断时核对后重跑。回滚把两个周期上限置 `null` 并重启，或恢复旧版应用，保留账本及记录，不删表或伪造退款；关闭期间的请求不计入本配额账本，重新开启前须明确这段用量的对账政策。

当前只覆盖 Agent 聊天运行时的供应商调用，不包括独立评测 Judge、Embedding、外部非 Agent 模型调用；提供当前登录用户余额、当前租户授权管理员的额度设置及人工对账。未实现金额成本计价或真实外部通知。数据库并发保护不等于 Agent 多副本支持，宿主 `ReplicaMode=single` 保持不变。

## 额度管理与人工对账

分类：BACKEND-HOST + BACKEND-BUSINESS + FRONTEND + API-CONTRACT + CROSS-END-CONTRACT + DATABASE。

- `AgentUserTokenQuota:ManagementEnabled` 默认 `false`；保持原宿主默认额度行为，不读取新增设置表。启用前手工执行 [`SQL Server 076`](../../eu.core/EU.Core.Api.Agent/Database/Migrations/SqlServer/076_add_user_token_quota_management.sql) / [`MySQL 014`](../../eu.core/EU.Core.Api.Agent/Database/Migrations/MySql/014_add_user_token_quota_management.sql)，前置为 075/013。新增设置及调整审计表，均继承 BaseEntity 并仅按集团/公司隔离；操作者 UserId 只用于授权和审计。不启动自动迁移。
- 开启后配置 `Administrators` 的 `{ "GroupId": "<集团 GUID>", "CompanyId": "<公司 GUID>", "UserId": "<现有登录用户 GUID>" }` 组合。该配置不是密钥；空列表拒绝所有管理操作。只复用当前登录 JWT/IUser，不增加 Token、环境变量或 Local JSON。管理策略 `AgentQuotaManage` 独立于既有仅要求登录的 AgentAdmin，匿名、非名单用户及跨集团/公司管理员不能访问。允许的管理员仅操作自己当前集团、公司的额度。
- `GET /api/agent-usage/management/access` 返回标准包装中的 `CanManage`。额度面板只在该响应允许时显示管理操作，服务端每个管理请求仍验证专用策略，不依赖前端隐藏。
- 当前集团公司路径为 `/api/agent-usage/management`：`GET/PUT policy` 读取/修改共享设置，`GET pending` 返回最多 100 条未结算记录，`POST reservations/{reservationId}/reconcile` 人工对账。不接受客户端 userId/groupId/companyId/operatorId，不暴露通用账本 CRUD；所有者取可信登录上下文。仓库外消费者需同步 PascalCase 输入/输出、十进制字符串 Token/版本、UTC 时间及 400/409/404/503 错误包装。
- 未配置集团公司按宿主默认设置（版本 0），读取不创建记录。覆盖设置必须至少有一个正周期上限及有效单次预占；`UseDefaults=true` 时三项覆盖值必须为 null。启用管理后，即使宿主默认额度关闭，也可为当前集团公司开启共享额度。设置不更换时区、不重置已用/预占；降低上限可能立即阻止新请求。自然周期时区不要在已有用量期间更改，以免产生另一套窗口。
- PUT 要求唯一 `OperationId`、读取到的 `ExpectedRevision` 和 `Reason`；事务内版本比较更新并追加调整审计。相同操作、所有者和内容返回原回执，不重复修改；不同内容/所有者复用标识返回 409。预占事务再次检查已解析的设置版本，拒绝过期授权；已开始请求仍结算原始窗口。
- 对账要求 `OperationId`、明确非负 `ActualTokens`、`Confirmed=true`、原因及 `EvidenceReference`。`ActualTokens` 是必需的 JSON 字段，漏传或 null 在请求绑定时拒绝，不进入对账事务；真实零必须明确填写 0（整数或十进制字符串）。现有前端已始终传入该字段，仓库外调用方不得依赖漏传默认零。引用不要包含凭据或供应商账单正文。对账将原日/月预占释放，实际消耗计入原周期，和追加式审计在同一事务提交。不可覆盖已有已知结算，最后一笔未知记录核实后才解除冻结；累计数溢出导致的冻结不能据此解除。
- 状态 3（已报告未知）允许核实；遗留状态 0/1 必须已有 Completed/Failed/Cancelled 的运行终态审计、终态时间超过 5 分钟。活跃、审批暂停、最近终态或缺失终态证据均返回 409；未开始的请求只能核实为 0。不自动过期、退款或推算真实用量。进程崩溃且无法提供终态证据的预占仍保留，需要维护者通过既有运行恢复流程补齐事实，不能仅凭时间释放。
- 请求提交后网络失败可能已提交。前端同参数重试复用 OperationId；重新读取或修改参数是新操作，不能盲目再次退款。修改后的回执和原快照保存在 `AgUserTokenQuotaAdjustment`，审计失败回滚设置/账本修改。迟到的未知清理不覆盖人工核实结果，冲突的迟到已知用量失败关闭。
- 回滚先关闭 ManagementEnabled 并恢复匹配应用，保留设置、账本和审计表，不删记录。若依赖用户覆盖上限，关闭管理将恢复宿主默认上限，可能放宽准入；停写并核对默认配置后回滚。关闭整个额度功能仍不代表这段时间的供应商调用无需计费。

管理入口是现有 Agent 额度/运行面板的一部分，不新增菜单、状态框架或第二套业务存储。数据读取、写入和账户切换均有取消/请求所有权边界；更改额度和对账必须二次确认。

## 当前用户余额接口

分类：BACKEND-HOST + BACKEND-BUSINESS + FRONTEND + API-CONTRACT + CROSS-END-CONTRACT + DATABASE。`GET /api/agent-usage/quota` 使用既有 `HistoryRead` 认证策略和标准 `ServiceResult` 包装，响应不缓存。所有者只取 `ICallerContext` 的集团和公司；没有所有者选择参数或账本修改方法，额外查询参数不能改变归属。当前用户只作为实际消费人留痕。网关消费者沿用 `/Agent/api/agent-usage/quota`。

`Data` 为 PascalCase：`Enabled`、`TimeZoneId`、`EvaluatedAtUtc`、`RequestReservationTokens`、`CanReserve`、`Periods`。Token 数全部为十进制字符串（或可空字段的 `null`），避免 JavaScript 长整数精度损失。每个周期包括：

- `Kind`：`Daily` 或 `Monthly`，只返回当前启用的周期。
- `StartUtc`（含）及 `EndUtc`（不含）：与预占共享同一时区/周期算法。
- `LimitTokens`、`UsedTokens`（已结算的已知部分）、`ReservedTokens`（未释放预占）。
- `RemainingTokens`：`max(0, 上限 - 已结算 - 预占)`；冻结未知用量时为 `null`，不是零或充足。
- `HasUnknownUsage`、`CanReserve`：冻结时禁止预占；余额小于配置单次预占量时也不能准入。外层 `CanReserve` 要求所有周期都允许。

关闭配置时 `Enabled=false`，`Periods=[]`，`RequestReservationTokens` 和 `CanReserve` 为 `null`；不解析数据库或未使用的时区。启用且当前周期尚无账本时，真实已结算量和预占量为零，但查询不创建账本或请求记录。日/月数据在同一只读事务内读取；软删除、负计数、窗口不一致或数据库失败返回 `MODEL_TOKEN_QUOTA_UNAVAILABLE`（业务状态 660043、HTTP 503），不返回零额度，不回显原始数据库异常。调用方取消继续传播取消。

当前快照不是收费流水或实际请求授权；并发运行会立即改变余额，模型调用仍执行原来的原子预占检查。新增映射 `MODEL_TOKEN_BUDGET_EXCEEDED`（660040/429）、`MODEL_TOKEN_USAGE_UNAVAILABLE`（660041/503）、`MODEL_TOKEN_QUOTA_EXCEEDED`（660042/429）、`MODEL_TOKEN_QUOTA_UNAVAILABLE`（660043/503）；运行事件仍使用原错误码字符串。仓库外消费者需接受此新增只读接口的字符串数值约定。

React 在 Agent Definition 的运行抽屉/历史里显示“当前租户共享 Token 额度”，支持独立刷新，并在运行终态及切换 Agent 时重新读取。额度查询失败不影响历史查询；刷新、失败或账号切换立即隐藏旧余额，不把旧的“可预占”当作当前授权。界面区分未启用、额度不足和未知用量冻结；显示日期按浏览器时区，周期时区另行标明。

本入口不增加迁移或默认启用额度。后端及消费者一起部署；回滚前端可继续使用原历史接口，回滚后端须同时回滚新余额组件，否则组件会显示读取失败。账本保留，不自动退款或修改数据。

## 查看方式

`GET /api/agents/{agentId}/runs?take=20` 在原包装和字段之外增加可空 `ModelProfileId`、`ModelUsage`。`ModelUsage` 包含三项 Token、完整性状态及两项耗时。认证、权限和最多 100 条的边界保持不变。

React Agent Definition 的“运行 Agent”抽屉和独立“运行历史”入口复用此接口，最多显示最近 20 条，不作为全部历史或账单：

- 展开单条记录查看输入/输出/总 Token、统计完整性、运行/模型循环耗时、首段文本时间、模型配置、输出字符数、错误码和工具审计。日期按浏览器本地时区显示。
- 未知或非法数值显示“未知”，不补零；真实零值保留为零。部分统计明确标注“部分统计”，不推算费用。接口的实际运行键为 `RunId`，不是 `Id`。
- 支持手动刷新；加载失败显示错误及重试入口，有旧数据时标明尚未刷新，不误报为空。关闭抽屉、切换 Agent/登录会话或卸载组件时取消请求，阻止旧请求写回。
- 停用、归档或有未保存 Draft 的 Agent 可以查看历史，但不能由此发起运行。服务端原 Debug 权限不变。
- 调试流区分完成、失败、客户端取消和审批暂停；未收到终态就断流时要求查看审计确认，不直接显示成功。取消客户端请求不等同于已确认服务端取消。

现有 `/metrics` 仍要求 `AgentDeployment:MetricsEnabled=true` 和 AuditRead 权限，本次不改变配置默认值和授权：

- `agent_runtime_runs_total{status}`：执行中的运行终态计数，审批暂停单列，不算成功。
- `agent_runtime_usage_reports_total{usage_status}`：完整、部分和未知统计覆盖率。
- `agent_runtime_tokens_total{usage_status,kind}`：模型报告的 Token 累计数（input/output/total）；没有值不创建零值序列，三种 kind 不应相加。
- `agent_runtime_tool_calls_total{status}`：MCP 调用的成功、失败和阻止次数；每个调用终态只统计一次，不包括内部工具。
- `agent_runtime_token_budget_signals_total{scope,signal}`：启用预算的去重信号；`scope` 仅有 `ModelOutput`、`AgentRun`、`ExecutionTree`，`signal` 为上表五项。OpenTelemetry 对应现有 Meter 的 `agent.runtime.token_budget_signals` Counter。
- `agent_runtime_duration_milliseconds`、`agent_model_duration_milliseconds`、`agent_model_time_to_first_text_milliseconds`、`agent_tool_duration_milliseconds`：固定桶直方图，可计算分位数。

新指标只用有限枚举标签，不写用户、租户、运行标识、工具名、模型地址、输入、输出或凭据。指标是进程内累计值，重启归零；持久历史来自运行审计。OpenTelemetry 使用已有 `EU.Core.Api.Agent` Meter，不新增出口配置。

Prometheus 示例（成功率中排除策略阻止）：

```promql
histogram_quantile(0.95, sum by (le) (rate(agent_model_time_to_first_text_milliseconds_bucket[5m])))
sum(rate(agent_runtime_tool_calls_total{status="ToolSucceeded"}[5m]))
  / sum(rate(agent_runtime_tool_calls_total{status=~"ToolSucceeded|ToolFailed"}[5m]))
```

预算监控可按范围及信号查询上述 Counter；未发生的信号不创建零值序列，进程重启归零。新增 `agent_user_token_quota_signals_total{signal}`，仅有 Rejected/Frozen/Unavailable，分别表示额度不足、用量未知和准入依赖失败；OpenTelemetry 为 `agent.user_token_quota.signals`。这些是事件计数，不是当前余额或持续冻结状态。

提供 [`Prometheus 告警规则`](monitoring/agent-token-alerts.rules.yml)，覆盖预算预警/阻止及共享额度异常，保留 job/instance 和有界 signal/scope；新序列分支补偿首次信号无历史基线的情况。部署时按实际抓取标签收窄范围，并使用 `promtool check rules <规则文件>` 验证；再配置 rule_files 和实际通知渠道。规则结构参考 [Prometheus 官方说明](https://prometheus.io/docs/prometheus/latest/configuration/alerting_rules/)。当前环境未安装 promtool，也未向真实监控服务加载规则。事件过去 10 分钟后告警会恢复，这不证明冻结已解除，账本/审计才是事实源。未发生的事件不能当成零用量证明。

没有自动发出短信、邮件或外部消息；通知接入需要用户指定渠道与部署方配置 Alertmanager。不估算费用，也不把规则文件存在当成生产告警已上线。

## 数据库上线与回滚

不自动迁移，不连接或修改部署数据库。更新实体新增七个可空字段，旧审计不补假值。

1. 备份数据库，确认运行审计已经完成规范化，停写后执行对应脚本：
   - SQL Server：[`074_add_agent_run_usage.sql`](../../eu.core/EU.Core.Api.Agent/Database/Migrations/SqlServer/074_add_agent_run_usage.sql)，前置为现有 048–051 审计规范化。
   - MySQL 8：[`012_add_agent_run_usage.sql`](../../eu.core/EU.Core.Api.Agent/Database/Migrations/MySql/012_add_agent_run_usage.sql)，要求已存在规范化的 `ID`、`AgentVersionId` 等审计字段。仓库 001 的旧 `RunId/DocumentJson` 表不是新版实体，脚本检测到旧结构会停止，不冒充完整规范化迁移；旧 MySQL 部署需先完成既有审计结构迁移。
2. 脚本可重复执行：不覆盖已有用量、不改公共字段；列类型或可空性不符时停止，禁止自动破坏性转换。MySQL DDL 自动提交，中断后确认错误并重跑。
3. 数据库扩展完成后部署新版后端，再部署前端；仓库外消费者只需忽略新增可空字段。数据库未迁移时不要部署新版后端，否则审计读写会因缺列失败。
4. 回滚先恢复旧版应用，保留新增可空列及已经记录的用量即可；不必删列。若确需删除，先备份并确认所有新版本已停止，再由数据库维护方执行显式删列，不能在应用启动时执行。

## 验证与后续

离线测试覆盖 SDK 实际流式 usage 解析与工具循环、重复快照、缺失/零值、失败与取消、审批暂停、内存 SQLite 插入/更新/读回、监控并发及分桶。外部模型和部署数据库需要隔离环境另做联调。

运行终态和每个工具终态的可选监控分别尽力记录；监听器/导出器异常不传播到运行流，不覆盖已经持久化的审计、取消信号或业务错误。单条监控失败不阻止后续工具统计，指标可能缺失，不能用监控失败时的统计代替审计事实源。回归覆盖抛异常的 telemetry、真实 MeterListener、成功/失败/取消/审批暂停，以及审计持久化异常仍向调用方传播。

前端轻量离线检查不增加测试框架或依赖，使用 Node 内置 runner 和现有 TypeScript。安装项目依赖后，在 `eu.admin.react` 运行：

```text
node tests/agentRunHistory.test.cjs
pnpm type:check
```

也可把已经安装的 TypeScript 包路径作为该脚本的唯一参数，不写环境变量或修改应用配置。覆盖用量/时间格式、审批/取消状态、事件流响应与错误、额度响应校验和 long 精度，以及刷新、会话切换和卸载的请求所有权。Hook 边界模拟不是 React 或浏览器集成测试，不能代替完整类型检查。

浏览器待验证清单：打开运行历史并展开记录；刷新失败后的旧记录标识；关闭后重开；切换 Agent/账号；停用、归档及未保存 Draft 下只能查看历史；运行完成/失败/取消/审批暂停后的显示与历史刷新，以及额度管理员的二次确认和失败后重试。项目锁定依赖已从本机缓存离线恢复，完整类型检查已通过；真实浏览器联调尚未执行。

离线预算测试覆盖配置绑定、无效额度、关闭兼容、零/缺失/负用量、快照去重、剩余额度、并发请求串行、执行间隔离、取消后不重试、输出截断与供应商忽略限制；使用已安装 SDK 的 HTTP 替身验证 Qwen 扩展和工具往返每轮限制，并验证预算失败的终态审计及成功查询结果保留。共享预算另覆盖父子同实例传递、编排节点重试、独立执行树隔离、等待取消不收费、在途取消关闭后续请求、作用域释放，以及真实 SDK 父请求释放后再委派、不死锁、父审计不重复包含子用量。

预算监控离线测试另覆盖预警百分比绑定及无效值、恰好达到阈值、跳过预警、长整数用量、并发去重、每个预算实例隔离、输出截断与实际超限区分、关闭预警不关闭额度、两个累计范围同时失败、取消阶段区分、统一入口到子 Agent 的监控传递、固定日志及 Meter/Prometheus 标签，以及监控异常不能释放额外额度或替代预算错误。

用户周期配额离线测试覆盖实际 SqlSugar 日/月事务、所有者隔离、并发独立 SQLite 连接、原始周期结算、重复开始/结算、取消/流释放、未知用量冻结、关闭兼容、数据库/结算故障关闭及真实 Autofac 作用域；使用已安装 SDK 的 HTTP 替身验证父模型 → 子 Agent → 父模型每个物理请求分别只计费一次（5 + 7 + 6 = 18 Token），以及本地预算先拒绝时不创建配额预占。

余额离线测试使用真实 SqlSugar/内存 SQLite，覆盖未使用周期只读不落表、已知结算及在途预占、未知冻结、超支及 long 边界、所有者隔离、软删除/损坏账本、周期跨界、单周期/关闭兼容、取消、认证策略和固定 503；正常 MVC JSON 校验 PascalCase、UTC 和十进制字符串，不暴露用户/租户标识。

管理与对账离线测试覆盖专用管理员策略、跨集团/公司/操作者隔离、同集团公司不同用户共享、设置版本及幂等重放、审计失败原子回滚、原周期结算、重复命令、取消、活跃请求保护、未知用量解除冻结，以及软删除未知预占仍保守冻结。新增请求边界回归确认漏传/null 的实际用量被拒绝且不释放预占，显式整数/字符串零可正常对账；真实 MVC 输入绑定及标准错误工厂返回 400。前端边界测试另覆盖二次确认、重复点击抑制、失败后沿用操作标识、账号切换/卸载取消请求、权限失败提示及精确整数输入。

本次验证：后端全解决方案编译通过（最后一次构建 72 条警告、0 错误；警告含现有依赖漏洞），284 项定向离线测试通过；前端完整类型检查和 48 项轻量离线检查通过。告警 YAML 语法/结构检查通过，未运行 promtool。未执行部署数据库迁移、真实供应商请求、浏览器联调或外部通知；不能将这些离线检查视为生产验收。

已补额度管理入口、原子人工对账及告警规则。后续仍需部署执行迁移、配置管理员、监控抓取/规则验证与通知渠道，以及真实数据库/供应商/浏览器联调；不按动态模型价格自动估算费用。未增加新任务 Worker、分布式协调或多副本支持，迁移未在部署数据库执行。

## 实现文件

- 集团公司共享周期配额：[`AgentUserTokenQuotaOptions.cs`](../../eu.core/EU.Core.Api.Agent/Configuration/AgentUserTokenQuotaOptions.cs)、[`AgUserTokenQuotaProvider.cs`](../../eu.core/EU.Core.Api.Agent/Configuration/AgUserTokenQuotaProvider.cs)、[`AgentUserTokenQuotaContracts.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/Runtime/AgentUserTokenQuotaContracts.cs)、[`IAgUserTokenQuotaServices.cs`](../../eu.core/EU.Core.IServices/AG/IAgUserTokenQuotaServices.cs)、[`AgUserTokenQuotaServices.cs`](../../eu.core/EU.Core.Services/AG/AgUserTokenQuotaServices.cs)、[`AgUserTokenQuotaPeriod.cs`](../../eu.core/EU.Core.Model/Entity/AG/AgUserTokenQuotaPeriod.cs)、[`AgUserTokenQuotaReservation.cs`](../../eu.core/EU.Core.Model/Entity/AG/AgUserTokenQuotaReservation.cs)、[`AgentUserTokenQuotaChatClient.cs`](../../eu.core/Src/EU.Core.Agent.Runtime/AgentUserTokenQuotaChatClient.cs)、[`AgentUserTokenQuotaTests.cs`](../../eu.core/Src/EU.Core.Tests/AgentUserTokenQuotaTests.cs)。
- 运行契约：[`AgentRuntimeContracts.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/Runtime/AgentRuntimeContracts.cs)、[`AgentRuntimeTelemetryContracts.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/Runtime/AgentRuntimeTelemetryContracts.cs)。
- 模型采集：[`MicrosoftAgentRuntimeEngine.cs`](../../eu.core/Src/EU.Core.Agent.Runtime/MicrosoftAgentRuntimeEngine.cs)、[`ModelUsageAccumulator.cs`](../../eu.core/Src/EU.Core.Agent.Runtime/ModelUsageAccumulator.cs)；[`AssemblyInfo.cs`](../../eu.core/Src/EU.Core.Agent.Runtime/Properties/AssemblyInfo.cs) 仅增加测试程序集的内部访问。
- 单次预算：[`AgentTokenBudgetChatClient.cs`](../../eu.core/Src/EU.Core.Agent.Runtime/AgentTokenBudgetChatClient.cs)、[`AgentRuntimeOptions.cs`](../../eu.core/Src/EU.Core.Agent.Runtime/AgentRuntimeOptions.cs)、[`AgentExecutionOptions.cs`](../../eu.core/EU.Core.Api.Agent/Configuration/AgentExecutionOptions.cs)、[`appsettings.json`](../../eu.core/EU.Core.Api.Agent/appsettings.json) 及宿主 `Program.cs` 的预算映射。
- 共享预算：[`AgentModelTokenBudgetContracts.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/Runtime/AgentModelTokenBudgetContracts.cs)、[`UnifiedEntryModelTokenBudget.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/UnifiedEntry/UnifiedEntryModelTokenBudget.cs)、[`UnifiedEntryExecutionScope.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/UnifiedEntry/UnifiedEntryExecutionScope.cs)、[`UnifiedEntryContracts.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/UnifiedEntry/UnifiedEntryContracts.cs)、[`UnifiedEntryOptions.cs`](../../eu.core/EU.Core.Api.Agent/Configuration/UnifiedEntryOptions.cs)，以及 `UnifiedEntryService`、`DelegateToAgentTool`、`RunOrchestrationTool`、`OrchestrationRuntimeService` 的预算传递。
- 审计持久化：[`AgentRuntimeService.cs`](../../eu.core/EU.Core.Services/AG/AgentRuntimeService.cs)、[`AgAgentRunAuditServices.cs`](../../eu.core/EU.Core.Services/AG/AgAgentRunAuditServices.cs)、[`AgAgentRunAudit.cs`](../../eu.core/EU.Core.Model/Entity/AG/AgAgentRunAudit.cs)。
- 宿主监控：[`AgentTokenBudgetMonitor.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/Runtime/AgentTokenBudgetMonitor.cs)、[`AgentMetrics.cs`](../../eu.core/EU.Core.Api.Agent/Observability/AgentMetrics.cs)、[`Program.cs`](../../eu.core/EU.Core.Api.Agent/Program.cs)（同实例指标接口别名及预算配置映射，Worker 注册不变）。
- React 消费端：[`agent.ts`](../../eu.admin.react/src/api/modules/agent.ts)、[`FormPage.tsx`](../../eu.admin.react/src/views/agent/agentDefinition/FormPage.tsx)、[`RunHistory.tsx`](../../eu.admin.react/src/views/agent/agentDefinition/RunHistory.tsx)、[`runHistoryPresentation.ts`](../../eu.admin.react/src/views/agent/agentDefinition/runHistoryPresentation.ts)。
- 前端离线检查：[`agentRunHistory.test.cjs`](../../eu.admin.react/tests/agentRunHistory.test.cjs)。
- 余额查询和展示：[`AgentUsageController.cs`](../../eu.core/EU.Core.Api.Agent/Controllers/AgentUsageController.cs)、[`UserTokenQuota.tsx`](../../eu.admin.react/src/views/agent/agentDefinition/UserTokenQuota.tsx)、[`AgentUserTokenQuotaBalanceTests.cs`](../../eu.core/Src/EU.Core.Tests/AgentUserTokenQuotaBalanceTests.cs)。
- 管理与对账：[`AgentUserTokenQuotaManagementContracts.cs`](../../eu.core/EU.Core.IServices/AG/Contracts/Application/Runtime/AgentUserTokenQuotaManagementContracts.cs)、[`AgUserTokenQuotaPolicy.cs`](../../eu.core/EU.Core.Model/Entity/AG/AgUserTokenQuotaPolicy.cs)、[`AgUserTokenQuotaAdjustment.cs`](../../eu.core/EU.Core.Model/Entity/AG/AgUserTokenQuotaAdjustment.cs)、[`AgentQuotaManagementController.cs`](../../eu.core/EU.Core.Api.Agent/Controllers/AgentQuotaManagementController.cs)、[`AgUserTokenQuotaServices.Management.cs`](../../eu.core/EU.Core.Services/AG/AgUserTokenQuotaServices.Management.cs)、[`AgentQuotaManagementAuthorizationHandler.cs`](../../eu.core/EU.Core.Api.Agent/Security/AgentQuotaManagementAuthorizationHandler.cs)、[`QuotaManagement.tsx`](../../eu.admin.react/src/views/agent/agentDefinition/QuotaManagement.tsx)、[`AgentUserTokenQuotaManagementTests.cs`](../../eu.core/Src/EU.Core.Tests/AgentUserTokenQuotaManagementTests.cs)。
- 固定预算/配额错误映射及契约回归：[`AgentApiErrorCatalog.cs`](../../eu.core/EU.Core.Api.Agent/Errors/AgentApiErrorCatalog.cs)、[`AgAgentApiResponseFoundation_Should.cs`](../../eu.core/Src/EU.Core.Tests/Service_Test/AgAgentApiResponseFoundation_Should.cs)。
- 回归测试：[`AgentRuntimeTelemetryTests.cs`](../../eu.core/Src/EU.Core.Tests/AgentRuntimeTelemetryTests.cs)、[`AgentTokenBudgetTests.cs`](../../eu.core/Src/EU.Core.Tests/AgentTokenBudgetTests.cs)、[`AgentTokenBudgetMonitoringTests.cs`](../../eu.core/Src/EU.Core.Tests/AgentTokenBudgetMonitoringTests.cs)、[`UnifiedEntryBusinessQueryResultTests.cs`](../../eu.core/Src/EU.Core.Tests/UnifiedEntryBusinessQueryResultTests.cs)。
- 数据库及文档：运行用量、共享配额、管理对账各自的 SQL Server/MySQL 迁移脚本、[`迁移 README`](../../eu.core/EU.Core.Api.Agent/Database/Migrations/README.md)、[`告警规则`](monitoring/agent-token-alerts.rules.yml)、本说明及[`后端文档索引`](README.md)。



集团/公司归属回归：额度用量、预占、租户设置、幂等回执及管理员权限均检查集团和公司；同一集团/公司的不同用户及不同 TenantId 共用额度。隔离 SQLite 覆盖跨集团/公司拒绝、缺失归属前置拒绝、可信 HTTP 上下文及后台任务原始归属恢复；离线检查确保 SQL Server/MySQL 初始建表不声明 TenantId 或所有者 UserId，且归属索引只包含集团、公司和业务周期字段。旧预览结构只能通过 077/015 在额度表全空时转换；临时测试不写主库业务数据，不以结构验证替代额度接口端到端联调。MySQL 未连接或执行部署数据库。
