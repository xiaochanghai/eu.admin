import React from "react";
import { Button, Input } from "antd";
import { PlusOutlined, DeleteOutlined } from "@ant-design/icons";

interface OptionItem {
  label: string;
  value: string;
  [key: string]: any;
}

interface Props {
  value?: OptionItem[];
  onChange?: (value: OptionItem[]) => void;
}

const OptionEditor: React.FC<Props> = ({ value = [], onChange }) => {
  const handleAdd = () => {
    onChange?.([...value, { label: "", value: "" }]);
  };

  const handleRemove = (index: number) => {
    const newList = value.filter((_, i) => i !== index);
    onChange?.(newList);
  };

  const handleChange = (index: number, field: keyof OptionItem, val: string) => {
    const newList = [...value];
    newList[index] = { ...newList[index], [field]: val };
    onChange?.(newList);
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      {value.map((item, index) => (
        <div key={index} style={{
          display: "flex",
          gap: 6,
          alignItems: "center",
          padding: "6px 8px",
          background: "#f8fafc",
          borderRadius: 6,
          border: "1px solid #f0f0f0"
        }}>
          <Input
            size="small"
            placeholder="标签"
            value={item.label}
            onChange={e => handleChange(index, "label", e.target.value)}
            style={{ flex: 1, borderRadius: 4 }}
          />
          <Input
            size="small"
            placeholder="值"
            value={item.value}
            onChange={e => handleChange(index, "value", e.target.value)}
            style={{ flex: 1, borderRadius: 4 }}
          />
          <Button
            size="small"
            type="text"
            danger
            icon={<DeleteOutlined style={{ fontSize: 12 }} />}
            onClick={() => handleRemove(index)}
            style={{ flexShrink: 0 }}
          />
        </div>
      ))}
      <Button
        size="small"
        type="dashed"
        block
        icon={<PlusOutlined />}
        onClick={handleAdd}
        style={{ borderRadius: 6, color: "#6b7280", borderColor: "#d1d5db" }}
      >
        添加选项
      </Button>
    </div>
  );
};

export default OptionEditor;
