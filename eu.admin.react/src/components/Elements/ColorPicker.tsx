import React, { useCallback, useState, useMemo } from "react";
import { ColorPicker as AntdColorPicker, Form, Button, Space, message } from "antd";
import { EyeOutlined } from "@ant-design/icons";
import FieldTitle from "./FieldTitle";
import { FieldProps } from "@/typings";
import useEyeDropper from "@/hooks/useEyeDropper";
import type { Color, ColorPickerProps as AntdColorPickerProps } from "antd/es/color-picker";

const FormItem = Form.Item;

/**
 * ColorPicker组件属性接口定义
 */
interface ColorPickerProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 颜色变化回调 */
  onChange?: (color: string) => void;
  /** 自定义预设颜色 */
  presets?: Array<{ label: string; colors: string[] }>;
  /** 支持的颜色格式 */
  format?: AntdColorPickerProps["format"];
}

/**
 * 内部颜色选择器组件，用于与 Form.Item 集成
 */
interface InternalColorPickerProps {
  value?: string;
  onChange?: (value: string) => void;
  disabled?: boolean;
  format?: AntdColorPickerProps["format"];
  presets?: Array<{ label: string; colors: string[] }>;
  onExternalChange?: (color: string) => void;
}

/**
 * 默认预设颜色
 */
const DEFAULT_PRESETS = [
  {
    label: "推荐",
    colors: ["#F5222D", "#FA8C16", "#FADB14", "#8BBB11", "#52C41A", "#13A8A8", "#1677FF", "#2F54EB", "#722ED1", "#EB2F96"]
  },
  {
    label: "最近使用",
    colors: []
  }
];

/**
 * 内部颜色选择器组件
 */
const InternalColorPicker: React.FC<InternalColorPickerProps> = ({
  value,
  onChange,
  disabled,
  format = "hex",
  presets = DEFAULT_PRESETS,
  onExternalChange
}) => {
  const [recentColors, setRecentColors] = useState<string[]>([]);
  const { color: eyeDropperColor, isEnabled, openEyeDropper } = useEyeDropper();

  /**
   * 处理颜色变化
   * @param color - 新的颜色值
   */
  const handleColorChange = useCallback(
    (color: Color) => {
      try {
        let formattedColor: string;

        // 根据格式转换颜色
        switch (format) {
          case "rgb":
            formattedColor = color.toRgbString();
            break;
          case "hsb":
            formattedColor = color.toHsbString();
            break;
          case "hex":
          default:
            formattedColor = color.toHexString();
            break;
        }

        // 更新最近使用的颜色
        setRecentColors(prev => {
          const newColors = [formattedColor, ...prev.filter(c => c !== formattedColor)];
          return newColors.slice(0, 10); // 最多保存10个最近使用的颜色
        });

        // 触发表单变化 - 这是关键！
        onChange?.(formattedColor);
        // 触发外部回调
        onExternalChange?.(formattedColor);
      } catch (error) {
        console.warn("颜色处理失败:", error);
      }
    },
    [format, onChange, onExternalChange]
  );

  /**
   * 处理清除颜色
   */
  const handleClear = useCallback(() => {
    onChange?.(undefined as any);
    onExternalChange?.("");
  }, [onChange, onExternalChange]);

  /**
   * 处理吸管工具
   */
  const handleEyeDropper = useCallback(async () => {
    if (!isEnabled) {
      message.warning("当前浏览器不支持吸管工具");
      return;
    }

    try {
      await openEyeDropper();
    } catch (error) {
      console.warn("吸管工具使用失败:", error);
      message.error("吸管工具使用失败");
    }
  }, [isEnabled, openEyeDropper]);

  /**
   * 合并预设颜色和最近使用的颜色
   */
  const mergedPresets = useMemo(() => {
    const updatedPresets = [...presets];
    const recentIndex = updatedPresets.findIndex(preset => preset.label === "最近使用");

    if (recentIndex !== -1 && recentColors.length > 0) {
      updatedPresets[recentIndex] = {
        ...updatedPresets[recentIndex],
        colors: recentColors
      };
    }

    return updatedPresets;
  }, [presets, recentColors]);

  // 当吸管工具获取到颜色时，自动填充
  React.useEffect(() => {
    if (eyeDropperColor) {
      // 直接更新表单值
      onChange?.(eyeDropperColor);
      // 更新最近使用的颜色
      setRecentColors(prev => {
        const newColors = [eyeDropperColor, ...prev.filter(c => c !== eyeDropperColor)];
        return newColors.slice(0, 10);
      });
      onExternalChange?.(eyeDropperColor);
    }
  }, [eyeDropperColor, onChange, onExternalChange]);

  return (
    <Space.Compact style={{ width: "100%" }}>
      <AntdColorPicker
        value={value}
        disabled={disabled}
        format={format}
        showText={color => <span style={{ marginLeft: 8 }}>{color.toHexString()}</span>}
        presets={mergedPresets}
        onChange={handleColorChange}
        onClear={handleClear}
        placement="bottomLeft"
        allowClear
        size="middle"
        style={{ width: "100%" }}
      />
      {isEnabled && (
        <Button
          icon={<EyeOutlined />}
          onClick={handleEyeDropper}
          disabled={disabled}
          title="使用吸管工具选择颜色"
          type="default"
        />
      )}
    </Space.Compact>
  );
};

/**
 * 颜色选择器组件
 * @param props - 组件属性
 * @returns React组件
 */
const ColorPickerField: React.FC<ColorPickerProps> = props => {
  const { field, disabled, onChange, presets = DEFAULT_PRESETS, format = "hex" } = props;

  const { DefaultValue, DataIndex, Required, Disabled, FormTitle } = field;

  /**
   * 验证颜色值是否有效
   * @param colorValue - 颜色值
   * @returns 是否有效
   */
  const isValidColor = useCallback((colorValue: string): boolean => {
    if (!colorValue) return false;

    // 验证十六进制颜色
    const hexRegex = /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/;
    if (hexRegex.test(colorValue)) return true;

    // 验证RGB颜色
    const rgbRegex = /^rgb\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)$/;
    if (rgbRegex.test(colorValue)) return true;

    // 验证RGBA颜色
    const rgbaRegex = /^rgba\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(0|1|0?\.\d+)\s*\)$/;
    if (rgbaRegex.test(colorValue)) return true;

    return false;
  }, []);

  /**
   * 表单验证规则
   */
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `请选择${FormTitle}!`
      },
      {
        validator: (_: any, value: string) => {
          if (!value || isValidColor(value)) {
            return Promise.resolve();
          }
          return Promise.reject(new Error("请输入有效的颜色值"));
        }
      }
    ],
    [Required, FormTitle, isValidColor]
  );

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={DefaultValue || undefined}>
      <InternalColorPicker disabled={disabled ?? Disabled} format={format} presets={presets} onExternalChange={onChange} />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(ColorPickerField, (prevProps, nextProps) => {
  return (
    prevProps.field === nextProps.field &&
    prevProps.disabled === nextProps.disabled &&
    prevProps.onChange === nextProps.onChange &&
    JSON.stringify(prevProps.presets) === JSON.stringify(nextProps.presets) &&
    prevProps.format === nextProps.format
  );
});
