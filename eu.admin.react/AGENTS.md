# EU Admin React Agent Index

本文件适用于 `eu.admin.react/**`，是 React 管理端的局部渐进式披露入口；根 [`AGENTS.md`](../AGENTS.md) 中的跨端契约、数据库、安全、工作区和交付约束继续生效。

## 1. 首先做什么

1. 将任务声明为 `FRONTEND` 或 `DOCS`；公开 API 变化附加 `API-CONTRACT`，同时修改服务端契约与本端消费者时再附加 `CROSS-END-CONTRACT`。
2. 阅读 [`package.json`](package.json)、相关入口、相邻模块和类型定义，确认真实脚本与依赖版本。
3. 按下表进入拥有模块；工作流任务先读对应 README，再读源码。
4. 沿页面 → 组件/Hook → 状态/API → 服务端契约追踪完整链路，不只根据 UI 猜实现。
5. 行为、路由、权限、接口、状态生命周期或构建配置变化时同步更新相关文档。

## 2. 必读路由

| 修改范围 | 必读/必查 |
|---|---|
| 应用启动、全局 Provider、插件 | [`src/main.tsx`](src/main.tsx)、`src/context/**`、`src/plugins/**` |
| API、请求封装、服务地址、错误处理 | `src/api/index.ts`、`src/api/base/**`、`src/api/config/**`、对应 API 模块 |
| 登录、用户、Token、权限 | `src/views/login/**`、`src/redux/modules/user.ts`、`src/redux/modules/auth.ts`、路由守卫 |
| 路由、动态菜单、页面访问 | `src/routers/**`、`src/redux/modules/module.ts`、对应 `src/views/**` |
| 布局、标签页、主题、国际化 | `src/layouts/**`、`src/redux/modules/tabs.ts`、`src/styles/**`、`src/languages/**` |
| 共享 UI、表单、上传附件 | `src/components/**`、相关 API 与业务页面 |
| 普通业务页面 | 对应 `src/views/<domain>/**`、`src/api/**`、相关 Redux 模块 |
| 动态页面、通用表格/表单、模块维护 | [`../doc/frontend/SmModule/README.md`](../doc/frontend/SmModule/README.md)，再按索引读取目标专题和源码 |
| 工作流运行与设计器 | [`src/workflow/README.md`](src/workflow/README.md)、[`src/workflow/WorkflowEditor/README.md`](src/workflow/WorkflowEditor/README.md)、[`src/workflow/setters/README.md`](src/workflow/setters/README.md) |
| 新版工作流编辑器 | [`src/workflow-editor/README.md`](src/workflow-editor/README.md)、`src/workflow-editor/**` |
| DSL、动态表单或模型转换 | `src/dsl/**`、相关工作流与表单组件、服务端输入输出模型 |
| 环境变量、代理、构建、PWA、压缩 | [`vite.config.ts`](vite.config.ts)、`.env*`、[`tsconfig.json`](tsconfig.json) |
| lint、格式化、提交规范 | `.eslintrc.cjs`、`.prettierrc.cjs`、`.stylelintrc.cjs`、`lint-staged.config.cjs` |

## 3. 前端固定不变量

- `src/api/**` 拥有 EU 后端业务 HTTP 边界；页面和组件不得另建 Axios 实例或散落服务地址、鉴权 Header 与响应解包逻辑。静态资源/版本探测和用户明确配置的第三方 API 可作为受控例外，但必须限制输入、响应、超时和错误处理，且不得复用 EU 登录凭据。
- 路由、菜单、按钮权限和用户会话是同一授权链路。不能只隐藏 UI 而仍允许未授权操作，也不能用前端判断代替服务端授权。
- 服务端响应、URL 参数、localStorage、上传文件、Markdown、DSL 和工作流数据均按不可信输入处理；渲染或执行前进行类型与边界校验。
- Redux、Context 和组件状态各守现有职责；Zustand 当前仅存在于依赖清单，除非已有拥有模块或任务明确批准，不得据此建立第二套全局状态体系。不创建万能 Store，也不为单一页面复制会话、请求或缓存状态。
- `src/workflow/**` 与 `src/workflow-editor/**` 是两套现存边界。修改前确认实际消费者，禁止凭名称批量同步或合并实现。
- 路由离开、用户登出、租户/账号切换、组件卸载时，必须清理 listener、timer、订阅、未完成请求和临时状态，避免旧结果写回新会话。
- `.env*` 中进入 Vite 客户端的值都会暴露给浏览器，不得放置服务端密钥；新增变量必须有明确环境语义和安全分类。
- 行为、接口、路由、权限、状态生命周期、环境变量或发布方式变化时必须同步文档和消费者。

## 4. 修改边界

- 页面逻辑按展示组件 → 业务组件/Hook → Redux/API → 服务端能力逐级提升；只有多个模块真正复用时才上移为公共能力。
- 优先复用现有 Ant Design、Pro Components、Hooks 和项目组件，不引入第二套 UI、路由、请求、表单或状态框架。
- TypeScript 类型必须与实际 JSON 一致。不新增无必要的 `any`、类型断言、`@ts-ignore` 或关闭规则来掩盖契约问题；历史类型债务不要求在无关任务中顺手清理。
- API 字段、大小写、可空性、枚举、分页或 `Data` 包装变化属于跨端契约，必须同步检查后端 Controller、DTO 和序列化结果。
- 修改公共组件、请求封装、路由守卫、全局状态或工作流核心时，列出所有已知消费者并做回归，不以单一页面通过代替全局验证。
- 不手工编辑 `node_modules/**`、`dist/**`、`.build/**` 或生成资源；不提交任务无关的依赖、锁文件或构建产物变化。
- 自动格式化和 autofix 只允许作用于任务拥有文件，不覆盖用户已有未提交改动。

## 5. 交互与质量要求

- 新增异步交互必须覆盖 loading、成功、空数据、失败、取消或重复触发；错误不能静默吞掉。
- 表单提交、删除、上传和批量操作应防止重复执行，并保持成功后的缓存、列表和详情状态一致。
- 新增页面或入口必须检查路由注册、菜单/权限来源、直接访问、刷新恢复及无权限场景。
- 样式遵循现有主题 Token 和布局体系，不硬编码会破坏暗色、紧凑、响应式或国际化布局的值。
- 可访问性至少保证语义化控件、键盘可操作、可见焦点、表单标签及非纯颜色状态表达。
- 性能优化必须有实际瓶颈依据；避免无边界缓存、重复请求、大对象进入全局 Store或在 render 中执行昂贵转换。

## 6. 验证与交付

最低门禁：

```text
pnpm type:check
git diff --check
git status --short
```

- 在 `eu.admin.react` 中运行命令。发布相关变化再运行对应的 `pnpm build`、`pnpm build:dev` 或 `pnpm build:test`。
- `lint:eslint`、`lint:prettier`、`lint:stylelint` 都会自动改写文件；运行前确认作用域，运行后复查 diff。
- 当前 `package.json` 没有 `test` 脚本，不得虚构测试已通过。关键逻辑应进行针对性验证，并准确记录自动化测试缺口。
- 引入 Vitest、Jest、Playwright 等测试工具链必须作为独立、可审查的工具链变更，说明范围、脚本、CI 接入和维护成本；不得由单个业务页面顺手引入。
- 跨端契约任务还必须完成后端编译/测试，并在必要时用真实本地响应验证字段与错误路径。
- 纯文档任务不要求业务 build，但必须验证相对链接、路径、命令、Markdown 和工作区状态。
- 最终说明列出任务分类、拥有路径、验证结果、未运行项和剩余风险；baseline failure 必须原样报告。
