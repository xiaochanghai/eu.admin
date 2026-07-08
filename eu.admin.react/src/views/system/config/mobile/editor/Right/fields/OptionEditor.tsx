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
    <div className="space-y-2">
      {value.map((item, index) => (
        <div key={index} className="flex gap-1 items-center">
          <Input
            size="small"
            placeholder="标签"
            value={item.label}
            onChange={e => handleChange(index, "label", e.target.value)}
            style={{ flex: 1 }}
          />
          <Input
            size="small"
            placeholder="值"
            value={item.value}
            onChange={e => handleChange(index, "value", e.target.value)}
            style={{ flex: 1 }}
          />
          <Button
            size="small"
            type="text"
            danger
            icon={<DeleteOutlined />}
            onClick={() => handleRemove(index)}
          />
        </div>
      ))}
      <Button size="small" type="dashed" block icon={<PlusOutlined />} onClick={handleAdd}>
        添加选项
      </Button>
    </div>
  );
};

export default OptionEditor;
