/**
 * ProTable 组件常量配置
 */

/**
 * 默认操作按钮样式
 */
export const DEFAULT_ACTION_BUTTON_STYLE = {
  border: 0,
  background: "transparent",
  boxShadow: "0 0px 0 rgb(255 255 255 / 2%)",
  marginRight: 8
} as const;

/**
 * 操作列配置
 */
export const ACTION_COLUMN_CONFIG = {
  title: "操作",
  dataIndex: "option",
  fixed: "left" as const,
  valueType: "option" as const,
  width: 100,
  align: "center" as const
};

/**
 * 确认对话框消息
 */
export const CONFIRM_MESSAGES = {
  DELETE_SINGLE: "是否确定删除记录?",
  DELETE_BATCH: "你确定需要批量删除所选数据吗？",
  AUDIT_SINGLE: "你确定需要提交该数据吗？",
  AUDIT_BATCH: "你确定需要批量提交所选数据吗？",
  REVOCATION_SINGLE: "你确定需要撤销该数据吗？",
  REVOCATION_BATCH: "你确定需要批量撤销所选数据吗？"
} as const;

/**
 * 加载提示消息
 */
export const LOADING_MESSAGES = {
  SUBMITTING: "数据提交中...",
  EXPORTING: "后台处理中，处理完成将自动下载！"
} as const;

/**
 * 表格滚动配置
 */
export const TABLE_SCROLL_CONFIG = {
  scrollToFirstRowOnChange: true,
  y: "100%"
} as const;

/**
 * 汇总行 ID
 */
export const SUM_ROW_ID = "SumRowID";

/**
 * 确认对话框按钮文本
 */
export const CONFIRM_BUTTON_TEXT = {
  OK: "确定",
  CANCEL: "取消"
} as const;
