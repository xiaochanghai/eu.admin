# Repository Agent Index

本文件是 EU-Admin 前后端分离仓库的 Agent 约束入口。开始任务时先定位范围与事实源，再读取相关代码、配置和文档；不要无差别扫描、修改或格式化整个仓库。

## 1. 仓库边界与任务路由

| 任务范围 | 首读 | 继续下钻 |
|---|---|---|
| React 管理端 | [`eu.admin.react/AGENTS.md`](eu.admin.react/AGENTS.md) | `package.json`、对应 API、路由、状态、组件和业务页面 |
| .NET Web API | [`eu.core/AGENTS.md`](eu.core/AGENTS.md) | `EU.Core.sln`、对应 Controller、Service、IService、Model、Repository |
| 后台任务 | [`eu.core/AGENTS.md`](eu.core/AGENTS.md) | `EU.Core.Jobs`、Tasks、调度配置、依赖服务和持久化边界 |
| 网关与 MCP | [`eu.core/AGENTS.md`](eu.core/AGENTS.md) | Gateway/MCP 各自配置、宿主代码和被调用服务 |
| 后端公共能力 | [`eu.core/AGENTS.md`](eu.core/AGENTS.md) | Common、Extensions、DataAccess、EventBus、Tasks、Serilog 等拥有模块 |
| 数据库与模型 | [`db`](db)、[`model`](model) | 实体、DTO、仓储、服务和所有调用方 |
| 前端开发文档 | [`doc/frontend/README.md`](doc/frontend/README.md) | 前端模块文档和源码旁 README |
| 后端开发文档 | [`doc/backend/README.md`](doc/backend/README.md) | 数据库、后端模块和宿主文档 |
| 通用文档与部署 | [`README.md`](README.md)、[`doc`](doc) | Docker、环境配置和实际启动入口 |

工作顺序：

1. 先执行 `git status --short`，识别并保护用户已有改动。
2. 声明任务类型和拥有路径，只读取完成任务所需的相邻实现与配置。
3. 以当前代码、类型、测试和运行配置为准；文档冲突时先核实再修改。
4. 行为、接口、数据库结构、配置或部署方式变化时，同步更新消费者和文档。
5. 交付前运行与风险匹配的检查，如实说明未运行项和既有失败。

## 2. 任务分类

- 仅修改 `eu.admin.react/**`：`FRONTEND`。
- 仅修改具体后端业务链路：`BACKEND-BUSINESS`。
- 修改 Common、Extensions、DataAccess、EventBus、Tasks、Serilog 等跨业务基础能力：`BACKEND-PLATFORM`。
- 修改 Web API、Jobs、Gateway 或 MCP 的宿主启动、全局中间件及宿主专属生命周期：`BACKEND-HOST`。
- 修改代码生成器、模板或生成流程：`TOOLING/CODEGEN`；只修改测试基础设施：`TESTS`。
- 任何公开 API 契约变化：附加 `API-CONTRACT`；同时修改仓库内前端消费者时再附加 `CROSS-END-CONTRACT`。
- 修改表结构、初始化数据、SQL、数据库包或持久化映射：附加 `DATABASE`。
- 只修改 `AGENTS.md`、`README.md` 或 `doc/**`：`DOCS`，不得声称运行时行为已经变化。

任务越过原范围时必须重新分类并扩大验证范围。一个任务可以同时拥有多个标签；不得用 `FRONTEND` 或 `DOCS` 规避后端、契约或数据库检查。

## 3. 前后端拥有边界

- 前端应用位于 `eu.admin.react/**`，后端解决方案位于 `eu.core/**`。不要让后端依赖前端源码或构建产物，也不要把后端实现复制到前端。
- 前端业务优先在现有 `src/api`、`src/views`、`src/components`、`src/routers`、`src/redux`、`src/hooks` 和 `src/utils` 职责内落位；先复用相邻模式，不重复建设请求封装、状态容器或通用组件。
- 后端保持现有分层：Controller 负责 HTTP 边界，`EU.Core.IServices` 定义服务契约，`EU.Core.Services` 承载业务逻辑，`EU.Core.Repository` 负责数据访问，`EU.Core.Model` 承载实体、DTO 和输入输出模型。
- 普通业务 Controller 不直接拼装数据库访问；DbFirst/代码生成等工具型接口按自身边界处理。前端页面不绕过 `src/api` 散落 EU 后端业务请求；受控例外见前端局部约束。
- 修改 Common、Extensions、DataAccess、认证授权、缓存、事件总线、日志或任务基础设施属于平台级变更；修改 Gateway、MCP、Jobs 或 Web API 启动属于宿主级变更。两者都必须检查已知调用方，不能作为单一业务补丁顺手修改。
- 遇到“代码由框架生成”文件时，先确认生成器、模板和覆盖范围。不得批量重新生成无关文件；直接修改生成结果时应提示后续再生成风险。

## 4. 跨端 API 契约

修改路由、HTTP 方法、认证策略、请求参数、DTO、字段名称/大小写、类型、可空性、枚举、分页结构、错误码或响应包装时，必须声明 `API-CONTRACT`；同时修改仓库内前端消费者时附加 `CROSS-END-CONTRACT`。

- 服务端事实源包括 Controller、输入/输出模型、服务接口和序列化配置；前端消费者包括 `src/api/**`、类型、页面、状态管理和工作流模块。
- 同一任务内同步更新仓库内所有生产者和消费者，不得只修改 TypeScript 类型而与实际 JSON 不一致；仓库外消费者必须在交付中列明影响。
- 普通业务 Web API 延续现有 `ServiceResult`/数据包装；流式接口、文件响应、MCP 等特殊协议延续所属宿主和相邻接口约定。
- 破坏性变更必须列出兼容性、调用方、上线顺序和回滚方式；可以兼容演进时，优先先扩展后移除。
- 契约变更至少验证后端编译/测试与前端类型检查，关键交互应补充或更新测试。

## 5. 前端约束

- 使用项目现有 React、TypeScript、Vite、Ant Design、Redux Toolkit 和 React Router 体系；Zustand 目前仅存在于依赖清单，除非已有拥有模块或任务明确批准，不得据此新建第二套全局状态体系。
- 保持类型明确，不新增无必要的 `any`、非空断言或关闭检查来掩盖契约问题；历史类型债务不要求在无关任务中清理。外部数据在边界处处理空值和异常。
- 遵循 [`eu.admin.react/.editorconfig`](eu.admin.react/.editorconfig)、ESLint、Prettier 和 Stylelint，只格式化任务拥有文件。
- `lint:eslint`、`lint:prettier`、`lint:stylelint` 包含自动修复；运行前确认作用域，不得改写无关文件。
- 不手工编辑 `node_modules/**`、`dist/**`、`.build/**` 等生成产物，除非任务明确要求，不提交构建输出。
- 涉及路由、权限、菜单、标签页缓存、国际化或主题时，检查集中配置和完整生命周期，不添加页面级旁路。

## 6. 后端约束

- 保持异步调用链；I/O 路径不得以 `.Result`、`.Wait()` 同步阻塞。
- 认证、授权、租户隔离、数据权限、事务和审计属于请求边界。新增或修改接口时核对相邻实现，不得为通过测试绕过过滤器或授权特性。
- 使用依赖注入和现有抽象；不要在业务代码中临时创建数据库连接、缓存客户端、HTTP 客户端或全局服务定位器。
- 多数据库兼容是项目能力。新增查询、映射或 SQL 时检查 SqlSugar/EF Core/Dapper 路径，不默认只适配单一数据库，除非任务明确限定并写入文档。
- 配置来自现有 `appsettings*.json`、环境变量和配置对象。不得硬编码连接串、令牌、密码、生产地址或租户凭据。
- 不手工编辑 `bin/**`、`obj/**`、发布目录、日志、上传文件或其他运行时产物。

## 7. 数据库与持久化

- 数据库变更必须声明 `DATABASE`，同步检查实体、DTO、ORM 映射、仓储、服务、接口、前端类型和文档。
- 未经用户明确授权，不连接或修改任何数据库，不执行建库、迁移、数据修复、清表或种子写入。
- 不覆盖 `db/**` 的数据库包或 `model/**` 的设计文件。确需更新时先确认格式、来源和恢复方案，严格限制目标。
- SQL 必须参数化，不得拼接外部输入；批量更新、删除、租户数据或权限数据必须检查过滤条件和事务边界。
- 迁移应可审查、可重复执行或有明确幂等边界，并提供回滚/恢复说明；不依赖应用启动时的隐式破坏性变更。

## 8. 安全与配置

- `.env*`、`appsettings*.json`、部署脚本和日志可能含敏感信息。只读取任务必需字段，输出中不得回显密码、Token、连接串或个人数据。
- 不把真实凭据写入源码、测试、示例、文档或 Git 跟踪配置；示例使用明显占位符。
- 历史配置中疑似凭据属于待确认基线；不得擅自删除、轮换、公开或清理历史。安全任务应只报告位置与风险，由项目所有者决定分类和处置方式。
- 文件上传、下载和路径拼接必须校验大小、类型、文件名与目标路径，避免路径穿越和任意覆盖。
- 身份认证、密码、Token、Cookie、CORS、权限或租户边界属于高风险修改，必须做针对性负向验证并说明安全影响。

## 9. 工作区与变更纪律

- 用户已有已跟踪或未跟踪改动均属于用户；不得覆盖、删除、移动、暂存或顺手格式化。来源不明的变化先停下并报告。
- 只修改声明的拥有路径。发现公共缺口时先重新分类，不扩大业务补丁，不重构无关代码。
- 不执行破坏性 Git 或文件操作，不擅自切换分支、拉取、合并、提交或推送。
- 依赖升级、锁文件重写、批量生成和全仓自动修复必须是任务明确需要的改动，否则保持现状。
- 命令意外修改范围外文件时，先确认来源，只还原本任务明确产生的变化；不得还原用户原有修改。

## 10. 验证与交付门禁

- 前端类型检查：在 `eu.admin.react` 运行 `pnpm type:check`。
- 前端构建：在 `eu.admin.react` 运行目标环境对应的 `pnpm build`、`pnpm build:dev` 或 `pnpm build:test`。
- 前端 lint：确认不会改写无关文件后再运行；优先对任务文件做只读检查。
- 后端构建：运行 `dotnet build eu.core/EU.Core.sln`，或在范围明确时构建受影响项目。
- 后端测试：先按 [`eu.core/AGENTS.md`](eu.core/AGENTS.md) 区分纯单元测试与会连接外部基础设施或写数据的集成测试；后者只能在隔离环境和明确授权下运行。
- 跨端契约：后端编译/测试与前端类型检查缺一不可；必要时启动本地服务验证真实请求和响应。
- 文档任务：至少检查链接、路径、命令与仓库实际结构一致。

每次交付前执行 `git diff --check` 和 `git status --short`。最终说明必须列出任务分类、修改文件、已运行验证、未运行项及原因、已知风险；不得把环境失败或既有失败描述为通过。
