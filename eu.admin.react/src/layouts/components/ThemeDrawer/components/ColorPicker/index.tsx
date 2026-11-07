import { useEffect, useState } from "react";
import { Input } from "antd";
import { HexColorPicker } from "react-colorful";
import { isHexColor } from "@/utils/is";
import { convertToSixDigitHexColor } from "@/utils";

import "./index.less";

const presetColors = [
  "#1677FF",
  "#00B96B",
  "#E0282E",
  "#DAA96E",
  "#0C819F",
  "#409EFF",
  "#FF5C93",
  "#E74C3C",
  "#27AE60",
  "#FD726D",
  "#F39C12",
  "#9B59B6"
];

interface ColorPickerProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

const ColorPicker = ({ value, onChange, disabled = false }: ColorPickerProps) => {
  const [inputPrimary, setInputPrimary] = useState(value);
  useEffect(() => setInputPrimary(value), [value]);

  const changePrimary = (value: string) => {
    if (disabled) return;
    onChange(value);
    setInputPrimary(value);
  };

  return (
    <div className="color-picker">
      <HexColorPicker
        color={value}
        onChange={e => {
          changePrimary(e.toLocaleUpperCase());
        }}
      />
      <Input
        value={inputPrimary}
        className="picker-input"
        disabled={disabled}
        addonBefore="HEX"
        onChange={e => setInputPrimary(e.target.value)}
        onBlur={e => {
          if (disabled) return;
          if (isHexColor(e.target.value)) {
            let value = e.target.value;
            if (e.target.value[0] !== "#") value = `#${e.target.value}`;
            changePrimary(convertToSixDigitHexColor(value));
          } else setInputPrimary(value);
        }}
      />
      <div className="picker-swatches">
        {presetColors.map(presetColor => (
          <button
            className="picker-swatch"
            key={presetColor}
            type="button"
            aria-label={`选择颜色 ${presetColor}`}
            disabled={disabled}
            style={{ background: presetColor }}
            onClick={() => {
              changePrimary(presetColor);
            }}
          />
        ))}
      </div>
    </div>
  );
};

export default ColorPicker;
