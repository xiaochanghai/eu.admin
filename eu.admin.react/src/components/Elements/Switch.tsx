import React from "react";
import { Switch, Form } from "antd";
import FieldTitle from "./FieldTitle";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { DefaultValue, DataIndex, Required, Disabled } = field;
  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false }]}
      valuePropName="checked"
    >
      <Switch checked={DefaultValue == "true" ? true : false} disabled={disabled ?? Disabled} />
    </FormItem>
  );
};
export default InputField;
