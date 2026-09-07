# 通用 BusinessQuery 项目适配

> 当前临时状态：按用户明确要求，所有环境均暂停项目模块权限和公司范围校验，原实现已注释保留。此状态随本次代码部署生效，不是 Development 专用开关。可调用工具的用户可以查询目录内所有公司的有效、未删除记录，包括公司为空的记录。身份认证、租户匹配、签名、防重放、字段白名单、状态过滤、币别分组、配额和审计仍然启用。

## 临时停用与恢复

暂停点使用 `TEMP-PROJECT-AUTH` 标记：

- `BusinessProjectCallerResolver.ResolveAsync` 中，读取有效用户/角色、模块授权、模块主表一致性和公司授权的整段原代码用块注释保留，临时返回不含公司范围的调用者。权限读取器及其 DI 注册未删除。可信用户 GUID、能力标记及租户匹配仍校验。
- `BusinessQueryPolicy.TryBuildScope` 对设置 `projectModuleCode` 的实体暂时返回空范围；非项目目录的租户范围机制不受影响。目录仍保留原模块绑定、公司 Scope 字段和结构校验，方便恢复。
- 恢复时取消解析器内原代码的注释，移除临时返回与策略中的项目范围旁路，恢复模块/公司拒绝测试并重新部署。应同时恢复两处，不要只恢复其中一处。
- 项目查询的审计规则标记为 `scope.project-disabled`，不声称已注入公司范围；恢复权限时将对应规则选择恢复为原 `AllowRuleIds`。

本次同时修复调用者解析期间取消漏写审计：先用 `CancellationToken.None` 写入 `cancelled / BUSINESS_QUERY_CANCELLED` 终态，再抛出原取消异常；若审计失败，报告 `BUSINESS_QUERY_AUDIT_UNAVAILABLE`。此阶段尚未预留配额，不进行配额结算。

策略检查与配额预留阶段也使用相同的取消终态处理。策略/配额依赖异常记录 `failed / BUSINESS_QUERY_POLICY_UNAVAILABLE` 并返回失败，不执行业务 SQL；审计写入失败则返回 `BUSINESS_QUERY_AUDIT_UNAVAILABLE`。尚未取得配额预留 ID 时不进行结算；如果存储端已写入预留但调用端未取得结果，该记录依靠既有 TTL 失效，短时间内可能占用配额，不能宣称已立即释放。

## 定位与范围

### 销售订单展示名称与空金额

- 销售项目目录（`SD_SALES_ORDER_MNG`）在聚合完成后，由 `ProjectBusinessQueryPresentation` 对结果中已有的客户／币别 ID 批量补充显示名称。
  查询固定使用 `BdCustomer.CustomerName`、`BdCurrency.CurrencyName`，与业务查询共用已选择的数据库；只读取有效、未删除记录，不允许模型指定表名或列名。
  这是展示字段扩展，不提供这些名称字段的筛选、分组或聚合能力；不修改原始分组键、行数、金额或 resultHash。
- 名称标记为不可信数据；去除控制字符并限制长度，Markdown 转义，React 按文本渲染。未匹配或名称为空时保留原 ID。
  查询异常不会被伪装成“查不到名称”，仍沿用查询失败和终态审计路径。名称读取也支持请求取消。
- 中文表头根据编译后的 LogicalField 确定，不猜测模型 resultKey 的含义，因此 `netAmountSum`、`totalNetAmount` 等别名都显示“未税金额”。
  未税金额、税额、含税金额按原金额口径展示，空值继续显示 `—`。
- 2026-09-07 对用户提供的客户／币别分组执行过参数化只读核查：有效且未删除订单 11 张，三项主表金额的非空记录数均为 0。
  本次未写数据库、未回填金额、未改用明细计算。该结果是核查时点的数据事实，不保证之后的数据状态。
- 本次展示扩展无需修改工具输入 schema 或目录哈希；重新构建并重启 MCP 后，新查询即可使用。历史 presentation 不自动重写。
  当前公司／模块权限仍按原要求停用；恢复时须连同名称查询的数据可见范围一起审查。

### 单次查询成功后的结束行为

主 Agent 直接调用 MCP 与委派子 Agent 两条路径均须登记经校验的业务结果。主路径通过
`BusinessQueryRunResultCollector` 检查已绑定工具、run/call 关联和回执，再加入执行作用域；
终态沿用已有会话消息持久化及 `business-query-result` 事件。不能仅保存 `tool-succeeded` 调试事件。
React 聊天页消费该结果事件渲染独立结果表格，以 queryId 去重；断线恢复从事件补取，历史会话沿用已有业务消息展示。
不依赖模型补写一段 Delta 文本来显示业务结果。旧运行若未保存业务结果消息，不会在此次更新中自动回填。

受控 BusinessQuery 继续保持每个 run 最多尝试一次。运行时通过内部
`AgentMcpToolCallLimit.CompleteAfterSuccess` 标记，仅在工具调用成功且结果大小／预算检查通过后，
设置 SDK `FunctionInvokingChatClient.CurrentContext.Terminate`，停止后续模型工具循环。
该标记默认关闭，普通 MCP 工具不受影响；不增加调用次数，不将工具失败转成成功。
服务层仍校验工具版本、调用关联、目录哈希及服务端回执，只有合法业务结果才被接受和展示。
金额为 null 的合法结果同样应正常结束，不应让模型通过重复调用来猜测金额。

三项销售金额现取 `SdOrderDetail.NoTaxAmount`、`TaxAmount`、`TaxIncludedAmount`：有效且未删除明细先按 `OrderId` 汇总，再 LEFT JOIN 订单并按请求维度聚合。明细 NULL 及无有效明细订单的金额补 0，不读取主表金额，不修改数据库数据。
显示 `—` 代表缺失值，不代表 0；本次没有改为明细汇总或执行数据回填。
核查应比较相同客户／币别、订单及明细 IsActive/IsDeleted 条件下的明细金额，不再用主表空金额作为核对基准。

该变更只需更新 Agent 运行时及其服务契约程序集，无需变更 MCP 工具 schema 或重新同步目录。
回滚时恢复调用限制工厂和运行时终止处理；旧的重复调用导致整轮失败行为也会恢复。

使用同一个 MCP 工具 `query_business_data`，通过受信任的语义目录接入 EU 业务模块，不为销售、采购等模块重复编写 MCP 工具。本次分类为 BACKEND-PLATFORM / BACKEND-HOST / API-CONTRACT / DATABASE；数据库标签来自映射和查询行为，未执行迁移或数据写入。

首版适用于使用 `CompanyId`、`IsActive`、`IsDeleted`，并启用角色数据范围的单主表模块。不是任意 SQL 执行器，不自动把页面的 FullSql、查询条件或数据库所有表暴露给模型。业务写入、审核、作废不属于本工具。

## 原授权链路（模块与公司部分暂时停用，恢复参考）

1. Agent 的 `HttpCallerContext` 从现有 `IUser` 取得用户和租户，给已认证调用者添加 `business.project.query` 能力标记。它只允许发起项目查询，不代表用户有任意模块的数据权限。
2. 现有签名上下文转发器把身份、能力、目录/工具哈希发给 MCP。传输层接受专用服务令牌或 MCP 宿主认证方案验证通过的项目登录 JWT；实际工具调用仍必须提供签名上下文，签名校验、防重放、审计流程不能由普通请求参数或登录 JWT 替代。Agent 同步时凭据别名优先，别名为空则转发已认证请求的 Bearer Token，详见 [Agent 统一认证](Agent统一认证.md)。
3. MCP 要求签名租户与部署 `BusinessQuery:TenantId` 完全一致。租户配置支持当前项目的数字字符串，例如 `7`，不会将其转换成公司 ID。

   当前 `SmUsersServices.GenerateJwtToken` 为项目登录签发 `TenantId = "0"`，因此项目 MCP 的
   `appsettings.json` 已将 `BusinessQuery:TenantId` 从演示值 `development` 对齐为 `0`。
   此值不是通配符，其他租户仍会被拒绝；未来切换真实多租户时，必须同时核对令牌签发和数据源隔离映射，不能直接接受任意请求租户。
4. 对设置 `projectModuleCode` 的实体，`BusinessProjectCallerResolver` 调用权限读取器，重新核对业务数据库中的有效用户、有效角色、角色模块授权和公司数据范围。
5. 模块必须唯一、有效、未删除且启用 `IsRoleDataScope`。模块 SQL 元数据的主表必须唯一并与目录物理表一致；只读取主表名称，不执行元数据 SQL。
6. 公司列表从 `SmRoleDataScope` 取得；无模块权限、空公司范围、空 GUID 或超过 100 个公司均拒绝。模型无法提供或覆盖范围。权限读取异常返回 `BUSINESS_QUERY_AUTHORIZATION_UNAVAILABLE`，不退回全库查询。
7. 授权成功后继续执行原有字段、聚合、范围、配额、SQL 编译和结果保护策略。

恢复权限后，权限读取不使用 Redis，因此不回填缓存；每次请求重新查询。用户/角色/角色关联/模块/范围记录均要求有效且未删除，可能比旧页面的历史过滤更严格。管理员也需要明确的角色模块和公司授权，不继承旧页面的 Admin 全库跳过逻辑。当前临时停用期间不执行这些数据库权限检查，也不限制公司范围，详见文首提示。

## 通用目录约定

### 当前统一项目目录

运行配置固定指向 [project.sqlserver.json](../../eu.core/EU.Core.MCP.Api/BusinessQuery/catalog/project.sqlserver.json)，
目录标识为 `eu-core-business-sqlserver`，revision 为 `1`。对应 MySQL 文件为
[project.mysql.json](../../eu.core/EU.Core.MCP.Api/BusinessQuery/catalog/project.mysql.json)。
同一个 `query_business_data` 同时暴露 `supplier` 和 `salesOrder`，按查询计划的 `entity` 选择主表，
不再为查询供应商或销售而切换 appsettings。两个实体之间未声明关联，不能混用字段或跨实体汇总。

- `supplier`：`BdSupplier` / `BD_SUPPLIER_MNG`，保留编码、名称、简称、税别和税率；税率仅支持 average/minimum/maximum，不支持数量统计或 sum。
- `salesOrder`：`SdOrder` / `SD_SALES_ORDER_MNG`，客户、币别取订单，金额取有效明细预汇总，详见下节。
- 两个实体均使用 `business.project.query` 能力、强制有效/未删除过滤，并保留公司 Scope 结构。供应商不再使用旧示例的 `business.supplier.*` 权限声明，查询范围也不包含无效/已删除记录。
- 原 `supplier.*.json`、`sales-order.*.json` 保留为单模块参考，当前运行配置不指向它们。

新增采购单时先确认主表、金额口径、字段映射、状态过滤和模块标识，再向统一目录的 `entities` 添加实体并补测试；
目前并未接入采购单，模型不得猜测 `purchaseOrder` 字段。现有引擎支持的同类聚合无需新增 MCP 工具。
修改现有统一目录时递增 revision、重新计算两个哈希并同步工具版本；目录路径保持不变。
当前查询仍一次选择一个根实体，并非任意跨模块 SQL 或自动发现全部业务表。

在现有目录实体中声明以下属性：

```json
{
  "projectModuleCode": "SD_SALES_ORDER_MNG",
  "requiredPermission": "business.project.query",
  "defaultScopeField": "salesOrder.companyId",
  "requiredBooleanFilters": { "IsActive": true, "IsDeleted": false },
  "requiredMeasureDimensions": ["salesOrder.currencyId"]
}
```

这是实体属性片段，不是完整目录。公司字段必须是映射到 `CompanyId` 的 string 类型 Scope；项目实体的所有字段使用 `business.project.query`，字段白名单决定当前模块查询允许返回的内容。不得把敏感字段随意纳入该白名单；模块授权不等于每个业务字段都应暴露。

保留能力标记不能用于没有模块绑定的实体。项目实体只能作为查询根，不能通过其他实体的关联绕过其模块和公司授权。旧的非项目目录继续使用原权限机制，不自动赋予 `business.project.query`。

`requiredBooleanFilters` 对根表及关联表都在输入侧强制过滤，且参数化；`requiredMeasureDimensions` 对聚合强制保留币别等维度。模型工具说明会提示必要分组，但最终由服务器校验，不依赖提示词保证安全。

布尔过滤最多 8 项，只接受安全的单段物理列名；显式 null 和大小写重复列名会被拒绝。规则复制进只读快照，目录规范化哈希包含这些约束。必要聚合维度必须属于本实体且可见，缺少时在配额预留前拒绝；仅提供币别筛选不能替代输出中的币别维度。

未声明新增属性的旧目录仍可加载，原 JSON 的哈希计算不变。非项目目录的部署租户范围只支持 `.tenantId` 结尾的字段，不能将历史上向任意范围字段填入部署租户 ID 的行为当成公司授权。

后续同类模块主要增加目录实体和测试；不同的数据隔离模型、字段级授权、跨模块关联或复杂业务指标需要扩展适配能力，而不是复制整个 MCP 工具。

## 销售订单首版

- SQL Server：[sales-order.sqlserver.json](../../eu.core/EU.Core.MCP.Api/BusinessQuery/catalog/sales-order.sqlserver.json)
- MySQL：[sales-order.mysql.json](../../eu.core/EU.Core.MCP.Api/BusinessQuery/catalog/sales-order.mysql.json)
- 模块：`SD_SALES_ORDER_MNG`；主表：`SdOrder`。
- 金额：有效未删除 `SdOrderDetail` 的 `NoTaxAmount`、`TaxAmount`、`TaxIncludedAmount`，按订单预汇总，NULL 补 0。固定销售源适配仅匹配 salesOrder / SD_SALES_ORDER_MNG / SdOrder，不改变供应商及非项目目录；后续其他主明细模块需要自己的适配，不自动套用销售表。
- 维度：币别 ID、客户 ID、订单编号、订单状态、审核状态；所有金额聚合必须包含币别 ID。
- 未提供币别/客户名称解析，不能把 GUID 当作人民币或自行编造名称；金额不硬编码 CNY。
- 暂未开放日期范围。当前引擎时间边界使用 UTC，而订单日期的实际存储语义尚未完成验证，不能直接用 UTC 边界比较本地业务日期。
- 项目业务实体（配置了 `ProjectModuleCode`）不再生成最小分组 `HAVING COUNT(*) >= MinimumGroupSize` 或度量非空数量限制，只有一张订单的分组也参与汇总。非项目统计目录继续保留小样本保护及原配置校验。状态过滤、币别分组、结果数量限制和审计不变。销售 NULL 金额现在补零；本次目录说明及哈希发生变化，需重启 MCP 和 Agent、重新同步并确认工具版本后新建查询，旧结果不重写。没有修改环境变量或凭据。

示例查询计划（不绕过权限，也不代表已联调通过）：

```json
{
  "entity": "salesOrder",
  "dimensions": ["salesOrder.currencyId"],
  "measures": [{ "field": "salesOrder.grossAmount", "aggregation": "sum", "resultKey": "grossTotal" }],
  "filters": [],
  "timeRange": null,
  "orderBy": [{ "field": "grossTotal", "direction": "descending" }],
  "limit": 10
}
```

## 连接、部署与回滚

本项目的 `BusinessQueryService` 使用已有仓储 `Db`，不是参考项目的独立凭据连接路径。本次对每次调用复制独立 SqlSugar 客户端，权限读取和业务查询使用同一个客户端，避免并发覆盖 ADO 超时/取消状态；实际数据库方言须与配置一致。

这不意味着仓储账户在数据库层已被限制为只读；查询代码只执行 SELECT，但部署仍应使用最小权限账户。`CredentialAlias`/`DataSourceCode` 元数据不会替你切换实际仓储数据库。首版是单部署租户绑定单业务库，不能用一个配置租户接收所有租户或宣称支持自动跨库路由。

上线前：

1. 确认部署者接受当前所有环境暂停模块/公司权限的范围扩展，使用明确的测试身份。恢复权限前再核对模块、角色和公司配置；不要为测试自动授予权限。
2. 先部署支持新目录属性的 MCP；按实际数据库方言选择统一项目目录，并核对 `DataSourceCode`、`Dialect` 和精确的 `TenantId`。目录含多个模块，不按用户问题切换配置。
3. 用 `BusinessSemanticCatalogLoader` 获取目录规范化哈希，再用 `BusinessQueryToolSchemaBuilder` 生成工具哈希；同步 MCP 的 `ExpectedCatalogHash` 与 Agent 的 `CatalogRevision`、`CatalogHash`、`ToolSchemaHash`。不要把文件原始字节 SHA 当作规范化目录哈希。
4. 已修改 MCP 的 `CatalogPath` / `ExpectedCatalogHash`，以及 Agent 的 `BusinessQueryForwarding:CatalogHash` / `ToolSchemaHash`，revision 仍为新统一目录的 1；MCP 的 TenantId 对齐当前登录签发值 `0`，未修改运行中工具注册数据。HTTP 保留服务令牌／登录 JWT 双认证；查询上下文签名现复用项目 JWT 配置，不再单独注入密钥，部署要求见 [Agent 统一认证](Agent统一认证.md)。
5. 重新编译并重启 MCP 和 Agent，确认 MCP Server 的 Code、Endpoint 与转发策略的 ServerCode、Origin 匹配；同步工具并完成 ReadOnly 分类，在 Agent Draft 中替换旧工具版本，保存后发布，再新建会话。历史发布版本不会自动更新。
6. 获得对审计、配额、防重放写入的确认后执行端到端测试。临时停用期间按目录内全部公司范围核对；权限恢复后再按同一用户获授权的公司范围核对。

回滚时先恢复之前批准的目录及配套哈希绑定，再回滚程序；也可先关闭 BusinessQuery 转发/宿主功能。不能通过移除模块绑定、公司范围或状态过滤来恢复可用性。旧二进制不支持新目录属性，会拒绝加载。

## 验证

### 销售订单历史只读核对（2026-09-07）

以下为此前经授权对 Web API 配置的 SQL Server 主库执行 SELECT 得到的时点快照，不是事务一致性快照，不代表当前登录用户的可见数量，也不能代替 MySQL 环境或后续运行状态的验证。本次文档合并没有重新连接数据库。

当时模块 `SD_SALES_ORDER_MNG` 使用 `/api/SdOrder`，未配置 `QueryApiUrl`；列表默认条件为 `A.IsActive = 'true' AND A.IsDeleted = 'false'`，读取主表 `A.*` 并关联客户、币别和结算方式。普通用户的公司范围由 `CommonServices` / `DataScopeHelper` 追加，旧页面管理员跳过公司过滤；MCP 的更严格行为见前述授权链路。

| 历史核对项 | 数量 |
|---|---:|
| 全部订单 | 24 |
| 未删除订单 / 有效且未删除订单（未施加公司权限） | 17 / 17 |
| 上述 17 张中公司为空 / 集团为空 | 12 / 12 |
| 上述 17 张中币别为空 / 订单日期为空 | 0 / 0 |
| 上述 17 张中没有有效且未删除明细 | 3 |
| 上述 17 张中主表未税金额 / 含税金额与明细汇总不一致 | 10 / 10 |

历史金额诊断先将有效未删除明细按订单聚合，再与主表比较；NULL 临时按 0 计算，绝对差大于 0.01 判为不一致。全表主表“未税金额 + 税额 = 含税金额”的同容差检查未发现不一致；全表有 2 张币别为空的订单，非空币别、公司、集团各有 1 个不同值。这些都是历史诊断结果，不是业务计算规则或当前数据结论。

历史上曾按用户要求只读主表金额。2026-09-07 用户进一步明确主表没有金额，改从明细获取并将 NULL 补零；当前口径以上文为准。该调整仅影响查询，不据此自动修复金额、公司或集团。

此前核对没有执行 DDL、数据修复、权限授予、迁移或种子写入，也未调用会回填 Redis 的权限助手及会写审计/配额/防重放记录的 MCP 接口。`ApplicationIntent=ReadOnly` 仅是连接意图，不证明账户权限已只读。初次配置解析异常曾意外输出敏感配置，后续已屏蔽异常正文；本文不保留凭据，分类和处置由项目所有者决定。

代码事实源：[销售订单服务](../../eu.core/EU.Core.Services/SD/SdOrderServices.cs)、[列表与公司过滤](../../eu.core/EU.Core.Services/CommonServices.cs)、[原公司权限助手](../../eu.core/Src/EU.Core.Common/Helper/DataScopeHelper.cs)、[签名上下文转发](../../eu.core/Src/EU.Core.Agent.Infrastructure/Mcp/BusinessQueryContextTokenProvider.cs)。

### 自动化与运行时验收

离线测试不启动宿主、不连接数据库或 Redis；权限读取使用测试替身，不能替代真实 SQL 权限联调。

源码配置已切换到包含供应商与销售的统一目录。离线验证覆盖两个根实体在 SQL Server/MySQL 下的独立编译且不抑制小分组、非项目目录保留小分组保护、禁止混用字段/度量、销售必要币别分组、禁止税率求和、未配置采购实体拒绝及宿主哈希一致性。此次编译器变更未重启运行中服务；运行验收还需覆盖小分组完整返回、状态过滤和取消终态审计；真实公司授权与跨公司拒绝待恢复权限后验收。不纳入明细一致性检查。

```powershell
dotnet test eu.core/Src/EU.Core.Tests/EU.Core.Tests.csproj -c Release --filter "FullyQualifiedName~BusinessQuerySalesBoundaryTests|FullyQualifiedName~BusinessProjectCallerTests|FullyQualifiedName~BusinessProjectCatalogTests|FullyQualifiedName~HttpCallerContext_Should|FullyQualifiedName~BusinessQueryCancellationAuditTests|FullyQualifiedName~BusinessQueryAuthenticationTests"
dotnet build eu.core/EU.Core.sln -c Release
```

仅重算目录/工具哈希时可运行 `BusinessProjectCatalogTests.Unified_catalog_exposes_both_entities_in_one_tool` 并启用 `--logger "console;verbosity=detailed"`，测试调用实际 Loader/SchemaBuilder 输出非敏感哈希。`Host_configuration_pins_the_same_unified_catalog_and_tool` 会核对源码配置与 SQL Server 目录一致；新增字段后应更新配置，不应为通过测试跳过校验。

主要实现：[项目权限读取器](../../eu.core/EU.Core.MCP.Api/Services/BusinessQuery/Security/BusinessProjectAccessReader.cs)、[调用者映射](../../eu.core/EU.Core.MCP.Api/Services/BusinessQuery/Security/BusinessProjectCallerResolver.cs)、[查询服务](../../eu.core/EU.Core.MCP.Api/Services/BusinessQuery/BusinessQueryService.cs)。
