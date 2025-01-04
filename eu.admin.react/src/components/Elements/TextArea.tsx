import React from "react";
import { Input, Form } from "antd";
const { TextArea } = Input;
const FormItem = Form.Item;
import FieldTitle from "./FieldTitle";

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { DefaultValue, DataIndex, Placeholder, Required, Disabled, MaxLength, LabelCol, WrapperCol, MinRows, FormTitle } = field;

  return (
    <FormItem
      labelCol={
        LabelCol
          ? {
              xs: { span: LabelCol },
              sm: { span: LabelCol },
              md: { span: LabelCol }
            }
          : {}
      }
      wrapperCol={
        WrapperCol
          ? {
              xs: { span: WrapperCol },
              sm: { span: WrapperCol },
              md: { span: WrapperCol }
            }
          : {}
      }
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
      initialValue={DefaultValue ?? null}
    >
      <TextArea
        placeholder={Placeholder ?? "请输入"}
        // autoSize={{ minRows: 6 }}
        disabled={disabled ?? Disabled}
        maxLength={MaxLength ?? null}
        autoSize={MinRows ? { minRows: MinRows } : {}}
      />
    </FormItem>
  );
};
export default InputField;
