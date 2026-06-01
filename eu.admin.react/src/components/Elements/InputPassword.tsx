import React, { useCallback, useMemo } from "react";
import { Input, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";

import { EyeInvisibleOutlined, EyeTwoTone } from '@ant-design/icons';
const FormItem = Form.Item;

/**
 * 输入框组件属性类型定义
 */
interface InputPasswordFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  /** 值变更回调函数 */
  onChange?: (value: string) => void;
}

/**
 * 输入框表单组件
 * 功能：封装Antd Input组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持多种修改类型（新增/编辑/查看）
 * 2. 内置表单验证规则
 * 3. 支持禁用状态和清除功能
 * 4. 自动处理字段标题和提示信息
 * 5. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const InputPasswordField: React.FC<InputPasswordFieldProps> = ({ field, disabled = false, modifyType = ModifyType.Edit, onChange }) => {
  // 解构字段属性
  const { DefaultValue, DataIndex, Placeholder, Required, Disabled, MaxLength, ModifyDisabled, AllowClear, FormTitle } = field;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  // 是否显示清除按钮
  const showClear = useMemo(() => AllowClear === true, [AllowClear]);

  // 处理输入变化
  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange?.(e.target.value);
    },
    [onChange]
  );

  // 验证规则
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `请输入${FormTitle ?? "该字段"}!`
      }
    ],
    [Required, FormTitle]
  );

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={DefaultValue ?? undefined}>
      <Input.Password
        placeholder={Placeholder ?? "请输入"}
        disabled={isDisabled}
        maxLength={MaxLength ?? undefined}
        allowClear={showClear}
        onChange={handleChange}
        style={{ width: "100%" }} iconRender={(visible) => (visible ? <EyeTwoTone /> : <EyeInvisibleOutlined />)}

      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(InputPasswordField);
