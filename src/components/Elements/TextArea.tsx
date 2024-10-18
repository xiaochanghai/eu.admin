import React from "react";
import { Input, Form } from "antd";
const { TextArea } = Input;
const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  const { field, disabled } = props;
  const { FormTitle, DefaultValue, DataIndex, Placeholder, Required, Disabled, MaxLength, LabelCol, WrapperCol, MinRows } = field;

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
      label={FormTitle}
      rules={[{ required: Required ?? false }]}
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
