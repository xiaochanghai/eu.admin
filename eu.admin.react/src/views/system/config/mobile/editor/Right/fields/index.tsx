import React, { useEffect, useState } from "react";
import OptionEditor from "./OptionEditor";
import { Input, InputNumber, Select, Radio, message } from "antd";

interface JsonEditorProps {
  value?: any;
  onChange?: (value: any) => void;
  placeholder?: string;
}

interface ColorEditorProps {
  value?: string;
  onChange?: (value: string) => void;
  placeholder?: string;
}

const normalizeColorValue = (value?: string) => {
  if (!value || !/^#[0-9a-fA-F]{6}$/.test(value)) return "#f9fafb";
  return value;
};

const ColorEditor: React.FC<ColorEditorProps> = ({ value, onChange, placeholder }) => (
  <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
    <input
      type="color"
      value={normalizeColorValue(value)}
      onChange={event => onChange?.(event.target.value)}
      style={{
        width: 36,
        height: 32,
        padding: 0,
        border: "1px solid #d9d9d9",
        borderRadius: 6,
        background: "transparent",
        cursor: "pointer"
      }}
    />
    <Input value={value} placeholder={placeholder || "#f9fafb"} onChange={event => onChange?.(event.target.value)} />
  </div>
);

const JsonEditor: React.FC<JsonEditorProps> = ({ value, onChange, placeholder }) => {
  const [text, setText] = useState("");

  useEffect(() => {
    setText(JSON.stringify(value ?? {}, null, 2));
  }, [value]);

  const handleBlur = () => {
    try {
      onChange?.(text.trim() ? JSON.parse(text) : {});
    } catch {
      message.error("JSON 格式错误，请修正后再保存");
    }
  };

  return (
    <Input.TextArea
      rows={7}
      value={text}
      placeholder={placeholder}
      onChange={event => setText(event.target.value)}
      onBlur={handleBlur}
      style={{ fontFamily: "Consolas, 'Cascadia Code', monospace", fontSize: 12 }}
    />
  );
};

const fields: Record<string, React.ComponentType<any>> = {
  Text: Input,
  Number: InputNumber,
  Select: Select,
  Radio: Radio.Group,
  TextArea: Input.TextArea,
  OptionEditor: OptionEditor,
  Json: JsonEditor,
  Color: ColorEditor
};

export default fields;
