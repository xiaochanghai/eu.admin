import React, { useCallback, useMemo } from "react";
import { Input, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";
import { RootState, useSelector } from "@/redux";
import { useTranslation } from "react-i18next";

const { TextArea } = Input;
const FormItem = Form.Item;

/**
 * TextArea组件属性接口定义
 */
interface TextAreaFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  /** 值变更回调函数 */
  onChange?: (value: string) => void;
  style?: React.CSSProperties | undefined;
}

/**
 * 多行文本输入框组件
 */
const TextAreaField: React.FC<TextAreaFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange, style }) => {
  const {
    DefaultValue,
    DataIndex,
    Placeholder,
    Placeholder_EN,
    Required,
    Disabled,
    ModifyDisabled,
    MaxLength,
    LabelCol,
    WrapperCol,
    MinRows,
    FormTitle,
    FormTitle_EN
  } = field;
  const language = useSelector((state: RootState) => state.global.language);
  const { t } = useTranslation();
  const placeholder = (language === "en" ? Placeholder_EN : Placeholder) || t("formOption.inputPlaceholder");

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  // 处理文本变化
  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLTextAreaElement>) => {
      onChange?.(e.target.value);
    },
    [onChange]
  );

  // 验证规则
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `请输入${language === "en" ? FormTitle_EN || FormTitle : FormTitle}!`
      }
    ],
    [Required, FormTitle, FormTitle_EN, language]
  );

  // 标签列配置
  const labelColConfig = useMemo(() => {
    return LabelCol
      ? {
          xs: { span: LabelCol },
          sm: { span: LabelCol },
          md: { span: LabelCol }
        }
      : undefined;
  }, [LabelCol]);

  // 包装列配置
  const wrapperColConfig = useMemo(() => {
    return WrapperCol
      ? {
          xs: { span: WrapperCol },
          sm: { span: WrapperCol },
          md: { span: WrapperCol }
        }
      : undefined;
  }, [WrapperCol]);

  // 自动调整大小配置
  const autoSizeConfig = useMemo(() => {
    return MinRows ? { minRows: MinRows } : { minRows: 3 };
  }, [MinRows]);

  return (
    <FormItem
      labelCol={labelColConfig}
      wrapperCol={wrapperColConfig}
      name={DataIndex}
      label={<FieldTitle {...field} />}
      rules={validationRules}
      initialValue={DefaultValue ?? undefined}
      style={style}
    >
      <TextArea
        placeholder={placeholder}
        disabled={isDisabled}
        maxLength={MaxLength ?? undefined}
        autoSize={autoSizeConfig}
        onChange={handleChange}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(TextAreaField);
