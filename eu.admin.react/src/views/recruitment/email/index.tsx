import React from "react";
import Main from "../../system/common/main";

/**
 * 颜色管理模块
 *
 * 该组件使用通用的Main组件加载颜色管理模块信息并渲染对应的表单。
 * Main组件会处理模块信息的获取和状态管理，并根据模块配置渲染适当的界面。
 *
 * 模块代码说明：
 * - RM_RECRUITMENT_EMAIL_MNG: 基础数据-邮件管理模块代码
 */
const Index: React.FC = React.memo(() => {
  return <Main moduleCode="RM_RECRUITMENT_EMAIL_MNG" />;
});

export default Index;
