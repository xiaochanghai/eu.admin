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

同步 Skill 表时还会同步共享 `FileAttachment` 表中的 Skill 文件路径索引：选择 `AgSkillDefinition` 时处理 `agent-skill-draft`，选择 `AgSkillVersion` 或 `AgSkillVersionFile` 时处理 `agent-skill-version`。同步器只替换这两类记录，不会删除或覆盖其他业务附件。附件记录的 `Path` 是相对于 `AgentStorage:SkillRootPath` 的目录，`FileName` 是目录内文件名。Skill 文件内容仍存放在该受控目录，数据库同步不会复制物理文件，部署或迁移时必须同步复制该目录。

Skill 文件名最多 64 个字符，扩展名最多 10 个字符，以保证路径索引可无损写入共享 `FileAttachment` 表。应用启动时只读扫描已有 Skill 目录并重建索引；目录缺失或不可访问会阻止启动，不会自动创建空目录并清除索引。

Skill 文件默认存放在 Agent Host 内容根目录下的 `wwwroot/skills`。Docker 镜像中的绝对路径为 `/app/wwwroot/skills`，部署时应将命名卷或宿主机目录挂载到该路径，并与 Agent 数据库一同备份、恢复和迁移。虽然目录位于 `wwwroot` 下，Agent Host 会阻断 `/skills` 静态访问；文件内容仍只能通过受认证、授权的 Skill API 访问。

## 安全边界

- 源连接和目标连接必须显式指定、已启用且不能相同。
- `replaceData=true` 会删除所选目标表的现有数据，必须同时传入 `confirmReplaceData=true`。
- 数据替换在目标连接事务中执行；任一表复制失败或行数校验失败时回滚数据事务。
- 单个应用实例内的同步请求会串行执行，避免共享 SqlSugar 配置和目标表覆盖操作相互干扰；部署多个 API 实例时仍应只保留一个维护入口执行同步。
- SqlSugar `CodeFirst` 结构同步先于数据事务执行。数据库 DDL 是否支持事务取决于数据库引擎，因此执行前仍应备份目标库并先做结构同步验证。
- 目标为 SQL Server 时，Agent 结构同步会临时关闭 SqlSugar 的 `nvarchar` 默认映射，所有字符串栏位按 `varchar` 创建；同步完成后恢复连接原配置。
- 同步器只接受已登记的 Agent 实体表，不接受任意表名或 SQL。
- `FileAttachment` 不能作为请求表名单独传入；它只会随 Skill 表按附件类型过滤同步，避免影响共享附件数据。
- 同一张表不能重复指定，`batchSize` 只能设置为 `1` 至 `10000`。
- 包含自增列的表会被拒绝，避免隐式改变主键生成语义。
- 当前实现一次读取一张源表，再按 `batchSize` 分批写入；超大表应在维护窗口执行并关注应用内存。

## 建议执行顺序

1. 备份目标数据库。
2. 使用 `replaceData=false` 执行结构同步。
3. 检查目标表结构、字段类型和索引。
4. 在维护窗口停止目标库写入流量。
5. 使用 `replaceData=true` 和 `confirmReplaceData=true` 执行数据替换。
6. 核对响应中的逐表源/目标行数，并完成关键 Agent、Skill、MCP、知识库、编排和评估流程验证。
