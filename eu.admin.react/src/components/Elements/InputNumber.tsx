import React, { useCallback, useMemo } from "react";
import { InputNumber, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";
import { RootState, useSelector } from "@/redux";
import { useTranslation } from "react-i18next";

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
 */
const InputNumberField: React.FC<InputNumberFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange }) => {
  const { FormTitle, FormTitle_EN, DefaultValue, DataIndex, Placeholder, Placeholder_EN, Required, Minimum, Maximum, Disabled, ModifyDisabled } = field;
  const language = useSelector((state: RootState) => state.global.language);
  const { t } = useTranslation();
  const placeholder = (language === "en" ? Placeholder_EN : Placeholder) || t("formOption.inputPlaceholder");
  const formTitle = language === "en" ? FormTitle_EN || FormTitle : FormTitle;

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
      { required: Required ?? false, message: `请输入${formTitle}!` }
    ];

    // 添加最小值验证规则
    if (Minimum != null) {
      rules.push({
        type: "number",
        min: Minimum,
        message: `${formTitle}最小值为${Minimum}!`
      });
    }

    // 添加最大值验证规则
    if (Maximum != null) {
      rules.push({
        type: "number",
        max: Maximum,
        message: `${formTitle}最大值为${Maximum}!`
      });
    }

    return rules;
  }, [Required, formTitle, Minimum, Maximum]);

  // 处理默认值
  const initialValue = useMemo(() => {
    if (DefaultValue === undefined || DefaultValue === null) return undefined;
    const numValue = Number(DefaultValue);
    return isNaN(numValue) ? undefined : numValue;
  }, [DefaultValue]);

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={initialValue}>
      <InputNumber
        placeholder={placeholder}
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
