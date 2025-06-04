/**
 * 视图类型定义
 * FormIndex: 列表视图
 * FormPage: 表单视图
 */
export enum ViewType {
  INDEX = "FormIndex",
  PAGE = "FormPage",
  /** SQL编辑视图 */
  SQL_EDIT = "SqlEdit",
  /** 表单配置视图 */
  FORM_COLLOCATE = "FormCollocate"
}
/**
 * 编辑页打开方式
 */
export enum EditOpenType {
  Modal = "Modal",
  Drawer = "Drawer"
}
export interface FieldProps {
  FieldType?: string;

  /** 默认值 */
  DefaultValue?: string;
  /** 数据索引/字段名 */
  DataIndex: string;
  /** 占位符文本 */
  Placeholder?: string;
  /** 是否必填 */
  Required?: boolean;
  /** 数据源ID，用于获取下拉选项 */
  DataSource?: string;
  /** 是否禁用 */
  Disabled?: boolean;
  /** 表单标题 */
  FormTitle: string;
  /** 是否显示提示 */
  IsTooltip?: boolean;
  /** 提示内容 */
  TooltipContent?: string;
  /** 编辑是否禁用 */
  ModifyDisabled?: boolean;
  /** 日期格式化字符串 */
  DataFormate?: string;
  /** 是否允许清除 */
  AllowClear?: boolean;
  /** 最大长度限制 */
  MaxLength?: number;
  /** 最小值限制 */
  Minimum?: number;
  /** 最大值限制 */
  Maximum?: number;
  LabelCol?: number;
  WrapperCol?: number;
  MinRows?: number;
}
