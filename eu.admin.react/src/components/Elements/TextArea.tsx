import React from "react";
import { Input, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps } from "@/typings";

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
 *
 * @param props - 组件属性
 */
const TextAreaField: React.FC<TextAreaFieldProps> = props => {
  const { field, disabled } = props;
  const { DefaultValue, DataIndex, Placeholder, Required, Disabled, MaxLength, LabelCol, WrapperCol, MinRows, FormTitle } = field;

  return (
    <FormItem
      labelCol={
        LabelCol
          ? {
              xs: { span: LabelCol },
              sm: { span: LabelCol },
              md: { span: LabelCol }
            }
          : undefined
      }
      wrapperCol={
        WrapperCol
          ? {
              xs: { span: WrapperCol },
              sm: { span: WrapperCol },
              md: { span: WrapperCol }
            }
          : undefined
      }
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
      initialValue={DefaultValue ?? null}
    >
      <TextArea
        placeholder={Placeholder ?? "请输入"}
        disabled={disabled ?? Disabled}
        maxLength={MaxLength ?? undefined}
        autoSize={MinRows ? { minRows: MinRows } : undefined}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(TextAreaField);
