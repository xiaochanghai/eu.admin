import React from "react";
import { Switch, Form } from "antd";
import FieldTitle from "./FieldTitle";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
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
export default InputField;
