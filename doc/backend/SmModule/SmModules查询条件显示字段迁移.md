# SmModules 查询条件显示字段迁移

> 状态：`CURRENT`（待目标环境执行）
> 最后核对：2026-10-05

本迁移为 `SmModules` 增加 `IsShowSearch`：是否显示列表工具栏中的查询按钮。数据库默认值为关闭（`false`/`0`），以保持现有模块页面不显示该按钮的行为；需要提供查询入口的模块再由模块维护数据设置为开启。查询区域是否展开仍由前端的临时交互状态控制。

仓库支持多种数据库。本文件仅给出 SQL Server、MySQL 8.0.16+ 和 PostgreSQL 的已核对语法。执行时只能选择目标数据库的一组语句；脚本不是幂等脚本，执行前应确认列与默认约束不存在。本仓库任务不会自动连接或修改数据库。

`SmModules` 实体、DTO 基类和 Service 文件带有框架生成标记。数据库迁移后如需反向生成，必须限制为 `SmModules`，并复核 `GetModuleInfo` 对 `isShowSearch` 的兼容输出未被覆盖。

## SQL Server

```sql
ALTER TABLE dbo.SmModules ADD IsShowSearch bit NOT NULL
    CONSTRAINT DF_SmModules_IsShowSearch DEFAULT (0);
```

回滚：

```sql
ALTER TABLE dbo.SmModules DROP CONSTRAINT DF_SmModules_IsShowSearch;
ALTER TABLE dbo.SmModules DROP COLUMN IsShowSearch;
```

执行前核对（预期 0 行）：

```sql
SELECT c.name AS ObjectName, 'COLUMN' AS ObjectType
FROM sys.columns AS c
WHERE c.object_id = OBJECT_ID(N'dbo.SmModules')
  AND c.name = N'IsShowSearch'
UNION ALL
SELECT o.name, o.type_desc
FROM sys.objects AS o
WHERE o.parent_object_id = OBJECT_ID(N'dbo.SmModules')
  AND o.name = N'DF_SmModules_IsShowSearch';
```

## MySQL 8.0.16+

```sql
ALTER TABLE `SmModules`
    ADD COLUMN `IsShowSearch` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否显示查询按钮';
```

回滚：

```sql
ALTER TABLE `SmModules` DROP COLUMN `IsShowSearch`;
```

执行前核对（预期 0 行）：

```sql
SELECT `COLUMN_NAME`
FROM `information_schema`.`COLUMNS`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_NAME` = 'SmModules'
  AND `COLUMN_NAME` = 'IsShowSearch';
```

## PostgreSQL

```sql
ALTER TABLE "SmModules" ADD COLUMN "IsShowSearch" boolean NOT NULL DEFAULT false;
```

回滚：

```sql
ALTER TABLE "SmModules" DROP COLUMN "IsShowSearch";
```

执行前核对（预期 0 行）：

```sql
SELECT column_name
FROM information_schema.columns
WHERE table_schema = current_schema()
  AND table_name = 'SmModules'
  AND column_name = 'IsShowSearch';
```

## 执行后核对

```sql
-- SQL Server
SELECT IsShowSearch, COUNT(*) AS RecordCount
FROM dbo.SmModules
GROUP BY IsShowSearch;

-- MySQL
SELECT `IsShowSearch`, COUNT(*) AS `RecordCount`
FROM `SmModules`
GROUP BY `IsShowSearch`;

-- PostgreSQL
SELECT "IsShowSearch", COUNT(*) AS "RecordCount"
FROM "SmModules"
GROUP BY "IsShowSearch";
```

迁移后，`GET /api/SmModule/GetModuleInfo/{moduleCode}` 会返回小写 `isShowSearch`。前端仅在该值为 `true` 时显示查询按钮；查询区域默认关闭，用户点击按钮后才展开或收起。模块配置由缓存提供，保存或 SQL 直接修改后必须按现有运维流程刷新模块缓存，并重新加载页面。
