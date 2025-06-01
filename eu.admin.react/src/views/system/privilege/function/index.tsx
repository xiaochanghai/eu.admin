import React from "react";
import Main from "../../common/main";

/**
 * 功能权限管理组件属性定义
 */
interface FunctionPrivilegeProps {}

/**
 * 功能权限管理组件
 *
 * 该组件用于管理系统功能权限，通过Main组件加载模块信息并渲染对应的功能界面。
 * 使用moduleCode="SM_FUNCTION_PRIVILEGE_MNG"标识功能权限管理模块。
 *
 * @returns 功能权限管理界面
 */
const FunctionPrivilege: React.FC<FunctionPrivilegeProps> = () => {
  return <Main moduleCode="SM_FUNCTION_PRIVILEGE_MNG" />;
};

// 使用React.memo优化组件，避免不必要的重渲染
export default React.memo(FunctionPrivilege);
