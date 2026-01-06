import { createElement } from "react";
import { ModuleInfo, ModuleInfoBeforeAction } from "@/api/interface/index";
import { Icon } from "@/components";

/**
 * 构建权限按钮映射表
 * @param actions 操作权限数组
 * @returns 权限映射对象
 */
export const buildAuthButtonMap = (actions?: string[]): Record<string, boolean> => {
  const map: Record<string, boolean> = {};
  actions?.forEach((item: any) => {
    map[item] = true;
  });
  return map;
};

/**
 * 构建行操作权限映射表
 * @param beforeActions 行前操作数组
 * @returns 权限映射对象
 */
export const buildOptionAuthMap = (beforeActions?: ModuleInfoBeforeAction[]): Record<string, boolean> => {
  const map: Record<string, boolean> = {};
  beforeActions?.forEach((item: any) => (map[item.id] = true));
  return map;
};

/**
 * 构建下拉操作权限映射表
 * @param dropActions 下拉操作数组
 * @returns 权限映射对象
 */
export const buildDropActionAuthMap = (dropActions?: any[]): Record<string, boolean> => {
  const map: Record<string, boolean> = {};
  dropActions?.forEach((item: any) => (map[item.id] = true));
  return map;
};

/**
 * 获取下拉菜单项
 * @param record 当前行数据
 * @param action ProTable action对象
 * @param dropActions 下拉操作配置
 * @param IsView 是否查看模式
 * @param onEdit 编辑回调
 * @param onView 查看回调
 * @param onDelete 删除回调
 * @param moduleInfo 模块信息
 * @param customActionHandlers 自定义操作处理函数映射
 * @returns 菜单项数组
 */
export const getDropActionItems = (
  record: any,
  action: any,
  dropActions: any[],
  IsView: boolean,
  onEdit: (record: any) => void,
  onView: (record: any) => void,
  onDelete: (action: any, record: any) => void,
  moduleInfo: ModuleInfo,
  customActionHandlers: Record<string, (id: string, action: any, record: any) => void>
) => {
  return dropActions
    .map((item: ModuleInfoBeforeAction) => {
      // 处理修改操作
      if (item.id === "Update" && !IsView) {
        return {
          key: "dropActionUpdate",
          label: "修改",
          onClick: () => onEdit(record)
        };
      }

      // 处理查看操作
      if (item.id === "View") {
        return {
          key: "dropActionView",
          label: "查看",
          icon: createElement(Icon, { name: "EyeOutlined" }),
          onClick: () => onView(record)
        };
      }

      // 处理删除操作
      if (item.id === "Delete" && !IsView) {
        return {
          key: "dropActionDelete",
          label: "删除",
          icon: createElement(Icon, { name: "DeleteOutlined" }),
          onClick: () => onDelete(action, record)
        };
      }

      // 处理自定义操作
      const customAction = moduleInfo.actionData?.find((data: any) => data.ID === item.id);
      if (customAction && customActionHandlers[customAction.FunctionCode]) {
        return {
          key: `dropAction${item.id}`,
          label: customAction.FunctionName,
          icon: customAction.Icon ? createElement(Icon, { name: customAction.Icon }) : undefined,
          onClick: () => customActionHandlers[customAction.FunctionCode](record.ID, action, record)
        };
      }

      return null;
    })
    .filter(Boolean);
};

/**
 * 检查是否有编辑权限（用于双击编辑）
 * @param moduleInfo 模块信息
 * @returns 是否有编辑权限
 */
export const hasUpdatePermission = (moduleInfo: ModuleInfo): boolean => {
  return (
    moduleInfo.beforeActions?.some((item: any) => item.id === "Update") ||
    moduleInfo.dropActions?.some((item: any) => item.id === "Update")
  );
};

/**
 * 计算表格滚动宽度
 * @param columnCount 列数量
 * @returns 滚动宽度
 */
export const calculateScrollWidth = (columnCount: number): number => {
  return columnCount * 100;
};
