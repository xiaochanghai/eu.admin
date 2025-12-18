import React, { useCallback, useMemo } from "react";
import { InputNumber, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";

const FormItem = Form.Item;

/**
 * 数字输入框组件属性类型定义
 */
interface InputNumberFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  /** 值变更回调函数 */
  onChange?: (value: number | null) => void;
}

/**
 * 数字输入框组件
 * 功能：封装Antd InputNumber组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持最小值/最大值验证
 * 2. 内置表单验证规则
 * 3. 支持禁用状态
 * 4. 自动处理字段标题和提示信息
 * 5. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const InputNumberField: React.FC<InputNumberFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange }) => {
  const { FormTitle, DefaultValue, DataIndex, Placeholder, Required, Minimum, Maximum, Disabled, ModifyDisabled } = field;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  // 处理数字变化
  const handleChange = useCallback(
    (value: number | null) => {
      onChange?.(value);
    },
    [onChange]
  );

  // 构建验证规则数组
  const validationRules = useMemo(() => {
    const rules: any[] = [
      // 必填验证
      { required: Required ?? false, message: `请输入${FormTitle}!` }
    ];

    // 添加最小值验证规则
    if (Minimum != null) {
      rules.push({
        type: "number",
        min: Minimum,
        message: `${FormTitle}最小值为${Minimum}!`
      });
    }

    // 添加最大值验证规则
    if (Maximum != null) {
      rules.push({
        type: "number",
        max: Maximum,
        message: `${FormTitle}最大值为${Maximum}!`
      });
    }

    return rules;
  }, [Required, FormTitle, Minimum, Maximum]);

  // 处理默认值
  const initialValue = useMemo(() => {
    if (DefaultValue === undefined || DefaultValue === null) return undefined;
    const numValue = Number(DefaultValue);
    return isNaN(numValue) ? undefined : numValue;
  }, [DefaultValue]);

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={initialValue}>
      <InputNumber
        placeholder={Placeholder ?? "请输入"}
        disabled={isDisabled}
        min={Minimum ?? undefined}
        max={Maximum ?? undefined}
        onChange={handleChange}
        style={{ width: "100%" }}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(InputNumberField);
