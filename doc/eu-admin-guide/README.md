# EU-Admin 项目说明索引

> 状态：`CURRENT`
>
> 适用范围：整个 EU-Admin 前后端分离仓库
> 最后核对：2026-08-10

## 1. 如何使用这套说明

本目录回答四个问题：系统有什么、具体如何实现、能力由哪里拥有、修改时还要检查什么。建议按以下顺序阅读：

1. 先看本页的 30 秒系统地图。
2. 需要理解整体边界时看[系统架构总览](系统架构总览.md)。
3. 需要查找具体功能时看[功能模块地图](功能模块地图.md)。
4. 需要沿源码追踪时看[实现链路与源码索引](实现链路与源码索引.md)，再进入[业务模块实现明细](业务模块实现明细.md)或[系统模块实现明细](系统模块实现明细.md)。
5. 需要了解静态复核结果和未验证边界时看[实现核查报告](实现核查报告.md)。
6. 准备修改或交付时看[开发与交付](开发与交付.md)。
7. 动态页面、工作流、数据库等复杂主题继续进入既有专题文档和源码旁 README。

文档用于导航，当前代码、类型、测试和运行配置才是实现事实源。数据库菜单、角色授权和租户配置会影响最终可见功能，不能只凭本文判断某个环境已启用全部菜单。

## 2. 30 秒系统地图

```text
EU-Admin
├─ eu.admin.react/                 React 管理端
│  ├─ src/views/                   系统、基础资料、采购、销售、库存、设备、示例页面
│  ├─ src/components/              动态表格/表单、上传、图表、AI 对话、设计器
│  ├─ src/api/ + src/redux/        HTTP 边界与全局状态
│  ├─ src/routers/ + src/layouts/  动态路由、权限、布局、标签页与主题
│  ├─ src/workflow/                当前业务流程设计与设置器
│  └─ src/workflow-editor/         独立的新流程编辑器边界
├─ eu.core/                        .NET 解决方案
│  ├─ EU.Core.Api                  主业务 Web API
│  ├─ EU.Core.Jobs                 后台任务宿主
│  ├─ EU.Core.MCP.Api              MCP/AI 独立宿主
│  ├─ Src/EU.Core.Gateway          Ocelot 网关；Nacos Provider 存在但当前发现注册被注释
│  ├─ EU.Core.Model/IServices/Services  模型、契约与业务逻辑
│  ├─ EU.Core.Repository + Src/EU.Core.DataAccess
│  │                                  仓储与多数据库访问
│  └─ Src/EU.Core.Common/Extensions/... 缓存、认证、租户、事件、任务、日志等平台能力
├─ db/                             数据库交付包
├─ model/                          PowerDesigner 数据模型
└─ doc/                            项目、开发、部署与专题文档
```

核心运行链路：

```text
浏览器页面
  → React 动态路由/权限
  → src/api HTTP 边界
  → EU.Core.Api Controller
  → IService → Service → Repository/DataAccess
  → 业务数据库 / Redis / 文件 / 消息与任务基础设施
```

动态业务页面还多一层元数据：

```text
SmModules（页面与能力）
  ├─ SmModuleSql（查询模型）
  └─ SmModuleColumn（列表、搜索、表单、校验与数据源）
       → ModuleInfo API → Redux → ProTable / 动态表单
```

## 3. 功能全景

| 功能族 | 主要能力 | 主要入口 |
|---|---|---|
| 账号与权限 | 登录、当前用户、密码、用户、角色、菜单、按钮、数据范围 | `views/login`、`views/system/privilege`、Authorize/SM Controllers |
| 界面与导航 | 多布局、主题、标签页、动态路由、菜单搜索、国际化、全屏 | `src/layouts`、`src/routers`、Redux global/tabs/auth |
| 动态页面平台 | 通用列表/表单、模块 SQL、栏位设计、用户列偏好、导入导出 | `components/ProTable`、`components/Elements`、SmModule 专题 |
| 低代码与流程 | PC 表单、移动端页面、流程节点、审批人/抄送/条件、发布 | `views/system/config`、`src/workflow*`、SmWorkFlow Controllers |
| 系统管理 | 组织、配置、字典、编码、地区、日志、任务、服务器、缓存 | `views/system`、`Controllers/SM` |
| 基础资料 | 客户、供应商、物料、仓库、单位、币种、结算、交货等 | `views/basedata`、`Controllers/BD` |
| 采购 | 申请、订单、到货/通知、入库、退货及转单/完结 | `views/purchase`、`Controllers/PO` |
| 销售 | 订单、变更、发货、出库、退货及单据转换 | `views/sales`、`Controllers/SD` |
| 库存 | 入库、出库、盘点、核算、安全库存、库存报表与预警 | `views/stock`、`Controllers/IV` |
| 设备 | 类型、设备台账、详情、维修单与维修日志 | `views/equipment`、`Controllers/EM` |
| 集成与基础设施 | 多租户、多数据库、Redis、SignalR、Quartz、事件、日志、网关、MCP | `eu.core` 各独立宿主与 `Src/**` |

完整清单及每项要点见[功能模块地图](功能模块地图.md)；页面、moduleCode、接口和服务链路见[实现链路与源码索引](实现链路与源码索引.md)。

## 4. 按任务选择文档

| 我要做什么 | 首读 | 继续下钻 |
|---|---|---|
| 了解项目整体 | [系统架构总览](系统架构总览.md) | 根 `README.md`、前后端 `AGENTS.md` |
| 查找一个功能或页面 | [功能模块地图](功能模块地图.md) | 对应 `src/views/<domain>`、Controller/Service |
| 核对具体实现是否存在 | [实现核查报告](实现核查报告.md) | [实现链路与源码索引](实现链路与源码索引.md) |
| 修改基础资料或业务单据 | [业务模块实现明细](业务模块实现明细.md) | 对应 BD/PO/SD/IV/EM Controller、Service、Model |
| 修改系统管理或平台能力 | [系统模块实现明细](系统模块实现明细.md) | 对应 SM/Tenant/Host 实现 |
| 修改动态列表或表单 | [前端 SmModule 索引](../frontend/SmModule/README.md) | [后端 SmModule 索引](../backend/SmModule/README.md) |
| 修改工作流 | `eu.admin.react/src/workflow/README.md` | WorkflowEditor/setters README、SmWorkFlow 服务 |
| 修改权限、菜单或路由 | [系统架构总览](系统架构总览.md) | auth/user/module Redux、RouterGuard、SM 权限服务 |
| 修改业务单据 | [功能模块地图](功能模块地图.md) | 对应 PO/SD/IV/EM 页面、Controller、Service、Model |
| 修改数据库或模型 | [数据库表设计约束](../backend/数据库表设计约束.md) | 实体、映射、仓储、服务、API、前端类型和 `model/db` |
| 修改后台任务 | [功能模块地图：后台任务](功能模块地图.md#13-后台任务事件日志与网关) | `EU.Core.Jobs`、`Src/EU.Core.Tasks` |
| 修改 MCP/AI | [系统架构总览](系统架构总览.md) | `StreamController`、`EU.Core.MCP.Api`、前端 Chat |
| 准备提交或发布 | [开发与交付](开发与交付.md) | 根及局部 `AGENTS.md`、部署文档 |

## 5. 状态标记

- `CURRENT`：已由当前源码或配置核对的能力。
- `BACKEND-ONLY`：后端存在能力，但当前 React 生产页面目录未发现对应入口。
- `EXAMPLE`：示例、演示或技术验证，不作为生产业务承诺。
- `PLANNED`：设计计划，尚不能作为已实现能力描述。
- `HISTORICAL`：历史分析或旧实现，仅供追溯。

## 6. 专题文档

- [实现链路与源码索引](实现链路与源码索引.md)
- [业务模块实现明细](业务模块实现明细.md)
- [系统模块实现明细](系统模块实现明细.md)
- [实现核查报告](实现核查报告.md)
- [前端开发文档](../frontend/README.md)
- [后端开发文档](../backend/README.md)
- [SmModule 动态页面后端专题](../backend/SmModule/README.md)
- [SmModule 动态页面前端专题](../frontend/SmModule/README.md)
- [Docker 部署（HISTORICAL）](../Docker部署.md)
- [创建开发环境（HISTORICAL）](../创建开发环境.md)
- [快速启动与常见问题（PARTIAL）](../使用手册.md)

## 7. 研究可追溯性

- [研究记录](research.md)
- [本地来源清单](sources.md)
- [文档元数据](metadata.md)
