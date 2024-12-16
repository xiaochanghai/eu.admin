import React from "react";
import { DatePicker, Form } from "antd";
import dayjs from "dayjs";

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { FormTitle, DefaultValue, DataIndex, Placeholder, Required, Disabled, DataFormate, AllowClear } = field;
  let allowClear = AllowClear === true ? true : false;

  return (
    <Form.Item
      name={DataIndex}
      label={FormTitle}
      rules={[{ required: Required ?? false }]}
      initialValue={DefaultValue ?? null}
      getValueProps={value => ({ value: value && dayjs(value) })}
    >
      <DatePicker disabled={disabled ?? Disabled} format={DataFormate} placeholder={Placeholder} allowClear={allowClear} />
    </Form.Item>
  );
};
export default InputField;
