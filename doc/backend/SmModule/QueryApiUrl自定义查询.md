# QueryApiUrl 自定义列表查询

> 状态：`CURRENT`  
> 最后核对：2026-08-10

## 字段职责

- `ApiUrl` 是模块资源基础地址，继续用于详情、新增、修改和删除。
- `QueryApiUrl` 是可空的列表查询地址。为空时，前端回退到 `/api/Common/QueryByFilter/{moduleCode}`。
- `QueryApiUrl` 只允许配置以 `/api/` 开头、最长 256 个字符的站内相对路径；禁止绝对 URL、查询字符串、片段、双斜线和父路径。

模块管理页面的推荐配置为：

```text
ModuleCode:  SM_MODULE_MNG
ApiUrl:      /api/SmModule
QueryApiUrl: /api/SmModule/QueryGrid
```

`GET /api/SmModule/QueryGrid` 接收现有 `[FromFilter] QueryFilter`，返回与动态 ProTable 一致的 `GridListReturn`。当前实现复用通用模块查询以保持筛选、排序、数据权限和分页结构兼容；模块管理的后续专属查询逻辑由 `SmModulesServices.QueryGrid` 承载。

## 数据库迁移

本仓库未提供可安全覆盖全部数据库类型的统一迁移链。以下脚本仅供 DBA 在确认目标数据库、Schema、表名大小写和同名字段不存在后人工执行；本变更不会自动连接或修改数据库。

### SQL Server

```sql
ALTER TABLE dbo.SmModules ADD QueryApiUrl varchar(256) NULL;

UPDATE dbo.SmModules
SET QueryApiUrl = '/api/SmModule/QueryGrid'
WHERE ModuleCode = 'SM_MODULE_MNG';
```

回滚：

```sql
ALTER TABLE dbo.SmModules DROP COLUMN QueryApiUrl;
```

### MySQL

```sql
ALTER TABLE `SmModules`
    ADD COLUMN `QueryApiUrl` varchar(256) NULL COMMENT '自定义列表查询地址';

UPDATE `SmModules`
SET `QueryApiUrl` = '/api/SmModule/QueryGrid'
WHERE `ModuleCode` = 'SM_MODULE_MNG';
```

回滚：

```sql
ALTER TABLE `SmModules` DROP COLUMN `QueryApiUrl`;
```

### PostgreSQL

```sql
ALTER TABLE "SmModules" ADD COLUMN "QueryApiUrl" varchar(256) NULL;

UPDATE "SmModules"
SET "QueryApiUrl" = '/api/SmModule/QueryGrid'
WHERE "ModuleCode" = 'SM_MODULE_MNG';
```

回滚：

```sql
ALTER TABLE "SmModules" DROP COLUMN "QueryApiUrl";
```

## 上线与回退

1. 先增加可空字段，不写配置。
2. 发布后端实体、模块元数据和 `QueryGrid` 接口。
3. 发布支持自定义查询地址及通用接口回退的前端。
4. 最后设置 `SM_MODULE_MNG.QueryApiUrl`。
5. 接口异常时将 `QueryApiUrl` 置空即可立即回退；删除字段只应在代码回滚后执行。

