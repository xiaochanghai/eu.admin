# EU-Admin 项目说明研究记录

> 状态：`CURRENT`
>
> 研究日期：2026-08-09
> 目标：基于当前仓库代码、配置和已有文档，建立可追溯的项目功能事实表，并形成完整项目说明。

## 1. 研究范围

- 前端：`eu.admin.react/src/**`，重点核对页面、路由、API、Redux、布局、通用表格/表单、工作流、低代码与 AI 对话。
- 后端：`eu.core/EU.Core.sln`、Web API Controllers、Services/IServices、Jobs、Gateway、MCP、Common、DataAccess、EventBus、Tasks、Serilog 与代码生成器。
- 数据与部署：`db/**`、`model/**`、根 README、`doc/**`、环境与 Docker 文档。
- 参考结构：`E:/DressCode/porcelain-client-platform/apps/porcelain-frontend/docs/**` 的渐进式索引、架构文档、模块地图和维护规范。

## 2. 受众假设

- 第一次接触仓库、需要快速了解系统能力的开发人员。
- 需要判断某个需求归属前端、后端、宿主、平台或数据库的维护人员。
- 需要了解现有业务覆盖、平台能力和部署组件的产品、测试及运维人员。

## 3. 已确认的总体结论

1. EU-Admin 是前后端分离的企业管理平台，不只是权限脚手架；仓库同时包含系统管理、基础资料、采购、销售、库存、设备、电子商务后端、租户、工作流、低代码、AI/MCP 与基础设施能力。
2. 前端大量业务页面由 `SmModules + SmModuleSql + SmModuleColumn` 元数据驱动，模块代码、动态路由、列/表单配置、操作权限和通用 CRUD 共同决定最终页面。
3. 后端采用 Controller → IService → Service → Repository/DataAccess → Model 分层，并有 Web API、Jobs、Gateway、MCP API 四类独立宿主。
4. 业务模块目前以 `BD/PO/SD/IV/EM/EC/SM` 等领域代码组织；React 中已有 BD、PO、SD、IV、EM 和 SM 的页面，EC 当前主要表现为后端能力。
5. 通用列表支持动态查询、搜索、分页、排序、列定制、用户列偏好、行操作、批量操作、导入导出、日志、主从表和合计；动态表单支持多种控件、校验、数据源、文件/图片上传与新增/编辑/查看模式。
6. 工作流存在 `src/workflow/**` 与 `src/workflow-editor/**` 两套边界；模块配置页还提供 PC 表单、移动端页面配置和流程配置入口。
7. 系统基础能力包括内置 JWT、IdentityServer4/Authing 可选认证路径、角色/模块/功能权限、数据权限、多租户、多数据库、Redis、SignalR、Quartz、事件总线、日志、文件附件、DbFirst/CodeFirst 和代码生成。
8. 文档必须区分当前已实现、仅后端已具备、示例演示和规划材料；HTML 分析报告与设计计划只能作为辅助材料，不能高于代码与运行配置。

## 4. 功能域盘点

### 4.1 用户可见基础体验

- 登录、当前用户、退出/令牌、修改密码、个人设置。
- 动态菜单和路由守卫、页面/按钮权限、403/404/500 错误页。
- 横向、经典、纵向、分栏与聊天布局。
- 标签页、详情页标签、拖拽排序、KeepAlive、最大化。
- 亮/暗主题、主题色、紧凑、圆角、灰色、色弱、菜单/Header 反色。
- 全屏、组件尺寸、国际化、菜单搜索、消息入口、面包屑。
- 首页图表与统计展示、AI 对话入口、附件和 Excel 上传。

### 4.2 系统与平台管理

- 用户、角色、角色模块、角色功能、角色数据范围、用户角色。
- 模块/菜单、模块 SQL、模块栏位、功能权限、用户自定义列。
- 公司、集团、部门、员工等组织资料。
- 配置、配置组、字典/LOV、语言配置、自动编码、行政区划。
- 数据表/字段目录、通用列表 SQL、导入模板与导入错误链路。
- 工作流、节点、节点审核；PC 表单设计、移动端页面配置。
- Quartz 任务与任务日志、API/登录/入口日志、服务器和缓存管理。
- 应用设备、应用记录、应用版本、微信配置。
- 文件附件、图片、视频与分片上传/下载。

### 4.3 业务域

- 基础资料：颜色、币种、客户及等级/分类/地址、供应商及等级/分类、交货/结算方式、地区、物料/物料类型、单位、材质、仓库、货位、物料库存与变动。
- 采购：采购申请、采购订单、到货/通知、采购入库、采购退货及明细；支持完结、转单、选择待处理单据和批量带入明细。
- 销售：销售订单、变更单、发货单、出库单、退货单及明细；支持完结、变更、发货/出库转换和退货带入。
- 库存：入库、出库、盘点、核算、安全库存；库存汇总、明细、变动、库龄和预警查询。
- 设备：设备类型、设备台账、维修单与维修日志、设备详情。
- 电子商务：Banner、商品、新闻后端接口；当前 React 页面目录未发现对应生产页面。

### 4.4 独立宿主与基础设施

- `EU.Core.Api`：主业务 HTTP API、Swagger、认证授权、动态 CRUD、文件与流式 AI 转发。
- `EU.Core.Jobs`：Quartz/任务中心宿主及后台任务运行。
- `EU.Core.MCP.Api`：MCP/AI 独立 API 宿主，由主 API 流式接口委托令牌访问。
- `EU.Core.Gateway`：当前启用 Ocelot + Polly；Nacos Provider 项目存在，但 Gateway 注册代码被注释。主 API 另有可配置的 Nacos 注册与配置监听链路。
- Common/DataAccess/Extensions：多数据库、缓存、认证、租户、模块元数据、工具与宿主扩展。
- EventBus/Tasks/Serilog：内存订阅关系管理、RabbitMQ/Kafka 事件传输、任务调度、结构化日志及可选 Elasticsearch 日志。
- CodeGenerator/DbFirst：数据库读取、模型与分层代码生成。

## 5. 文档结构决策

采用参考项目的“L1 索引 → L2 架构/模块 → 专题 deep dive”模式：

1. `doc/readme.md`：全仓文档索引与按任务路由。
2. `doc/eu-admin-guide/README.md`：项目说明入口与 30 秒系统地图。
3. `系统架构总览.md`：边界、宿主、数据流、权限、元数据、部署关系。
4. `功能模块地图.md`：完整罗列用户体验、系统平台、业务领域、基础设施、示例和状态。
5. `开发与交付.md`：修改路由、影响矩阵、验证门禁和文档维护。
6. `实现链路与源码索引.md`：公共实现、运行链路、接口和关键边界。
7. `业务模块实现明细.md`：BD/PO/SD/IV/EM/EC 的页面、moduleCode、Controller、Service 和动作。
8. `系统模块实现明细.md`：SM、Tenant、独立宿主、AI/MCP 与工具能力。
9. `实现核查报告.md`：复核方法、纠正项、实现注意项和运行时未验证边界。
10. 现有 `doc/frontend/**`、`doc/backend/**`、工作流 README、SmModule 专题继续作为 deep dive，不复制其全部实现细节。

## 6. 不确定性与边界

- 菜单由数据库配置动态生成；仅凭源码不能证明目标环境是否启用某个页面或授予某角色权限。
- Controller/实体存在只证明后端能力，不证明 React 已提供入口，因此文档单独标记“后端已有”。
- `example/**`、测试 Controller、Redis 示例和旧文件不属于生产业务承诺。
- HTML 扫描报告和 workflow 计划可能随代码过期，正式说明只引用其中可被当前源码再次核对的内容。
- 未连接数据库、未读取真实租户菜单、未启动各宿主，因此不声明生产环境菜单和外部依赖已验证。
- 当前 MCP 业务实现只确认 Supplier；旧按钮权限 API 返回静态 JSON；文件 Controller 的真实基础路由由类名 `FileController` 生成。这些已写入实现核查报告。

## 7. 实现级复核新增结论

- 生产页面源码共提取到 83 个唯一 `*_MNG` 模块代码，均已进入本说明；模块 URL 仍需由数据库 `SmModules` 数据确认。
- 主 API 非 Base Controller 类及各领域 Controller 已逐类与说明比对；明细 Controller、导入链路 Controller 和旧/示例 Controller 已显式区分。
- 采购订单页面存在调用销售订单变更接口的跨域接线，采购退货页面存在调用采购入库完结接口的跨域接线，文档按实际源码标记为待确认问题。
- 当前退出只清用户资料、token 和授权菜单，没有直接清理 module/tabs Redux。
- Supplier MCP 的导航类工具与写操作已分开描述；`create_supplier_from_file` 的方法体没有实现 Attribute 中声明的文件解析创建。
- Monitor、Common 工具接口、AI 上传、图片/下载和应用公开接口中存在多个匿名入口；本文只记录位置和行为，没有修改安全策略。
- `Authorize/GetAccessToken` 是匿名的开发测试永久 Token 入口，不是标准刷新 Token 能力；已从 `CURRENT` 改标为 `EXAMPLE`。
- 前端 Layout 的 SignalR effect 缺少依赖数组和卸载清理，存在重复连接/监听风险。
- `views/home` 使用页面静态统计值和 `config/**` 图表数据；`views/account/settings` 当前仅维护用户名和头像，修改密码由 Header 弹窗承担。
- EventBus 当前只有内存订阅管理器与 RabbitMQ/Kafka 传输，未发现 Channel/Redis EventBus Provider；数据库七类枚举也不代表所有辅助链路均完成适配。
- `axiosCancel.ts` 是尚未接入 Axios 请求实例的辅助文件，不能写成已经生效的重复请求取消机制。
- `SdOrderController111.cs` 被 `EU.Core.Api.csproj` 排除编译，SD 当前应按 10 个编译内 Controller 统计，历史文件单列。
- Quartz 调度实现多处同步读取异步结果，属于后台任务启动与执行链路的既有阻塞风险。
- 主 API 无条件开启 IdentityModel PII 诊断输出，生产日志存在敏感信息暴露风险。
- CORS 开放模式允许任意来源并携带凭据，是部署配置中的高风险开关。
- 匿名 AI 文件上传未复用普通上传校验，缺少大小、类型、目标路径和文件名边界。

## 8. 完成标准

- 新成员能在两次跳转内找到任一功能域的拥有路径。
- 所有稳定业务域、系统管理能力、前端平台能力和后端独立宿主均进入功能地图。
- 每个模块至少写清用户行为、前端入口、后端拥有边界、关键要点和状态。
- 文档链接和路径可解析；不包含真实凭据、连接串或个人数据。
