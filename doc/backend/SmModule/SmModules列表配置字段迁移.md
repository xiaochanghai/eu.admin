# SmModules 列表配置字段迁移

> 状态：`CURRENT`（待目标环境执行）  
> 最后核对：2026-08-10

本次为 `SmModules` 增加：

- `OptionPosition`：操作列位置，值为 `left` 或 `right`；现有数据默认 `left`。
- `IsAllowCustomColumn`：是否允许用户设置自定义列；现有数据默认允许。

仓库暴露多种数据库类型，下面只给出本次已核对语法的 SQL Server、MySQL 8.0.16+ 和 PostgreSQL 脚本。Oracle、SQLite、达梦和人大金仓等环境应由 DBA 按实际版本、Schema、大小写和布尔映射另行适配，不能直接套用。部署时只执行与目标环境匹配的一组语句；脚本不是幂等脚本，执行前应备份并确认同名列及约束不存在。本仓库任务不会自动连接或修改数据库。

仓库没有可用于本变更的有效 EF Core 迁移链：`Src/EU.Core.DataAccess/Migrations/DataContextModelSnapshot.cs` 当前为空模型，`db/**` 保存的是数据库包而不是可审查的增量 SQL。因此本次以本文件作为人工部署脚本，不生成可能误导的 EF Migration，也不修改数据库包。

`SmModules` 实体、DTO 基类和 Service 文件带有框架生成标记。目标数据库完成迁移后再运行代码生成器，数据库反向生成应能重新识别两个字段；但生成器还可能覆盖 Service 中的默认值、旧客户端兼容和 `GetModuleInfo` 组装逻辑。任何重新生成都必须限制到 `SmModules`，并在提交前逐项对比本次修改文件，不得批量覆盖其他模块。

## SQL Server

```sql
ALTER TABLE dbo.SmModules ADD OptionPosition varchar(8) NOT NULL
    CONSTRAINT DF_SmModules_OptionPosition DEFAULT ('left');
ALTER TABLE dbo.SmModules ADD IsAllowCustomColumn bit NOT NULL
    CONSTRAINT DF_SmModules_IsAllowCustomColumn DEFAULT (1);
ALTER TABLE dbo.SmModules ADD CONSTRAINT CK_SmModules_OptionPosition
    CHECK (OptionPosition IN ('left', 'right'));
```

回滚：

```sql
ALTER TABLE dbo.SmModules DROP CONSTRAINT CK_SmModules_OptionPosition;
ALTER TABLE dbo.SmModules DROP CONSTRAINT DF_SmModules_OptionPosition;
ALTER TABLE dbo.SmModules DROP CONSTRAINT DF_SmModules_IsAllowCustomColumn;
ALTER TABLE dbo.SmModules DROP COLUMN IsAllowCustomColumn, OptionPosition;
```

## MySQL 8.0.16+

```sql
ALTER TABLE `SmModules`
    ADD COLUMN `OptionPosition` varchar(8) NOT NULL DEFAULT 'left' COMMENT '操作列位置（left/right）',
    ADD COLUMN `IsAllowCustomColumn` bit(1) NOT NULL DEFAULT b'1' COMMENT '是否允许设置自定义列';
ALTER TABLE `SmModules` ADD CONSTRAINT `CK_SmModules_OptionPosition`
    CHECK (`OptionPosition` IN ('left', 'right'));
```

回滚：

```sql
ALTER TABLE `SmModules` DROP CHECK `CK_SmModules_OptionPosition`;
ALTER TABLE `SmModules`
    DROP COLUMN `IsAllowCustomColumn`,
    DROP COLUMN `OptionPosition`;
```

## PostgreSQL

```sql
ALTER TABLE "SmModules" ADD COLUMN "OptionPosition" varchar(8) NOT NULL DEFAULT 'left';
ALTER TABLE "SmModules" ADD COLUMN "IsAllowCustomColumn" boolean NOT NULL DEFAULT true;
ALTER TABLE "SmModules" ADD CONSTRAINT "CK_SmModules_OptionPosition"
    CHECK ("OptionPosition" IN ('left', 'right'));
```

回滚：

```sql
ALTER TABLE "SmModules" DROP CONSTRAINT "CK_SmModules_OptionPosition";
ALTER TABLE "SmModules" DROP COLUMN "IsAllowCustomColumn";
ALTER TABLE "SmModules" DROP COLUMN "OptionPosition";
```

## 执行前后核对

执行前先用目标数据库的系统目录确认脚本将创建的列和命名约束均不存在；如果查询返回任意记录，不要继续套用整段脚本，应先核对是否为已执行、部分执行或同名对象冲突。

```sql
-- SQL Server：预期返回 0 行
SELECT c.name AS ObjectName, 'COLUMN' AS ObjectType
FROM sys.columns AS c
WHERE c.object_id = OBJECT_ID(N'dbo.SmModules')
  AND c.name IN (N'OptionPosition', N'IsAllowCustomColumn')
UNION ALL
SELECT o.name, o.type_desc
FROM sys.objects AS o
WHERE o.parent_object_id = OBJECT_ID(N'dbo.SmModules')
  AND o.name IN (
      N'DF_SmModules_OptionPosition',
      N'DF_SmModules_IsAllowCustomColumn',
      N'CK_SmModules_OptionPosition'
  );

-- MySQL：两条查询均预期返回 0 行
SELECT `COLUMN_NAME`
FROM `information_schema`.`COLUMNS`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_NAME` = 'SmModules'
  AND `COLUMN_NAME` IN ('OptionPosition', 'IsAllowCustomColumn');

SELECT `CONSTRAINT_NAME`
FROM `information_schema`.`TABLE_CONSTRAINTS`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_NAME` = 'SmModules'
  AND `CONSTRAINT_NAME` = 'CK_SmModules_OptionPosition';

-- PostgreSQL：两条查询均预期返回 0 行
SELECT column_name
FROM information_schema.columns
WHERE table_schema = current_schema()
  AND table_name = 'SmModules'
  AND column_name IN ('OptionPosition', 'IsAllowCustomColumn');

SELECT constraint_name
FROM information_schema.table_constraints
WHERE constraint_schema = current_schema()
  AND table_name = 'SmModules'
  AND constraint_name = 'CK_SmModules_OptionPosition';
```

执行后按目标方言至少运行一条组合统计：

```sql
-- SQL Server
SELECT OptionPosition, IsAllowCustomColumn, COUNT(*) AS RecordCount
FROM dbo.SmModules
GROUP BY OptionPosition, IsAllowCustomColumn;

-- MySQL
SELECT `OptionPosition`, `IsAllowCustomColumn`, COUNT(*) AS `RecordCount`
FROM `SmModules`
GROUP BY `OptionPosition`, `IsAllowCustomColumn`;

-- PostgreSQL
SELECT "OptionPosition", "IsAllowCustomColumn", COUNT(*) AS "RecordCount"
FROM "SmModules"
GROUP BY "OptionPosition", "IsAllowCustomColumn";
```

结果中 `OptionPosition` 只能为 `left/right`，两个字段均不得为 `NULL`。随后通过模块维护页分别保存 `left/right` 与允许/禁止组合，并确认 `GET /api/SmModule/GetModuleInfo/{moduleCode}` 返回小写 `optionPosition` 与 `isAllowCustomColumn`。新增和更新会归一化默认值，旧客户端更新时未携带新字段则保留数据库原值。模块配置有缓存，保存后应重新获取模块信息；若由 SQL 直接改数据，则需按现有运维流程刷新模块缓存。
