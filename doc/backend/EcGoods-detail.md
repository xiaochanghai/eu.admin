# 商品详情补充（2026-11-27）

任务类型：BACKEND-BUSINESS、API-CONTRACT。

GET /api/EcGoods/{id} 仍返回 ServiceResult<EcGoodsDto>。QueryById 使用现有租户仓储连接读取商品，非法 ID、不存在和已删除商品返回 null，由现有控制器处理查询失败。

新增 Accessories 数组，元素为 ID、FileName、OriginalFileName。只查询 MasterId 为当前商品、ImageType 为 Plugin 且未删除的附件，按创建时间和 ID 排序，不返回文件系统路径。图片沿用 /api/File/Img/{ID}。

每次成功查询通过 SQL 表达式原子累计 ViewCount；空值按零起算。只更新浏览量，不更新商品更新时间。读取返回累计后的值；并发访问时可能已包含其他访问次数。

此变化沿用旧版页面浏览即计数语义，因此管理端调用同一 QueryById 也会计数，请勿将该接口作为不应计数的后台轮询来源。现有浏览计数不是唯一访客统计。

GoodsDesc 已由商品实体和输入 DTO 补充，部署数据库需具备对应字段；本次不执行数据库迁移。不修改认证策略或 Redis 配置。此前权限服务依赖注入异常需要单独解决，否则请求无法进入商品服务。

前端 sphinx.website 优先读取 Accessories，旧服务未返回该字段时保留原配件接口作为兼容。部署时先发布后端，再发布前端。回滚服务方法及 DTO 新字段即可撤销新增行为，已累计浏览量不自动回退。

EcGoodsServices 和 EcGoods.Dto.View 为带生成标记的文件，本次按要求直接修改；重新生成时需保留定制内容。

测试：EcGoodsDetail_Should 只使用仓储代理，不访问数据库。有效商品的 SQL 累计、并发和租户过滤仍需隔离数据库集成验证；未连接或写入实际数据库。
