import React from "react";
import { InputNumber, Form } from "antd";
import FieldTitle from "./FieldTitle";

import { FieldProps } from "@/typings";
import { ModifyType } from "@/api/interface/index";

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
}

/**
 * 数字输入框组件
 * 功能：封装Antd InputNumber组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持最小值/最大值验证
 * 2. 内置表单验证规则
 * 3. 支持禁用状态
 * 4. 自动处理字段标题和提示信息
 */
const InputNumberField: React.FC<InputNumberFieldProps> = props => {
  const { field, disabled, modifyType = ModifyType.Edit } = props;
  const { FormTitle, DefaultValue, DataIndex, Placeholder, Required, Minimum, Maximum, Disabled, ModifyDisabled } = field;
  const isDisabled = (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled;

  // 构建验证规则数组
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

  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={rules}
      initialValue={DefaultValue ?? null}
    >
      <InputNumber
        placeholder={Placeholder ?? "请输入"}
        disabled={disabled ?? isDisabled}
        // 设置最小值/最大值（可选，与验证规则配合使用）
        min={Minimum ?? undefined}
        max={Maximum ?? undefined}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(InputNumberField);
