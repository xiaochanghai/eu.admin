# EU Core Agent Index

本文件适用于 `eu.core/**`，是 .NET 后端的局部渐进式披露入口；根 [`AGENTS.md`](../AGENTS.md) 中的跨端契约、数据库、安全、工作区和交付约束继续生效。

## 1. 首先做什么

1. 将任务声明为 `BACKEND-BUSINESS`、`BACKEND-PLATFORM`、`BACKEND-HOST`、`TOOLING/CODEGEN`、`TESTS` 或 `DOCS`；公开 API 变化附加 `API-CONTRACT`，同时修改前端消费者时再附加 `CROSS-END-CONTRACT`，持久化变化附加 `DATABASE`。
2. 阅读 [`EU.Core.sln`](EU.Core.sln)、[`build/common.targets`](build/common.targets)、目标项目文件、相邻实现与测试，确认项目引用、`.NET 10` 目标和真实宿主。
3. 按下表沿 HTTP/Host → IService → Service → Repository/DataAccess → Model/数据库追踪完整调用链。
4. 涉及认证、租户、事务、缓存、事件或后台任务时继续追踪横切能力，不能只修改表面调用点。
5. 行为、接口、模型、数据库、配置、宿主生命周期或部署方式变化时同步消费者和文档。

## 2. 必读路由

| 修改范围 | 必读/必查 |
|---|---|
| Web API 启动、中间件、DI、Swagger | [`EU.Core.Api/Program.cs`](EU.Core.Api/Program.cs)、`EU.Core.Api/Filter/**`、`Src/EU.Core.Extensions/**` |
| Controller 与 HTTP 契约 | `EU.Core.Api/Controllers/**`、对应 IService、Service、DTO/Input |
| 业务服务 | `EU.Core.IServices/<domain>/**`、`EU.Core.Services/<domain>/**`、相关仓储和模型 |
| 实体、DTO、输入输出模型 | `EU.Core.Model/Entity/**`、`EU.Core.Model/ViewModels/**`、`EU.Core.Model/Insert/**`、`EU.Core.Model/Edit/**` |
| 仓储、事务、ORM、连接 | `EU.Core.Repository/**`、`Src/EU.Core.DataAccess/**`、相关配置 |
| 登录、JWT、权限、数据权限 | `EU.Core.Api/Controllers/Authorize/**`、`Src/EU.Core.Common/Authorizations/**`、`Src/EU.Core.Extensions/Authorizations/**`、认证中间件 |
| 多租户 | `EU.Core.Model/Tenants/**`、`Src/EU.Core.Common/DB/TenantUtil.cs`、`EU.Core.Api/Controllers/Tenant/**`、对应服务 |
| 后台任务与 Quartz | [`EU.Core.Jobs/Program.cs`](EU.Core.Jobs/Program.cs)、`Src/EU.Core.Tasks/**`、业务服务与配置 |
| 网关、路由、Nacos | [`Src/EU.Core.Gateway/Program.cs`](Src/EU.Core.Gateway/Program.cs)、`Src/EU.Core.Gateway/**`、`Src/Ocelot.Provider.Nacos/**` |
| MCP API | [`EU.Core.MCP.Api/Program.cs`](EU.Core.MCP.Api/Program.cs)、其 Controllers、Services、Models 和 Extensions |
| 缓存、事件、日志、公共扩展 | `Src/EU.Core.Common/**`、`Src/EU.Core.EventBus/**`、`Src/EU.Core.Serilog*/**`、`Src/EU.Core.Extensions/**` |
| 代码生成 | `Src/EU.CodeGenerator/**`、目标生成文件及所有可能被覆盖的定制代码 |
| 自动化测试 | [`Src/EU.Core.Tests`](Src/EU.Core.Tests)、受测项目及测试配置 |

## 3. 后端固定不变量

- Controller 拥有 HTTP 输入、路由、授权和响应边界；IService 定义业务契约，Service 承载业务规则，Repository/DataAccess 承载持久化，Model 承载实体和传输模型。
- 普通业务 Controller 不直接执行数据库访问；Repository 不承担 HTTP 或页面语义；Model 不依赖 Controller。DbFirst/代码生成等工具型接口按自身边界处理，其他跨层例外必须说明并限制范围。
- 认证、授权、租户隔离、数据权限、事务和审计是业务正确性的一部分，不得为方便调用或通过测试而绕开。
- 普通业务 Web API 延续现有 `ServiceResult`、异常处理和序列化约定；流式响应、文件、MCP 等特殊协议延续所属宿主和相邻接口约定，不得在单个接口发明不兼容格式。
- Web API、Jobs、Gateway、MCP API 是独立宿主。配置、DI、生命周期和失败语义分别拥有，不能假定一个宿主中的注册会自动出现在其他宿主。
- `Src/EU.Core.Common/**`、`Src/EU.Core.Extensions/**`、`Src/EU.Core.DataAccess/**`、EventBus、Tasks 和 Serilog 等公共能力被多个项目消费，修改必须检查所有项目引用和启动路径。`Src/EU.Core.Tests/**`、`Src/EU.CodeGenerator/**`、Gateway 和 Ocelot Provider 分别属于测试、工具链或宿主/网关边界，不因位于 `Src` 自动归类为公共平台。
- I/O 保持异步调用链，不使用 `.Result`、`.Wait()` 等同步阻塞；后台循环和长期任务必须支持取消、停止、异常观测和资源释放。
- 多数据库支持是现有能力。公共查询、映射和迁移不得无说明地绑定单一数据库方言。
- 行为、接口、权限、租户边界、模型、数据库、配置或宿主生命周期变化时必须同步文档与消费者。

## 4. 业务模块修改边界

`BACKEND-BUSINESS` 默认只修改同一领域的：

- Controller、IService、Service；
- Entity、DTO、Insert/Edit Input；
- Repository 或查询；
- 针对性测试和相关文档；
- 明确需要的前端 API 消费者与数据库变更，此时重新分类。

以下变化默认升级为 `BACKEND-PLATFORM`：

- `Src/EU.Core.Common/**`、`Src/EU.Core.Extensions/**`、`Src/EU.Core.DataAccess/**`；
- 认证授权、租户解析、事务、缓存、日志、事件总线和任务基础设施；
- Host 启动、全局过滤器、中间件、序列化、Swagger、网关和服务发现；
- 基础 Repository/Service 泛型行为或跨领域公共模型。

平台能力必须说明通用性、所有消费者、兼容性、禁用/失败行为和回滚方案。若业务需求暴露平台缺口，应拆清职责，不把领域名称或特殊分支固化进公共层。

以下变化使用独立分类：

- Web API、Jobs、Gateway、MCP 的启动代码、宿主级配置、中间件和生命周期：`BACKEND-HOST`；
- `Src/EU.CodeGenerator/**`、生成模板、生成注册和生成流程：`TOOLING/CODEGEN`；
- `Src/EU.Core.Tests/**` 的测试公共设施、fixture 或环境装配：`TESTS`。

## 5. API、数据与安全边界

- 路由、HTTP 方法、授权、参数来源、DTO、字段名称/大小写、类型、可空性、枚举、分页、错误码或响应包装变化均属于契约变化。
- 契约变化必须搜索所有后端调用方和 `../eu.admin.react/src/api/**` 等前端消费者；优先兼容演进，破坏性变化须说明上线顺序和回滚。
- 数据库变化附加 `DATABASE`，同步检查实体、ORM 映射、仓储、服务、API、前端类型和文档。未经明确授权，不连接或写入任何数据库。
- SQL 必须参数化；批量更新、删除、租户数据或权限数据必须验证过滤条件与事务边界。公共实现需考虑受支持数据库差异。
- 配置通过 `appsettings*.json`、环境变量和强类型配置进入；不硬编码连接串、密码、Token、生产地址或租户凭据，不在输出中回显敏感值。
- 历史配置中疑似凭据属于待确认基线；不得擅自删除、轮换、公开或清理历史。安全任务只报告位置与风险，由项目所有者决定分类和处置。
- 上传、下载、压缩包、导入导出和路径拼接必须限制大小、类型、文件名和目标目录，防止路径穿越、资源耗尽和任意覆盖。
- 仓库存在大量带生成标记的 C# 文件。代码生成前必须定位生成器、模板、命令、输出清单和手写扩展点；事实源不明确时禁止批量生成。生成结果与手写扩展分别审查，并提示再次生成的覆盖风险。

## 6. 实现与测试要求

- 沿用依赖注入与现有抽象，不在业务路径临时创建数据库连接、缓存连接、`HttpClient` 或全局服务定位器。
- 新增异步接口应正确传播 `CancellationToken`（调用链支持时），明确超时、重试、幂等和重复提交语义。
- 缓存变化必须说明 key 范围、租户/用户隔离、失效时机、TTL、并发与回源失败行为；不得用缓存掩盖持久化一致性问题。
- 事件和任务处理必须明确至少一次/重复投递语义，保证必要的幂等性，并使异常可观测；不能静默吞掉失败。
- 测试覆盖成功、无权限/跨租户、无效输入、空数据、依赖失败和关键并发/事务边界；测试不得依赖生产凭据或污染共享数据库。
- 新增异步测试必须返回 `Task`，不得使用 `async void`。历史测试中的 `async void` 属于基线，不得复制；相关任务应在不扩大范围的前提下修正。
- 运行测试前先检查 fixture、构造函数和 `appsettings.json`，区分纯单元测试与 Redis、MongoDB、SQL、消息队列或外部 HTTP 集成测试。任何连接外部基础设施、写数据或使用非隔离配置的测试都不是默认安全命令。
- 项目未在解决方案级统一启用 Nullable 或 Analyzer；普通业务任务不得顺手改变全解决方案编译策略。确需统一时作为独立平台变更处理。
- 不手工编辑 `bin/**`、`obj/**`、日志、上传目录或发布产物，不提交任务无关的生成变化。

## 7. 验证与交付

最低门禁：

```text
dotnet build EU.Core.sln
git diff --check
git status --short
```

- 在 `eu.core` 中运行命令；范围明确的小改动可先构建受影响项目，但跨项目或交付前应覆盖解决方案。
- 测试命令按改动范围选择。只有确认目标测试不连接外部服务、不写共享数据时，才可直接运行针对性 `dotnet test`；全量 `dotnet test Src/EU.Core.Tests/EU.Core.Tests.csproj` 仅允许在依赖齐全的隔离环境或用户明确授权下运行。
- 宿主、配置或集成变化应启动对应的 API、Jobs、Gateway 或 MCP Host 做针对性验证；不要用 Web API 启动成功代替其他宿主验证。
- 跨端契约任务还必须在 `../eu.admin.react` 运行 `pnpm type:check`，必要时验证真实请求、响应和失败路径。
- 数据库相关测试优先使用隔离环境；任何会写入外部数据库的命令都必须事先获得明确授权。
- 纯文档任务不要求业务 build/test，但必须验证相对链接、路径、命令、Markdown 和工作区状态。
- 最终说明列出任务分类、拥有路径、验证结果、未运行项和剩余风险；baseline failure 必须原样报告，不得声称全绿。
