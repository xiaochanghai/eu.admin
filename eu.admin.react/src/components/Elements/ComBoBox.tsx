import React, { useCallback, useMemo } from "react";
import { Form } from "antd";
import { ComBoBox } from "@/components";
import FieldTitle from "./FieldTitle";
import { FieldProps, SmLovData } from "@/typings";
import { ModifyType } from "@/typings";

const FormItem = Form.Item;

/**
 * ComboBox组件属性接口定义
 */
interface ComboBoxFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  /** 值变更回调函数 */
  onChange?: (value: string, option: any, record?: SmLovData[] | null) => void;
}

/**
 * 下拉选择框组件
 * 功能：封装ComBoBox组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持必填验证
 * 2. 支持禁用状态
 * 3. 自动处理字段标题和提示信息
 * 4. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const ComboBoxField: React.FC<ComboBoxFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange }) => {
  const { DefaultValue, DataIndex, Placeholder, Required, DataSource, Disabled, ModifyDisabled, FormTitle } = field;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  /**
   * 处理值变更事件
   * @param value - 选中的值
   * @param option - 选中的选项
   * @param record - 选中的记录数据
   */
  const handleChange = useCallback(
    (value: string, option: any, record?: SmLovData[] | null) => {
      onChange?.(value, option, record);
    },
    [onChange]
  );

  // 验证规则
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `请选择${FormTitle}!`
      }
    ],
    [Required, FormTitle]
  );

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={DefaultValue ?? undefined}>
      <ComBoBox
        id={DataSource ?? DataIndex} // 如果没有指定DataSource，则使用DataIndex作为数据源ID
        placeholder={Placeholder ?? "请选择"}
        disabled={isDisabled}
        onChange={handleChange}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(ComboBoxField);
