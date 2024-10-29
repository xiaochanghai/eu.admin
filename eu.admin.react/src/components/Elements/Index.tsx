// const NotFound: React.FC<any> = props => {
//   // const {} = props;
//   return <p>1111</p>;
// };

// export default NotFound;
import { useMemo } from "react";
import Input from "./Input"; //单行文本框
import InputNumber from "./InputNumber"; //单行文本框
import ComboBox from "./ComBoBox"; //
import ComboGrid from "./ComboGrid"; //
import Switch from "./Switch"; //开关
import TextArea from "./TextArea"; //多行文本框
import DatePicker from "./DatePicker"; //日期
import ColorPicker from "./ColorPicker"; //日期

// import Radio from "./Radio"; //单选框
// import Checkbox from "./Checkbox"; //多选框

const Layout: React.FC<any> = props => {
  const { field, disabled, onChange, parentColumn, parentId, modifyType } = props;
  // debugger;
  const FIELD_MAP: any = {
    Input,
    InputNumber,
    ComboBox,
    ComboGrid,
    Switch,
    TextArea,
    DatePicker,
    ColorPicker
    // Radio,
    // Checkbox,
  };

  const Component = useMemo(() => {
    return FIELD_MAP[field?.FieldType];
  }, [field?.FieldType]);
  return Component ? (
    <Component
      field={field}
      disabled={disabled}
      onChange={onChange}
      parentColumn={parentColumn}
      parentId={parentId}
      modifyType={modifyType}
    />
  ) : (
    <span>{field.FormTitle || field.DataIndex}</span>
  );
};
export default Layout;
