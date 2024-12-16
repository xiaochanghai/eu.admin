import React from "react";
import { DatePicker, Form } from "antd";
import dayjs from "dayjs";
import FieldTitle from "./FieldTitle";

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { DefaultValue, DataIndex, Placeholder, Required, Disabled, DataFormate, AllowClear } = field;
  let allowClear = AllowClear === true ? true : false;

  return (
    <Form.Item
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false }]}
      initialValue={DefaultValue ?? null}
      getValueProps={value => ({ value: value && dayjs(value) })}
    >
      <DatePicker disabled={disabled ?? Disabled} format={DataFormate} placeholder={Placeholder} allowClear={allowClear} />
    </Form.Item>
  );
};
export default InputField;
