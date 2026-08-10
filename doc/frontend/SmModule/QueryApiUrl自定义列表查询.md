# QueryApiUrl 自定义列表查询

动态列表的数据源由模块元数据统一决定：

```text
QueryApiUrl 有值 → 请求 QueryApiUrl
QueryApiUrl 为空 → 请求 /api/Common/QueryByFilter/{moduleCode}
```

选择逻辑集中在 `src/api/modules/module.ts`，ProTable 通过 `useProTableData` 传入模块元数据中的 `queryApiUrl`。业务页面不得按 `moduleCode` 硬编码查询地址，也不得自行创建请求实例。

自定义接口必须继续接受现有 `QueryFilter`，并返回 ProTable 使用的分页字段：

```json
{
  "status": 200,
  "success": true,
  "message": "查询成功",
  "current": 1,
  "pageSize": 20,
  "total": 0,
  "pageCount": 0,
  "data": []
}
```

`QueryApiUrl` 只能是以 `/api/` 开头的站内相对路径。模块维护表单在动态字段尚未配置时提供临时输入项；后续增加同名 `SmModuleColumn` 后，动态表单字段拥有显示职责，临时项会自动隐藏。

