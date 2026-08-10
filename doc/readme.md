# EU-Admin 文档索引

> 文档角色：全仓渐进式文档入口
>
> 状态：`CURRENT`
> 最后核对：2026-08-10

## 1. 首次阅读

1. [EU-Admin 项目说明索引](eu-admin-guide/README.md)：30 秒系统地图、功能全景和按任务导航。
2. [系统架构总览](eu-admin-guide/系统架构总览.md)：前后端分层、独立宿主、权限、元数据与基础设施。
3. [完整功能模块地图](eu-admin-guide/功能模块地图.md)：罗列系统所有稳定功能点、拥有路径和可见状态。
4. [实现链路与源码索引](eu-admin-guide/实现链路与源码索引.md)：页面、moduleCode、API、Controller、Service 和模型链路。
5. [业务模块实现明细](eu-admin-guide/业务模块实现明细.md)：BD、PO、SD、IV、EM、EC 的具体实现。
6. [系统模块实现明细](eu-admin-guide/系统模块实现明细.md)：权限、模块、组织、配置、运维、租户、AI/MCP 和工具链。
7. [实现核查报告](eu-admin-guide/实现核查报告.md)：静态复核结论、已修正描述和运行时未验证边界。
8. [开发与交付导航](eu-admin-guide/开发与交付.md)：需求定位、影响矩阵、验证和文档维护。

文档负责导航；当前代码、类型、测试和运行配置是实现事实源。数据库动态菜单和角色授权会改变具体环境中的可见功能。

## 2. 按任务选择文档

| 我要处理 | 首读 | 继续下钻 |
|---|---|---|
| React 管理端 | [前端文档索引](frontend/README.md) | `../eu.admin.react/AGENTS.md`、对应页面/API/Redux/组件 |
| .NET Web API | [后端文档索引](backend/README.md) | `../eu.core/AGENTS.md`、Controller/Service/Model/Repository |
| 追踪具体实现 | [实现链路与源码索引](eu-admin-guide/实现链路与源码索引.md) | [实现核查报告](eu-admin-guide/实现核查报告.md) |
| 业务单据/基础资料 | [业务模块实现明细](eu-admin-guide/业务模块实现明细.md) | 对应领域页面、Controller、Service 和 Model |
| 系统管理/平台能力 | [系统模块实现明细](eu-admin-guide/系统模块实现明细.md) | SM/Tenant/独立宿主源码 |
| 动态页面/通用表格表单 | [SmModule 前端索引](frontend/SmModule/README.md) | [SmModule 后端索引](backend/SmModule/README.md) |
| 工作流/流程设计 | `../eu.admin.react/src/workflow/README.md` | WorkflowEditor、setters、`src/workflow-editor/README.md` |
| 数据库与模型 | [数据库表设计约束](backend/数据库表设计约束.md) | `../model`、`../db`、实体与映射 |
| Jobs/后台任务 | [功能地图：后台任务](eu-admin-guide/功能模块地图.md#13-后台任务事件日志与网关) | `../eu.core/EU.Core.Jobs`、`../eu.core/Src/EU.Core.Tasks` |
| Gateway/MCP/AI | [系统架构总览](eu-admin-guide/系统架构总览.md) | 对应独立宿主、StreamController、前端 Chat |
| Docker/服务器部署 | [Docker 部署（HISTORICAL）](Docker部署.md) | 当前宿主 Dockerfile、目标环境配置；旧初始化/创建脚本仅供追溯 |
| 操作使用说明 | [使用手册](使用手册.md) | 对应功能模块源码与部署环境菜单 |

## 3. 当前专题文档

### 前端

- [前端开发文档索引](frontend/README.md)
- [SmModule 动态页面元数据消费约束](frontend/SmModule/动态页面元数据消费约束.md)
- [SmModule 表单控件类型](frontend/SmModule/表单控件类型.md)
- 工作流 README：`eu.admin.react/src/workflow/**`
- 新流程编辑器 README：`eu.admin.react/src/workflow-editor/README.md`

### 后端

- [后端开发文档索引](backend/README.md)
- [数据库表设计约束](backend/数据库表设计约束.md)
- [SmModule 元数据模型与维护约束](backend/SmModule/元数据模型与维护约束.md)
- [SmModule 数据库只读核对基线](backend/SmModule/数据库只读核对基线.md)
- [SmModule 配置脚本说明](backend/SmModule/配置脚本说明.md)

### 部署与环境

> 以下服务器脚本文档来自旧版 `eucloud`/CentOS 部署链路，当前仓库未包含其完整脚本和资源，均按 `HISTORICAL` 使用；不得直接作为当前生产部署步骤。

- [Docker 部署（HISTORICAL）](Docker部署.md)
- [初始化服务器（HISTORICAL）](初始化服务器.md)
- [创建开发环境（HISTORICAL）](创建开发环境.md)
- [移除开发环境（HISTORICAL）](移除开发环境.md)
- [服务器 sudo 与免密配置（HISTORICAL）](服务器添加sudo权限和免密.md)

## 4. 分析、设计与历史材料

以下材料用于背景、审计或方案讨论，不自动代表当前运行行为：

- `当前项目分析与问题解决方案.html`
- `eu.admin.react深度分析报告.html`
- `EU.Core.Jobs项目分析与优化报告.html`
- `frontend-scan-report.html`
- `api-status-analysis.html`
- `workflow-design-plan.html`
- `workflow-runtime-development-plan.html`
- `mobile-lowcode-config-design.html`
- `数据权限设计文档.html`

引用其中结论前，应重新通过源码、测试和配置核对。设计计划未落地时应标记 `PLANNED`。

## 5. 事实源优先级

1. 用户在当前任务中的明确决定。
2. 根及局部 `AGENTS.md` 强制约束。
3. 当前源码、类型、测试和受 Git 跟踪的运行配置。
4. 标记为 `CURRENT` 的专题文档。
5. 分析报告、设计计划和历史材料。

## 6. 文档维护

- 新增稳定模块时更新[功能模块地图](eu-admin-guide/功能模块地图.md)和对应前/后端索引。
- 架构、宿主、权限、租户、数据库或跨端契约变化时更新[系统架构总览](eu-admin-guide/系统架构总览.md)。
- 验证脚本、构建或部署方式变化时更新[开发与交付](eu-admin-guide/开发与交付.md)和部署文档。
- 文档链接使用仓库相对路径，不记录真实密码、Token、连接串或个人数据。
