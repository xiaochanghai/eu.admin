import { Form } from "antd";
import ComboGrid from "@/components/ComBoGrid/index";
import { ModifyType } from "@/api/interface/index";
import FieldTitle from "./FieldTitle";

const FormItem = Form.Item;

const InputField: React.FC<any> = props => {
  let { field, disabled, onChange, parentColumn, parentId, modifyType } = props;
  const { DataIndex, Placeholder, Required, DataSource, Disabled, ModifyDisabled, FormTitle } = field;
  if ((modifyType == ModifyType.Edit && ModifyDisabled) || Disabled) disabled = true;

  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
    >
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
