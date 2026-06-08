import React, { useCallback, useMemo } from "react";
import { Switch, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";
import { RootState, useSelector } from "@/redux";
import { useTranslation } from "react-i18next";

const FormItem = Form.Item;

/**
 * Switch组件属性接口定义
 */
interface SwitchFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  /** 值变更回调函数 */
  onChange?: (checked: boolean) => void;
}

/**
 * 开关组件
 */
const SwitchField: React.FC<SwitchFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange }) => {
  const { DefaultValue, DataIndex, Required, Disabled, ModifyDisabled, FormTitle, FormTitle_EN } = field;
  const language = useSelector((state: RootState) => state.global.language);
  const formTitle = language === "en" ? FormTitle_EN || FormTitle : FormTitle;
  const { t } = useTranslation();

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  // 处理默认值，支持多种格式
  const defaultChecked = useMemo(() => {
    if (DefaultValue === undefined || DefaultValue === null) return false;
    if (typeof DefaultValue === "boolean") return DefaultValue;
    if (typeof DefaultValue === "string") {
      return DefaultValue.toLowerCase() === "true" || DefaultValue === "1";
    }
    if (typeof DefaultValue === "number") return DefaultValue === 1;
    return Boolean(DefaultValue);
  }, [DefaultValue]);

  // 处理开关变化
  const handleChange = useCallback(
    (checked: boolean) => {
      onChange?.(checked);
    },
    [onChange]
  );

  // 验证规则
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `${t("formOption.selectPlaceholder")}${formTitle ?? null}!`
      }
    ],
    [Required, formTitle]
  );

  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle {...field} />}
      rules={validationRules}
      valuePropName="checked"
      initialValue={defaultChecked}
    >
      <Switch disabled={isDisabled} onChange={handleChange} />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(SwitchField);
