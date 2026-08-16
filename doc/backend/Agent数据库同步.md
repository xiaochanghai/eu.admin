# Agent 数据库同步

Agent 数据库同步用于在两个已启用的 SqlSugar 连接之间同步 `EU.Core.Model.Entity.AG` 下的表结构和数据。

## 接口

```http
POST /api/Common/SyncAgentDatabase
Authorization: Bearer <token>
Content-Type: application/json
```

该接口继承 `CommonController` 的权限策略，并额外要求 `SuperAdmin` 角色，不允许匿名或普通用户调用。旧的匿名 `GET /api/Common/SyncData` 和 `GET /api/Common/SyncData/{tableName}` 已移除。

仅同步表结构：

```json
{
  "sourceConfigId": "source-config-id",
  "targetConfigId": "target-config-id",
  "tables": [],
  "syncStructure": true,
  "replaceData": false,
  "confirmReplaceData": false,
  "batchSize": 1000
}
```

同步结构并替换目标数据：

```json
{
  "sourceConfigId": "source-config-id",
  "targetConfigId": "target-config-id",
  "tables": [],
  "syncStructure": true,
  "replaceData": true,
  "confirmReplaceData": true,
  "batchSize": 1000
}
```

`tables` 为空时处理全部 Agent 表；指定表名时仅处理所选表。同步器会使用内置的父表到子表顺序写入，并按相反顺序清理目标数据，不依赖请求中的表名顺序。

## 安全边界

- 源连接和目标连接必须显式指定、已启用且不能相同。
- `replaceData=true` 会删除所选目标表的现有数据，必须同时传入 `confirmReplaceData=true`。
- 数据替换在目标连接事务中执行；任一表复制失败或行数校验失败时回滚数据事务。
- SqlSugar `CodeFirst` 结构同步先于数据事务执行。数据库 DDL 是否支持事务取决于数据库引擎，因此执行前仍应备份目标库并先做结构同步验证。
- 同步器只接受已登记的 Agent 实体表，不接受任意表名或 SQL。
- 包含自增列的表会被拒绝，避免隐式改变主键生成语义。
- 当前实现一次读取一张源表，再按 `batchSize` 分批写入；超大表应在维护窗口执行并关注应用内存。

## 建议执行顺序

1. 备份目标数据库。
2. 使用 `replaceData=false` 执行结构同步。
3. 检查目标表结构、字段类型和索引。
4. 在维护窗口停止目标库写入流量。
5. 使用 `replaceData=true` 和 `confirmReplaceData=true` 执行数据替换。
6. 核对响应中的逐表源/目标行数，并完成关键 Agent、Skill、MCP、知识库、编排和评估流程验证。
