import React from "react";
import { Switch, Form } from "antd";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { FormTitle, DefaultValue, DataIndex, Required, Disabled } = field;
  return (
    <FormItem name={DataIndex} label={FormTitle} rules={[{ required: Required ?? false }]} valuePropName="checked">
      <Switch checked={DefaultValue == "true" ? true : false} disabled={disabled ?? Disabled} />
    </FormItem>
  );
};
export default InputField;
