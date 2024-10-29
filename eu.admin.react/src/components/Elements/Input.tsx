import { Input, Form } from "antd";
import { ModifyType } from "@/api/interface/index";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  let { field, disabled, modifyType } = props;
  // debugger;
  const { FormTitle, DefaultValue, DataIndex, Placeholder, Required, Disabled, MaxLength, ModifyDisabled } = field;

  if ((modifyType == ModifyType.Edit && ModifyDisabled) || modifyType == ModifyType.View) disabled = true;
  if (Disabled) disabled = true;

  return (
    <FormItem name={DataIndex} label={FormTitle} rules={[{ required: Required ?? false }]} initialValue={DefaultValue ?? null}>
      <Input placeholder={Placeholder ?? "请输入"} disabled={disabled} maxLength={MaxLength ?? null} />
    </FormItem>
  );
};
export default InputField;
