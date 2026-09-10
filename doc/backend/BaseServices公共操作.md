# BaseServices 公共操作

实现：`eu.core/EU.Core.Services/Base/BaseServices.cs`。

- 通用单条实体新增 `BaseRepository.Add(TEntity, Guid?)` 直接执行 SqlSugar 插入，不再将 SQL 转成字符串后交给 ADO 执行；保留可选主键覆盖和主键返回行为。其他新增重载不在此次调整范围。
- `AgAgentOperationAuditServices` 复用基类单条新增和限量列表查询；列表保留租户、未删除过滤、双字段倒序及 1～100 条限制。审计结果更新使用 `UpdateAsync(entity, columns, predicate)`，保留专用条件与四个指定字段。
- 两个服务基类及仓储提供表达式 `UpdateAsync`：实体、字段表达式和条件均不可为空；不自动追加审计字段或主键条件，调用方必须提供完整筛选（例如 ID、租户和原状态）。该方法只复用更新机制，不负责业务校验。

- 两个批量新增重载逐条等待表单校验及自动编号，全部完成后才执行插入；校验失败会通过返回的 Task 传播。
- 字典批量更新直接异步读取实体，不再额外执行同步存在性查询；不存在的记录仍跳过。
- `UpdateReturn` 更新失败返回默认值，成功后通过 `QueryDto` 读取持久化结果。派生类自行覆盖的实现不受此修复覆盖。
- 审核、撤销在写入条件中校验原状态，保留主键限制；批量操作的布尔结果仍表示至少一条发生变化，不代表每条都成功。
- 仓储带 `where` 的实体集合更新改为逐条执行，显式拼入 ORM 主键表达式（含联合主键）；无主键时拒绝更新。无附加条件仍走原批量路径。此调整也覆盖销售订单、销售变更单、销售出库单中的条件更新；不会自行开启事务，需原子提交的调用方应沿用外层事务。
- 唯一性校验将字段值、删除标记、排除 ID 参数化。表名、字段名限制为普通标识符，表名可带 schema。可选 `whereCondition` 仅用于服务端可信 SQL 片段，禁止传入请求文本。
- 唯一性查询不替代数据库唯一约束；并发插入的最终唯一性仍应由数据库保证。

离线回归测试：

```text
dotnet test Src/EU.Core.Tests/EU.Core.Tests.csproj --no-restore -c Release --filter FullyQualifiedName~BaseServicesRegressionTests
```

在 `eu.core` 目录运行。该测试类不加载应用配置、不执行 SQL，只验证返回值及 SQL 生成。真实数据库并发验证需另行在获授权的隔离环境执行。
