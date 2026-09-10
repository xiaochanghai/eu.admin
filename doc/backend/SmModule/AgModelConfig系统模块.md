# AgModelConfig 系统模块

> 状态：CURRENT（2026-09-10 按项目所有者确认的标准处理方式更新）
>
> 本次更新分类：DOCS。以下是后续维护约定，不表示本次修改了运行时代码或重新完成了联调。

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

[前端页面](../../../eu.admin.react/src/views/agent/modelConfig/index.tsx)复用通用 TableList，编辑维护沿用系统模块表单配置。073 脚本是初始只读列表配置；现库的按钮、表单和权限以模块管理中的实际配置为准。模型配置维护本身不会使数据库模型配置自动参与 Agent 运行。

## 当前新增与更新处理

宿主为 EU.Core.Api。新增使用 BaseController 的 `POST /api/AgModelConfig`，修改使用 `PUT /api/AgModelConfig/{Id}`，详情使用 `GET /api/AgModelConfig/{Id}`，请求和返回遵循基类及当前 DTO，不沿用旧方案的统一 PUT 新增接口。

当前 Service 的处理顺序（源码事实，不是对事务和安全性的验证结论）：

1. 新增：ConvertToEntity/InsertInput 转换 → CheckOnly → 设置 Provider 为 OpenAICompatible、两个修订号为 1、初始密文为空 → base.Add → 使用返回 ID 加密 API Key → 按指定字段更新 ApiKeyCiphertext。
2. 修改：转换输入 → 填写 API Key 时加密 → 从输入字典提取更新字段 → 调用基类 Update。

后续完善应继续采用这一标准 Service/基类协作方式。新增插入与密文更新的事务原子性、修改时 ApiKey 到 ApiKeyCiphertext 的更新字段映射、空密钥保留和修订号更新应在此链路内核对，不能因为文档记录了顺序就视为已验证。

## 密钥配置与安全边界

- ApiKey 是输入明文；ApiKeyCiphertext 是持久化密文，两者不能混用。密钥加解密复用现有 ModelConfigCredentialCipher，不承担独立数据存储职责。
- ModelConfig:EncryptionKey 来自部署环境 EU.Core.Api/appsettings.json，要求 32 字节安全随机数的 Base64 表示。不要在文档或仓库写入真实密钥；无需另建环境变量或 appsettings.Local.json。
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
