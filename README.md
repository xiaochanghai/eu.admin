
<div align="center"><h1>EU-Admin</h1></div>
<div align="center"><h3>EU（一优） 一心一意 做好每件事</h3></div>

## 前言

坐标苏州，2014年7月开始入行，学习.NET,最开始就学了webform，工作之后学了MVC、Sencha Touch（已抛弃技术）、JS、React、ReactNative、VUE，从.NetFramework 转到.NET CORE，很庆幸自己一直还是从事着开发的工作

从去年开始， 希望把前几年工作经历，以及想法做一个沉淀，故而有了这个项目。
新开的这个项目，期望实现这样的能力：业务人员只需关注实体的构建，业务服务的编写，以及路由的配置，未来的计划是所以基础代码一键生产。
部分代码是很早之前写的，可能不是那么规范，后面会统一优化掉

让业务的开发，变成简单的三步走：创建实体 >> 业务开发 >> 路由配置。


阿里巴巴大神毕玄曾说过，"一个优秀的工程师和一个普通工程师的区别，不是满天飞的架构图，他的功底体现在所写的每一行代码上"。

与君共思共勉！

## 项目概述📖

🚀🚀🚀 EU-Admin 一款基于 React18、React-RouterV6、React-Hooks、Redux-Toolkit、Zustand、TypeScript、Vite5、Ant-Design5 的实现的通用管理平台框架，开箱即用。
集成SqlSugar、缓存、 通讯、远程请求、任务调度等

前后端分离，使用 JWT 认证。

后端：基于 .NET8 、SqlSugar、 EF Core、Dapper，集成常用组件。

> 数据库文件：[https://pan.quark.cn/s/f302bcb7c7aa](https://pan.quark.cn/s/f302bcb7c7aa "https://pan.quark.cn/s/f302bcb7c7aa")
下载后SQL SERVER手动部署，版本2014（含）以上


### 在线预览地址 👀

- Link：http://cloud.auto-free.cn
- 账号密码：Admin，1


### 前端主要功能 🔨

- 使用 React18 + TypeScript 开发，整个项目使用高质量 Hooks + TypeScript 代码完成
- 使用 Vite5 作为开发、打包工具（配置 Gzip | Brotli 压缩打包、PWA 应用、Visualizer 包分析…）
- 使用 React-RouterV6 全新路由钩子，项目支持多路由（Hash | History）切换、路由懒加载配置
- 项目菜单、路由权限使用 **动态路由** 控制，完全根据后端菜单数据动态生成路由
- 使用 Redux-Toolkit、Zustand 作为状态管理工具（多分支），集成 persist 持久化工具
- 使用 Ant-Design 5 组件库开发，将 Design Token 注入到 CSS 变量中，方便配置项目主题
- 项目支持多主题：主题颜色、暗黑模式、灰色模式、色弱模式、紧凑主题、圆角大小配置
- 项目支持多布局：横向布局、经典布局（可开启菜单分割功能）、纵向布局、分栏布局配置
- 项目其它功能：菜单手风琴模式、无限级菜单、多标签页（拖拽）、详情页标签、面包屑导航、页面水印、ECharts 组件封装、SVG 图标组件、数据大屏…
- 使用 Prettier 统一格式化代码，集成 Eslint、Stylelint 代码校验规范
- 使用 husky、lint-staged、commitlint、czg、cz-git 规范代码提交信息
- 支持Keepalive页面切换不刷新

### 后端主要功能 🔨

- 采用`仓储+服务+接口`的形式封装框架；
- 异步 async/await 开发；  
- 支持自由切换多种数据库，MySql/SqlServer/Sqlite/Oracle/Postgresql/达梦/人大金仓；
- 实现数据库主键类型配置化，什么类型都可以自定义 ✨； 
- 五种日志记录，审计/异常/请求响应/服务操作/Sql记录等,并自动持久化到数据库表🎶； 
- 支持项目事务处理（若要分布式，用cap即可）✨；
- 设计4种 AOP 切面编程，功能涵盖：日志、缓存、审计、事务 ✨；
- 或使用 DbFirst 一键创建自己项目的四层文件（支持多库）；
- 实现分表案例，支持分表的增删改查哈分页查询，具体查看SplitDemoController.cs;
- 支持signalR对指定用户通讯; 
- 认证：集成Cookies、JWT；默认启用 JWT，支持多终端认证系统
- 授权：[基于策略（Policy）的授权](https://docs.microsoft.com/zh-cn/aspnet/core/security/authorization/policies?view=aspnetcore-6.0)
- ORM：[EF Core](https://docs.microsoft.com/zh-cn/ef/core/) 的 [Code First 模式](https://docs.microsoft.com/zh-cn/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli)、接入国产数据库ORM组件 —— SqlSugar，封装数据库操作，支持级联操作
- 依赖注入：默认 DI 容器，实现自动注入
- 提供 Redis 做缓存处理
- 使用 Swagger 做API文档
- 支持 CORS 跨域
- 事件总线：[默认启用 BackgroupService](https://docs.microsoft.com/zh-cn/dotnet/core/extensions/queue-service?source=recommendations)，基于[Channel](https://docs.microsoft.com/zh-cn/dotnet/api/system.threading.channels.channel-1) 实现的单机版发布订阅；可替换为 Redis 的发布订阅（可用于分布式）；也可替换为 RabbitMQ 的发布订阅（可用于分布式）
- 定时任务：使用 Quartz.net 做任务调度
- 对象映射：AutoMapper
- RabbitMQ 消息队列
- EventBus 事件总线  
- 支持加载动态权限菜单，多方式轻松权限控制。
- 数据库：SQL Server2014,设计文档见（EU.Web/Model）,依托于PowerDesigner进行数据库设计
- 所有基础列表查询通过数据库脚本配置，实现自定义查询，包括列的显示、类型、是否允许导出、宽度、顺序等
- 自定义导入导出，实现常规操作
- 权限设计：用户关联角色，角色关联模块（菜单）
- 
## 数据库设计

![image-20230602140542](./doc/images/20230602140542.png)
![image-20230602140529](./doc/images/20230602140529.png)
![image-20230602140537](./doc/images/20230602140537.png)

[数据库设计文件](./model)

## 部署

前端利用Nginx部署，后端是用IIS

## 容器化部署
[Docker部署](./doc/Docker部署.md)
开发环境发布工具、生产环境运维工具开发中

## 相关技术文档

### TypeScript
https://www.tslang.cn/docs/home.html

### React Js
https://react.docschina.org/docs/getting-started.html

### Ant Design 
https://ant.design/components/overview-cn/

### Ant Design Pro
https://pro.ant.design/zh-CN/docs/overview

### Ant Design Chart
https://charts.ant.design/zh

### Senparc
https://github.com/JeffreySu/WeiXinMPSDK

https://sdk.weixin.senparc.com/

感谢这些优秀的开源项目！

## 一些Q&A

#### 为什么前端用React?

答：现在国内大部分都是用vue，我个人可能比较喜欢react的语法吧，喜欢ant-design react版本，vue也会写写，后面会尝试深入学习vue

## 贡献

- 提 Issue 请到 gitee

## 联系我

邮箱：xiaochanghai@foxmail.com

部分内容来源与其他开源作者，谢谢

## 感谢

苏州市创采软件有限公司 费鹏先生