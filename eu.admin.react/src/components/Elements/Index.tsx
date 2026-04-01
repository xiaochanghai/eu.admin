import React, { useMemo, useCallback } from "react";
import { Alert } from "antd";
import Input from "./Input"; // 单行文本框
import InputNumber from "./InputNumber"; // 数字输入框
import ComboBox from "./ComBoBox"; // 下拉框
import ComboGrid from "./ComboGrid"; // 下拉网格
import Switch from "./Switch"; // 开关
import Checkbox from "./Checkbox"; // 多选框
import Radio from "./Radio"; // 单选框
import TextArea from "./TextArea"; // 多行文本框
import {
  DatePickerField as DatePicker,
  DateTimePickerField as DateTimePicker,
  TimePickerField as TimePicker
} from "./DatePicker"; // 日期选择器
import ColorPicker from "./ColorPicker"; // 颜色选择器
import ImageCover from "./ImageCover"; // 封面图
import { FieldProps, ModifyType } from "@/typings";

/**
 * 字段布局组件属性接口
 */
interface FieldLayoutProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 值变更回调 */
  onChange?: (value: any, option?: any) => void;
  /** 父列名，用于联动查询 */
  parentColumn?: string;
  /** 父ID，用于联动查询 */
  parentId?: string | number | null;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  style?: React.CSSProperties | undefined;
}

/**
 * 支持的字段类型列表
 */
export const SUPPORTED_FIELD_TYPES = [
  "Input",
  "InputNumber",
  "ComboBox",
  "ComboGrid",
  "Switch",
  "Checkbox",
  "Radio",
  "TextArea",
  "DatePicker",
  "ColorPicker",
  "ImageCover",
  "DateTimePicker",
  "TimePicker"
] as const;

/**
 * 字段组件映射表
 */
const FIELD_MAP: Record<string, React.ComponentType<any>> = {
  Input,
  InputNumber,
  ComboBox,
  ComboGrid,
  Switch,
  Checkbox,
  Radio,
  TextArea,
  DatePicker,
  DateTimePicker,
  TimePicker,
  ColorPicker,
  ImageCover
};

/**
 * 字段布局组件
 *
 * 功能：根据字段类型动态渲染对应的表单组件
 * 特性：
 * 1. 支持多种表单控件类型
 * 2. 统一的属性传递和事件处理
 * 3. 错误处理和降级显示
 * 4. 使用 React.memo 优化性能
 * 5. 支持父子级联动查询
 *
 * @param props - 组件属性
 * @returns React组件
 */
const FieldLayout: React.FC<FieldLayoutProps> = React.memo(
  ({ field, disabled, onChange, parentColumn, parentId, modifyType = ModifyType.Edit, style }) => {
    // 验证字段配置
    const isValidField = useMemo(() => {
      return field && field.FieldType && field.DataIndex;
    }, [field]);

    // 根据字段类型获取对应的组件
    const Component = useMemo(() => {
      if (!isValidField || !field.FieldType) return null;
      return FIELD_MAP[field.FieldType] || null;
    }, [field?.FieldType, isValidField]);

    // 处理错误情况的回调
    const handleError = useCallback(
      (error: Error) => {
        console.error(`字段组件渲染错误 [${field?.DataIndex}]:`, error);
      },
      [field?.DataIndex]
    );

    // 如果字段配置无效
    if (!isValidField)
      return <Alert title="字段配置错误" description={`字段配置无效: ${JSON.stringify(field)}`} type="error" showIcon />;

    // 如果找不到对应的组件
    if (!Component) {
      const supportedTypes = SUPPORTED_FIELD_TYPES.join(", ");
      return (
        <Alert
          title="不支持的字段类型"
          description={
            <div>
              <p>字段类型 "{field.FieldType}" 暂不支持</p>
              <p>支持的类型: {supportedTypes}</p>
              <p>字段标题: {field.FormTitle || field.DataIndex}</p>
            </div>
          }
          type="warning"
          showIcon
        />
      );
    }

    // 渲染对应的组件
    try {
      return (
        <Component
          field={field}
          disabled={disabled}
          onChange={onChange}
          parentColumn={parentColumn}
          parentId={parentId}
          modifyType={modifyType}
          style={style}
        />
      );
    } catch (error) {
      handleError(error as Error);
      return (
        <Alert
          title="组件渲染失败"
          description={`字段 "${field.FormTitle || field.DataIndex}" 渲染时发生错误`}
          type="error"
          showIcon
        />
      );
    }
  }
);

// 设置显示名称，便于调试
FieldLayout.displayName = "FieldLayout";

export default FieldLayout;
