import React, { useMemo } from "react";
import { ModifyType } from "@/api/interface/index";
import Input from "./Input"; // 单行文本框
import InputNumber from "./InputNumber"; // 数字输入框
import ComboBox from "./ComboBox"; // 下拉框
import ComboGrid from "./ComboGrid"; // 下拉网格
import Switch from "./Switch"; // 开关
import TextArea from "./TextArea"; // 多行文本框
import DatePicker from "./DatePicker"; // 日期选择器
import ColorPicker from "./ColorPicker"; // 颜色选择器
import { FieldProps } from "@/typings";

// 暂未实现的组件
// import Radio from "./Radio"; // 单选框
// import Checkbox from "./Checkbox"; // 多选框

/**
 * 字段布局组件属性接口
 */
interface FieldLayoutProps {
  field: FieldProps; // 字段配置
  disabled?: boolean; // 是否禁用
  onChange?: (value: any, option?: any) => void; // 值变更回调
  parentColumn?: string; // 父列名
  parentId?: string | number | null; // 父ID
  modifyType?: ModifyType; // 修改类型
}

/**
 * 字段组件映射表
 */
const FIELD_MAP: Record<string, React.ComponentType<any>> = {
  Input,
  InputNumber,
  ComboBox,
  ComboGrid,
  Switch,
  TextArea,
  DatePicker,
  ColorPicker
  // Radio,
  // Checkbox,
};

/**
 * 字段布局组件
 *
 * 根据字段类型动态渲染对应的表单组件
 * 支持多种表单控件类型，如输入框、下拉框、开关、日期选择器等
 *
 * @param props 组件属性
 */
const FieldLayout: React.FC<FieldLayoutProps> = React.memo(props => {
  const { field, disabled, onChange, parentColumn, parentId, modifyType } = props;

  // 根据字段类型获取对应的组件
  const Component = useMemo(() => {
    return field?.FieldType ? FIELD_MAP[field.FieldType] : undefined;
  }, [field?.FieldType]);

  // 如果找到对应的组件则渲染，否则显示字段名称
  return Component ? (
    <Component
      field={field}
      disabled={disabled}
      onChange={onChange}
      parentColumn={parentColumn}
      parentId={parentId}
      modifyType={modifyType}
    />
  ) : (
    <span>{field.FormTitle || field.DataIndex}</span>
  );
});

export default FieldLayout;
