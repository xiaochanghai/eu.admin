import { Input, Form } from "antd";
import { ModifyType } from "@/api/interface/index";
import FieldTitle from "./FieldTitle";
import { FieldProps } from "@/typings";

const FormItem = Form.Item;

/**
 * 输入框组件属性类型定义
 */
interface InputFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
}

/**
 * 输入框表单组件
 * 功能：封装Antd Input组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持多种修改类型（新增/编辑/查看）
 * 2. 内置表单验证规则
 * 3. 支持禁用状态和清除功能
 * 4. 自动处理字段标题和提示信息
 */
const InputField: React.FC<InputFieldProps> = ({ field, disabled = false, modifyType = ModifyType.Edit }) => {
  // 解构字段属性
  const { DefaultValue, DataIndex, Placeholder, Required, Disabled, MaxLength, ModifyDisabled, AllowClear, FormTitle } = field;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled;

  // 是否显示清除按钮
  const showClear = AllowClear === true;

  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[
        {
          required: Required ?? false,
          message: `请输入${FormTitle ?? "该字段"}!`
        }
      ]}
      initialValue={DefaultValue ?? null}
    >
      <Input
        placeholder={Placeholder ?? "请输入"}
        disabled={disabled ?? isDisabled}
        maxLength={MaxLength ?? undefined}
        allowClear={showClear}
      />
    </FormItem>
  );
};

export default InputField;
