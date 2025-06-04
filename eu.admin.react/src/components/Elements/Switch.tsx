import React from "react";
import { Switch, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps } from "@/typings";

const FormItem = Form.Item;

/**
 * Switch组件属性接口定义
 */
interface SwitchFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
}

/**
 * 开关组件
 * 功能：封装Antd Switch组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持必填验证
 * 2. 支持默认值设置
 * 3. 支持禁用状态
 * 4. 自动处理字段标题和提示信息
 *
 * @param props - 组件属性
 */
const SwitchField: React.FC<SwitchFieldProps> = props => {
  const { field, disabled } = props;
  const { DefaultValue, DataIndex, Required, Disabled, FormTitle } = field;

  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
      valuePropName="checked"
    >
      <Switch checked={DefaultValue == "true" ? true : false} disabled={disabled ?? Disabled} />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(SwitchField);
