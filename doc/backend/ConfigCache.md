# 系统配置缓存

实现位于 `eu.core/Src/EU.Core.Common/Helper/ConfigCacheHelper.cs`，类名为 `ConfigCache`。仓库内初始化调用方为 `UtilityHelper`。

缓存使用 Redis 数据库 3 的 `SmConfig` 键，以配置编码存储配置对象，沿用现有全局键范围，不新增租户或用户隔离，不设置新的 TTL。

前端主题字典存放在同一键的 `Theme` 哈希字段。`ExistsThemeAsync` 检查该字段，`GetThemeAsync` 读取字典，`AddThemeAsync` 写入字典，`ClearThemeAsync` 仅删除该字段。`SmConfigServices.GetClientConfig` 命中时读取缓存，未命中时查询并缓存短键主题字典；`Update(Guid, object)` 完成后清除主题字段。当前未覆盖新增、删除、分组变更的失效，也未协调并发查询回写与更新失效，此限制本次保留。

初始化不再维护进程内布尔标记。`InitAsync` 在获取进程内同步锁前后检查 Redis 键是否存在：存在且未指定 `forceRefresh` 时返回 0；否则清除旧缓存并从传入的数据库上下文重新加载。`GetValueAsync` 未命中时调用初始化并重读，`GetValuesAsync` 读取前检查初始化状态。`Clear` 删除 Redis 键，后续读取可重新加载；`forceRefresh: true` 始终重新加载。

空表或没有有效配置编码时不会创建缓存键，后续读取会再次查库。数据库和 Redis 异常继续向调用方传播。锁仅覆盖当前进程的初始化，跨进程初始化及读取期间部分缓存可见的问题仍沿用现有实现；本次不提供分布式锁或原子替换。接口签名和配置项无需变更；回滚时恢复原帮助类实现即可。
