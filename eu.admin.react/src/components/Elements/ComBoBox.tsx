import { Form } from "antd";
import { ComBoBox } from "@/components";
import FieldTitle from "./FieldTitle";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  let { field, disabled, onChange } = props;
  const { DefaultValue, DataIndex, Placeholder, Required, DataSource, Disabled, FormTitle } = field;
  if (Disabled) disabled = true;
  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
    >
      <ComBoBox
        id={DataSource ?? DataIndex}
        placeholder={Placeholder}
        defaultValue={DefaultValue}
        disabled={disabled}
        onChange={onChange}
      />
    </FormItem>
  );
};
export default InputField;
