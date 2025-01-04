import { Input, Form } from "antd";
import { ModifyType } from "@/api/interface/index";
import FieldTitle from "./FieldTitle";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  let { field, disabled, modifyType } = props;
  // debugger;
  const { DefaultValue, DataIndex, Placeholder, Required, Disabled, MaxLength, ModifyDisabled, AllowClear, FormTitle } = field;

  if ((modifyType == ModifyType.Edit && ModifyDisabled) || modifyType == ModifyType.View) disabled = true;
  if (Disabled) disabled = true;
  let allowClear = AllowClear === true ? true : false;
  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
      initialValue={DefaultValue ?? null}
    >
      <Input placeholder={Placeholder ?? "请输入"} disabled={disabled} maxLength={MaxLength ?? null} allowClear={allowClear} />
    </FormItem>
  );
};
export default InputField;
