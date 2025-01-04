import React from "react";
import { InputNumber, Form } from "antd";
import FieldTitle from "./FieldTitle";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { FormTitle, DefaultValue, DataIndex, Placeholder, Required, Minimum, Maximum, Disabled } = field;
  let rules: any[] = [{ required: Required ?? false, message: `请输入${FormTitle}!` }];
  let min = Minimum ?? null;
  let max = Maximum ?? null;
  if (min != null) rules.push({ type: "number", min: min, message: FormTitle + "最小值为" + min + "!" });
  if (max != null) rules.push({ type: "number", max: max, message: FormTitle + "最大值为" + max + "!" });
  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={rules}
      initialValue={DefaultValue ?? null}
    >
      <InputNumber placeholder={Placeholder ?? "请输入"} disabled={disabled ?? Disabled} />
    </FormItem>
  );
};
export default InputField;
