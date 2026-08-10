# SmModule 前端文档索引

> 文档角色：动态页面元数据前端消费的渐进式披露入口  
> 适用范围：`eu.admin.react/**` 中的模块 API、Redux、通用表格/表单及模块维护页面  
> 最后核对：2026-08-10

## 按任务阅读

| 任务 | 必读 | 继续核对 |
|---|---|---|
| 使用模块元数据开发动态页面 | [动态页面元数据消费约束](动态页面元数据消费约束.md) | `src/api/modules/module.ts`、`src/redux/modules/module.ts`、通用组件和目标页面 |
| 选择或新增表单 `FieldType` | [表单控件类型](表单控件类型.md) | `CompDatas.tsx`、`Elements/Index.tsx`、`fieldSettingSchema.tsx` 和具体控件实现 |
| 修改 `ModuleInfo`、栏位或按钮契约 | [动态页面元数据消费约束](动态页面元数据消费约束.md)、[后端 SmModule 索引](../../backend/SmModule/README.md) | 后端组装、前端类型、缓存和全部消费者 |
| 修改模块维护页面 | [动态页面元数据消费约束](动态页面元数据消费约束.md)、[后端维护约束](../../backend/SmModule/元数据模型与维护约束.md) | `src/views/system/privilege/module/**`、保存接口和缓存刷新 |
| 配置自定义列表查询接口 | [QueryApiUrl 自定义列表查询](QueryApiUrl自定义列表查询.md) | `src/api/modules/module.ts`、`useProTableData` 和后端分页契约 |

## 文档边界

- 前端文档描述 HTTP 契约、Redux 生命周期和 UI 消费，不复制后端 SQL 与数据库字段说明。
- `SmModuleColumn.FieldType` 的可选值以 [`CompDatas.tsx`](../../../eu.admin.react/src/views/system/config/form/components/CompDatas.tsx) 为选择器事实源，并以通用渲染器实际映射做二次确认。
- 后端三表关系、写入不变量、脚本和数据库核对统一维护在 [`doc/backend/SmModule`](../../backend/SmModule/README.md)。
- 新增 SmModule 前端说明应进入本目录并更新本索引；`AGENTS.md` 只保留入口与跨模块强约束。
