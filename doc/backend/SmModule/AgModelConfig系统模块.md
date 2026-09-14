# AgModelConfig 系统模块

> 状态：CURRENT（2026-09-10 按项目所有者确认的标准处理方式更新）
>
> 2026-09-10 接入分类：BACKEND-BUSINESS + BACKEND-HOST。Agent 模型目录和运行时已接入标准 Service；真实宿主及数据库联调仍需验证。

## 标准开发约定（后续必须遵循）

模型配置按照项目现有标准业务模块开发，不另建平行的存储、接口或管理体系。

```text
AgModelConfigController : BaseController<...>
  → IAgModelConfigServices : IBaseServices<...>
  → AgModelConfigServices : BaseServices<...>
  → IBaseRepository<AgModelConfig> / BaseRepository<AgModelConfig>
  → 现有 SqlSugar 数据访问
```

- [Controller](../../../eu.core/EU.Core.Api/Controllers/AG/AgModelConfigController.cs) 保持继承通用 BaseController，使用现有 HTTP、响应包装和认证边界；不为普通增删改查重新设计专用接口。
- [IService](../../../eu.core/EU.Core.IServices/AG/IAgModelConfigServices.cs) 继承通用 IBaseServices，沿用实体、Base DTO、InsertInput、EditInput、View DTO 的标准分层。
- [Service](../../../eu.core/EU.Core.Services/AG/AgModelConfigServices.cs) 继承通用 BaseServices，通过构造函数注入 IBaseRepository 并赋给 BaseDal。
- 标准能力直接复用基类；模型配置特有的转换、唯一性校验、默认值及密钥处理放在本 Service 对应的 Add/Update 重写方法中，继续通过基类方法持久化。
- **后续不得为本模块新增、恢复或扩展 `ModelConfigStore`、`IModelConfigStore`，也不得以其他名称重复包装一套同等职责的 Store/Repository。** 仓库中遗留文件的存在不代表它们是当前标准链路。
- 不以 `AgModelConfigServices1` 或独立维护 Service 建立第二套生产入口。需要修正业务行为时，在当前 AgModelConfigServices 中完成，公共缺口先检查既有 BaseServices/BaseRepository。
- Service 和 Repository 沿用现有 Autofac 程序集自动注册；不要再把 AgModelConfigServices 加入 ManuallyRegisteredServiceTypeNames，也不要额外在 Program.cs 重复手动注册。
- 前端沿用 TableList 与系统模块动态表单元数据。当前入口没有启用 DynamicFormPage；旧的专用 FormPage/API 文件不能作为必须恢复的实现。
- 带框架生成标记的文件如需调整，限制在本模块并保留手写业务重写；再次生成时必须核对 Add/Update，避免覆盖定制逻辑。

该约定约束的是实现分层和复用方式，不豁免认证授权、事务、日志脱敏或密钥安全要求；发现缺陷应在标准链路内修复，而不是引入第二套 Store。

## 模块配置

| 项目 | 值 |
| --- | --- |
| 模块名称 | 模型配置 |
| 模块代码 | AG_MODEL_CONFIG_MNG |
| 父模块 | AG_AGENT_MNG（智能体管理） |
| 模块 ID | c37d1814-0ba2-47df-9428-51c71f86a1f0 |
| 业务表 | AgModelConfig（必须已存在） |
| 路由 | /agent/model-config |
| 页面 | /agent/modelConfig/index |

[SQL Server 部署脚本](../../../eu.core/EU.Core.Api.Agent/Database/Migrations/SqlServer/073_add_model_config_admin_module.sql)适用于本次核对的 SQL Server 主库；不是 MySQL 通用迁移。脚本仅创建 SmModules、SmModuleSql、SmModuleColumn 数据，在事务内执行；同一模块重复执行直接退出，不覆盖后续人工维护。冲突或缺少业务表、父模块时失败。

[前端页面](../../../eu.admin.react/src/views/agent/modelConfig/index.tsx)复用通用 TableList，编辑维护沿用系统模块表单配置。073 脚本是初始只读列表配置；现库的按钮、表单和权限以模块管理中的实际配置为准。Agent 接入方式见下一节。

## Agent 模型目录与调用接入

- `GET /api/platform/capabilities` 的 `ModelProfileIds` 字段结构不变，改为由 `AgModelConfigServices.ListAvailableProfilesAsync` 读取启用、未删除的合法 `ProfileCode`。Agent 编辑页、模型裁判选择器继续消费该字段，无需新增前端请求。
- Agent 草稿、发布版本及运行快照中的 `ModelProfileId` 对应数据库 `ProfileCode`，不是记录 ID，也不是供应商 `ModelName`。迁移时保留已有引用所使用的 ProfileCode，避免旧 Agent 引用失效。
- 草稿保存、发布和模型引用检查使用数据库目录。运行及模型裁判通过 `ResolveRuntimeProfileAsync` 读取实际 ModelName、Endpoint、TimeoutSeconds 和密钥；聊天使用 EnableThinking（仅 Qwen），模型裁判保持其原有供应商默认思考行为。
- `AgModelProfileCatalog` 仅为 Agent 宿主的作用域适配：单例运行器每次创建作用域调用自动注册的 `IAgModelConfigServices`，无数据库操作、无缓存、无第二套 Store。业务查询继续复用 BaseServices/SqlSugar。
- 禁用、删除、重复标识、错误密钥、不符合基础 URL 规则的地址均拒绝新调用，不静默回退到 `.env`。正在执行的请求保持已经解析的配置；数据库修改在下一次运行生效。
- 目录读取不解密密钥；运行时配置的 ApiKey 同时对 Newtonsoft.Json 和 System.Text.Json 隐藏，不进入 Agent 快照、公开目录或 AOP 返回值日志。管理 CRUD 的既有安全边界仍须按下文核对，不等于已完成维护接口的完整安全审计。

部署顺序：

1. 在模型配置模块中维护启用的记录。Provider 为 `OpenAICompatible`，ProfileCode 使用合法公开标识，TimeoutSeconds 为 5–600 秒。
2. 保存记录前，由部署侧在 Redis DB 9 的 Hash `<Redis:InstanceName>ModelConfig` 中配置 `EncryptionKey` 字段。EU.Core.Api 与 Agent 宿主必须使用同一 Redis 实例及键前缀；前缀直接拼接，未配置时为 `nc`。两个宿主的 appsettings.json 均无需配置 ModelConfig.EncryptionKey。已有密文必须使用原主密钥，不得在迁移存储位置时直接换成新值。
3. Endpoint 必须是无用户名密码、查询参数、fragment 的非回环 HTTPS 地址。2026-09-11 按项目所有者要求移除模型专用 `ModelConfig:AllowedHosts` 校验，不再依赖旧 `AgentPlatform:ModelEndpoint` 授权主机。模型配置由可信管理员维护；填写错误或恶意地址可能导致凭据外发，HTTPS 基础校验不等同于完整 SSRF 防护，部署网络仍应限制内部地址访问与 DNS 重绑定。`AgentMcp:AllowedHosts` 不受影响。
4. 重新编译并重启 Agent 宿主，刷新 Agent 编辑页，检查可选模型来自数据库。已有 Agent 的 ProfileCode 无需修改；如换用新标识则重新保存、发布。
5. 新建对话验证实际调用。数据库没有启用模型时目录为空，不能再依赖 `AgentControl:ModelProfileIds` 提供兜底。旧环境变量解析实现仍保留兼容其他运行时消费者，但当前 Agent 宿主的聊天及裁判路径均使用数据库解析器。

本次不修改 appsettings 中的真实值、不访问数据库、不迁移既有环境变量里的 API Key。沿用已有主密钥。若回退本次接入，恢复上一版 Agent 宿主及其原模型配置即可；不删除数据库记录或轮换密钥。

验证记录：解决方案 Release 编译通过（存在既有警告）；37 项定向离线测试通过（含 22 项新增模型配置接入测试、思考参数、宿主配置校验、运行日志依赖及 BusinessQuery 单次调用回归）；React 类型检查通过。新增测试使用内存仓储和真实业务 Service，未连接数据库或模型服务。未运行完整集成测试；真实主密钥一致性、部署白名单、管理端新增/修改后再调用及浏览器端到端流程需要部署联调。

## 当前新增与更新处理

宿主为 EU.Core.Api。新增使用 BaseController 的 `POST /api/AgModelConfig`，修改使用 `PUT /api/AgModelConfig/{Id}`，详情使用 `GET /api/AgModelConfig/{Id}`，请求和返回遵循基类及当前 DTO，不沿用旧方案的统一 PUT 新增接口。

当前 Service 的处理顺序（源码事实，不是对事务和安全性的验证结论）：

1. 新增：ConvertToEntity/InsertInput 转换 → CheckOnly → 设置 Provider 为 OpenAICompatible、两个修订号为 1 → 生成服务端 ID、完成 API Key 加密 → `base.Add(model, id)` 一次写入。密钥缺失或加密失败时，不再先插入空密文记录。
2. 修改：按路由 ID 查询未删除记录 → 转换输入并强制使用路由 ID → 从更新列中排除明文、客户端密文及修订号 → 非空 API Key 加密后显式加入 ApiKeyCiphertext/CredentialRevision 更新列 → 递增 LogicalRevision → CheckOnly → 调用基类按列 Update。空白或未提交 API Key 时保留原密文及凭据修订号；目标不存在、已删除或没有可更新字段时返回失败。

后续完善继续采用这一标准 Service/基类协作方式。本轮修正了单次新增持久化、ApiKey 到密文的字段映射、空密钥保留及服务端修订号递增。修订号递增目前不是带条件的乐观并发更新，不能宣称已防止并发覆盖；其它继承的批量/DTO 重载、动态模块的表单校验与日志脱敏也不属于本次验证覆盖。

本轮补充验证（BACKEND-BUSINESS + DATABASE，无表结构或真实数据库变更）：新增/更新后通过实际运行配置解析方法读取 API Key；错误主密钥、空 API Key、新增加密失败不留半成品；路由主键覆盖输入主键；客户端密文/修订号不参与直接更新；空密钥保留、缺失/删除记录拒绝更新。测试仓储为内存替身，不调用真实数据库、Redis 或模型服务。

保存链路修正后，相关定向测试共 48 项通过（模型配置 33 项，其余回归 15 项），解决方案 Release 编译通过（63 条既有警告、0 错误），`git diff --check` 通过。本轮未改前端或公开请求/响应字段，未重复前端类型检查；上一轮类型检查已通过。尚未执行真实动态表单、数据库及模型调用联调。

## 密钥配置与安全边界

- ApiKey 是输入明文；ApiKeyCiphertext 是持久化密文，两者不能混用。密钥加解密复用现有 ModelConfigCredentialCipher，不承担独立数据存储职责。
- 主密钥来自 Redis DB 9 的 Hash `<Redis:InstanceName>ModelConfig`、字段 `EncryptionKey`，要求 32 字节安全随机数的 Base64 表示，存储原始字符串，不附加 JSON 引号。不要在文档或仓库写入真实密钥。
- Service 的新增、更新 API Key 和运行时解密统一通过 `await Redis.GetAsync("ModelConfig", "EncryptionKey")` 读取主密钥，不回退到 appsettings。Service 构造函数注入 IBaseRepository 和已有 IRedisCacheServiceFactory，通过工厂创建 DB 9 的实例，不再使用静态缓存或全局服务定位器；两个宿主沿用 AddCacheSetup 的工厂注册。模型地址直接使用 AgModelConfig.Endpoint，不读取模型主机白名单。
- 主密钥为平台共享配置，不按租户或用户隔离；不得设置过期时间，需防止缓存清空或淘汰并配置持久化、备份和访问控制。每次加解密重新读取，不续期；Redis 不可用、字段缺失或格式错误时操作失败，不生成替代密钥。迁移前先由所有者确认原值并通过安全渠道配置 Redis，再部署代码；回滚须恢复旧代码及部署侧相同主密钥配置。
- AgModelConfigAgentIntegrationTests 使用真实 RedisCacheServiceFactory/RedisCacheService 配合内存 IConnectionMultiplexer、IDatabase 代理，断言 DB 9、键前缀和 EncryptionKey 字段。每个 fixture 独立持有随机主密钥，不读写全局 AppSettings，不连接真实 Redis、数据库或模型服务；无效/缺失主密钥、读取失败及实例隔离均有负向验证。定向测试 40 项通过；真实 Redis 联调仍需在授权的隔离环境中执行。
- 主密钥必须备份，不得直接更换导致已有密文失效；历史密文格式迁移需要单独处理。
- 自动注册可能启用服务日志 AOP；API Key 的输入日志、SQL 参数及详情/导出返回均需防泄露。不得把“恢复自动注册”理解为允许记录密钥。
- 权限沿用项目现有请求与模块边界；不将旧专用 Service 的权限检查、DTO 隐藏字段或乐观并发能力宣称为当前生成式链路已经具备。

## 旧方案与脚本适用范围

此前 ModelConfigStore、AgModelConfigServices1、ModelConfigContracts、独立 FormPage 及统一 PUT 接口属于旧方案，不作为后续扩展依据。2026-09-10 按项目所有者要求，已删除 AgModelConfigServices1.cs、ModelConfigStore.cs（含 IModelConfigStore）及其 ModelConfigMaintenanceTests.cs、ModelConfigStoreTests.cs 两个旧测试文件。当前生产 Service 和 ModelConfigCredentialCipher 保留不变；其余遗留文件未在本次清理范围内。

[073 脚本](../../../eu.core/EU.Core.Api.Agent/Database/Migrations/SqlServer/073_add_model_config_admin_module.sql)保留初始模块配置；[074 脚本](../../../eu.core/EU.Core.Api.Agent/Database/Migrations/SqlServer/074_enable_model_config_maintenance.sql)指向旧专用 FormPage，**不能作为当前标准动态表单的直接部署步骤**。后续若需执行，应先按当前页面、DTO、模块表单元数据核对并修正，再经授权执行。

此前 15 项定向测试及前端类型检查是旧实现阶段的历史结果，不代表当前 AgModelConfigServices 重写方法已通过相同验证。上述旧测试已按要求删除，其中的加密测试本次也未迁移；当前生产链路的定向测试覆盖仍需后续补充。

## 初始部署记录（历史）

2026-09-09 已按用户授权写入当前配置的 SQL Server 主库：1 个模块、1 条查询配置、12 个栏位。正式写入前通过事务回滚试执行，写入后重复执行验证未新增重复记录。未查询模型配置中的凭据内容，未改动 AgModelConfig 结构或业务数据。

上述部署阶段的前端类型检查通过；当时未做浏览器端到端验证，也未代为分配角色权限或执行全局缓存清理。这不是本次文档更新的运行时验证。

启用步骤：

1. 部署前端页面（开发服务通常由 Vite 自动发现）。
2. 在系统模块/角色管理中为目标角色分配该模块及查询权限；不复制其他角色的整套权限。
3. 使用现有系统“清理缓存”功能刷新 ModuleInfo、ModuleSql、ModuleSqlColumn 及用户菜单权限缓存；随后重新登录，重新加载前端模块元数据。
4. 打开“智能体管理 → 模型配置”，验证权限、查询和空数据状态。

## 回退

优先在模块管理中停用本模块，并刷新上述缓存。若需要彻底移除，先核对并撤销后来分配的角色/功能引用，再以固定模块 ID 为边界，在事务内依次删除 SmModuleColumn（SmModuleId）、SmModuleSql（ModuleId）、SmModules（ID）。不要删除或清空 AgModelConfig 业务表。元数据删除前应通过模块导出功能备份人工维护内容；本次初始配置可由部署脚本重建。
