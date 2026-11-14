# 前端配置接口

`GET /api/SmConfig/GetClientConfig` 无业务入参，沿用 `SmConfigController` 的授权策略和 `ServiceResult<ClientConfigDto>` 响应包装。

响应数据中的 `Theme` 是去掉 `theme.` 前缀的配置编码到字符串值的字典（属性名保留大写 `T`）。数据库中的编码保持不变。例如数据部分为：

```json
{
  "Theme": {
    "primary": "#1677ff",
    "isDark": "false"
  }
}
```

优先读取 Redis 中的主题缓存，缓存行为及已知限制见 [系统配置缓存](ConfigCache.md)。未命中时查询有效、未删除的 `system-theme` 分组，按 `Sequence`、`ID` 取第一个；仅返回该分组内有效、未删除且位于 `SmConfigServices.GetClientConfig` 白名单内的配置。没有匹配分组或配置时返回空字典；空值转为空字符串；重复编码作为配置错误交由现有异常处理流程处理。前端不能通过参数扩大读取范围。

当前仅提供 `Theme`，不包含未定义的注册开关或其他系统参数。新增前端配置时显式扩展 `ClientConfigDto` 和服务映射，并审查其是否适合公开给已授权前端。调用链使用现有 `Db` 查询入口，不绕过已有全局过滤器；取消令牌在查询前后检查，未中止已提交的数据库查询。

旧 `GET /api/SmConfig/GetTheme` 及其服务方法已移除，`GetListByGroup` 保持不变。前端 `src/hooks/useInitSystemTheme.ts` 通过 `src/api/modules/smConfig.ts` 调用新接口，从 `Data.Theme` 读取短键主题配置；配置维护页面继续使用分组明细接口。无需数据库迁移。移除旧接口属于破坏性变更：先提供 GetClientConfig 并迁移前端及外部消费者，再移除 GetTheme；仍调用旧地址的客户端将无法获取原响应。回滚旧前端前须先恢复旧 Controller、IService 和 Service 方法。供 GetClientConfig 使用的 ConfigCache.GetThemeAsync 缓存方法保留。

Controller、IService 和 Service 文件带有生成标记，后续重新生成时须保留这些自定义方法。

相较初版 GetClientConfig，Theme 的键不再包含 theme. 前缀。仓库内主题加载已使用短键；已接入初版或旧 GetTheme 的外部消费者需同步改用新接口及短键。部署时应协调消费者切换，回滚时一并恢复旧键读取方式。
