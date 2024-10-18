import { Form } from "antd";
import ComboGrid from "@/components/ComBoGrid/index";
import { ModifyType } from "@/api/interface/index";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  let { field, disabled, onChange, parentColumn, parentId, modifyType } = props;
  const { FormTitle, DataIndex, Placeholder, Required, DataSource, Disabled, ModifyDisabled } = field;
  if ((modifyType == ModifyType.Edit && ModifyDisabled) || Disabled) disabled = true;

  return (
    <FormItem name={DataIndex} label={FormTitle} rules={[{ required: Required ?? false }]}>
      <ComboGrid
        code={DataSource}
        disabled={disabled}
        onChange={onChange}
        parentColumn={parentColumn}
        parentId={parentId}
        placeholder={Placeholder ?? ""}
      />
    </FormItem>
  );
};
export default InputField;
