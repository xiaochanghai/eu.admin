import React, { useMemo } from "react";
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

const FIELD_MAP = {
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

const Base = ({ field, IsView, onChange, parentColumn, parentId }) => {
  const Component = useMemo(() => {
    return FIELD_MAP[field?.FieldType];
  }, [field?.FieldType]);

  return Component ? (
    <Component field={field} isView={IsView} onChange={onChange} parentColumn={parentColumn} parentId={parentId} />
  ) : null;
};

export default Base;
