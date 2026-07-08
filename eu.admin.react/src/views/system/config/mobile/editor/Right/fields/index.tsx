import OptionEditor from "./OptionEditor";
import { Input, InputNumber, Select, Radio } from "antd";

const fields: Record<string, React.ComponentType<any>> = {
  Text: Input,
  Number: InputNumber,
  Select: Select,
  Radio: Radio.Group,
  TextArea: Input.TextArea,
  OptionEditor: OptionEditor
};

export default fields;
