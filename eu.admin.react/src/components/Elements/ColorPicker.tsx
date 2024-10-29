import React from "react";
import { ColorPicker, Form } from "antd";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { FormTitle, DefaultValue, DataIndex, Required, Disabled } = field;
  return (
    <FormItem
      name={DataIndex}
      label={FormTitle}
      rules={[{ required: Required ?? false }]}
      initialValue={DefaultValue ?? null}
      normalize={value => {
        return value && `${value.toHexString()}`;
      }}
    >
      <ColorPicker disabled={disabled ?? Disabled} defaultFormat="hex" showText />
    </FormItem>
  );
};
export default InputField;
