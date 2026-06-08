import React, { useCallback, useMemo } from "react";
import { Form } from "antd";
import { ComboGrid } from "@/components";
import FieldTitle from "./FieldTitle";
import { FieldProps, SmLovData, ModifyType } from "@/typings";
import { RootState, useSelector } from "@/redux";
import { useTranslation } from "react-i18next";

const FormItem = Form.Item;

/**
 * ComboGrid组件属性接口定义
 */
interface ComboGridFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 值变更回调函数 */
  onChange?: (value: string, option: any, record?: SmLovData[] | null) => void;
  /** 父级列名，用于联动查询 */
  parentColumn?: string;
  /** 父级ID，用于联动查询 */
  parentId?: string | number;
  /** 修改类型 */
  modifyType?: ModifyType;
}

/**
 * 下拉表格选择框组件
 */
const ComboGridField: React.FC<ComboGridFieldProps> = ({
  field,
  disabled,
  onChange,
  parentColumn,
  parentId,
  modifyType = ModifyType.Edit
}) => {
  const { DataIndex, Placeholder, Placeholder_EN, Required, DataSource, Disabled, ModifyDisabled, FormTitle, FormTitle_EN, IsMultiple, MultipleMaxCount } = field;
  const language = useSelector((state: RootState) => state.global.language);
  const { t } = useTranslation();
  const placeholder = (language === "en" ? Placeholder_EN : Placeholder) || t("formOption.selectPlaceholder");
  const formTitle = language === "en" ? FormTitle_EN || FormTitle : FormTitle;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  /**
   * 处理值变更事件
   */
  const handleChange = useCallback(
    (value: string | null, option: any, record?: SmLovData[] | null) => {
      onChange?.(value ?? "", option, record);
    },
    [onChange]
  );

  // 验证规则
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `${placeholder}${formTitle}!`
      }
    ],
    [Required, formTitle]
  );

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules}>
      <ComboGrid
        code={DataSource}
        disabled={isDisabled}
        onChange={handleChange}
        parentColumn={parentColumn}
        parentId={parentId}
        placeholder={placeholder}
        mode={IsMultiple ? "multiple" : undefined}
        maxCount={IsMultiple && MultipleMaxCount ? MultipleMaxCount : undefined}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(ComboGridField);
