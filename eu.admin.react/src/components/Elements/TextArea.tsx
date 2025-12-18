import React, { useCallback, useMemo } from "react";
import { Input, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";

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
 * 功能：封装Antd TextArea组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持必填验证
 * 2. 支持默认值设置
 * 3. 支持禁用状态
 * 4. 自动处理字段标题和提示信息
 * 5. 支持最小行数设置
 * 6. 支持自定义标签布局
 * 7. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const TextAreaField: React.FC<TextAreaFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange, style }) => {
  const {
    DefaultValue,
    DataIndex,
    Placeholder,
    Required,
    Disabled,
    ModifyDisabled,
    MaxLength,
    LabelCol,
    WrapperCol,
    MinRows,
    FormTitle
  } = field;

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
        message: `请输入${FormTitle}!`
      }
    ],
    [Required, FormTitle]
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
        placeholder={Placeholder ?? "请输入"}
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
