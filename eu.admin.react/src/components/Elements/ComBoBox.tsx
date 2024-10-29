import { Form } from "antd";
import ComBoBox from "@/components/ComBoBox/index";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  let { field, disabled, onChange } = props;
  const { FormTitle, DefaultValue, DataIndex, Placeholder, Required, DataSource, Disabled } = field;
  if (Disabled) disabled = true;
  return (
    <FormItem name={DataIndex} label={FormTitle} rules={[{ required: Required ?? false }]}>
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
