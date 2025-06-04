import React, { useCallback } from "react";
import { ColorPicker as AntdColorPicker, Form } from "antd";
import FieldTitle from "./FieldTitle";
import { FieldProps } from "@/typings";

const FormItem = Form.Item;

/**
 * ColorPicker组件属性接口定义
 */
interface ColorPickerProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
}

/**
 * 颜色选择器组件
 * @param props - 组件属性
 * @returns React组件
 */
const ColorPickerField: React.FC<ColorPickerProps> = props => {
  const { field, disabled } = props;
  const { DefaultValue, DataIndex, Required, Disabled, FormTitle } = field;

  // 使用useEyeDropper钩子获取吸管功能（可选扩展）
  // const { isEnabled, openEyeDropper } = useEyeDropper();

  /**
   * 格式化颜色值
   * @param value - 颜色值
   * @returns 格式化后的十六进制颜色字符串
   */
  const normalizeColor = useCallback((value: any) => {
    return value && `${value.toHexString()}`;
  }, []);

  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
      initialValue={DefaultValue ?? null}
      normalize={normalizeColor}
    >
      <AntdColorPicker
        disabled={disabled ?? Disabled}
        defaultFormat="hex"
        showText
        // 可以添加吸管功能（可选扩展）
        // onOpenChange={(open) => {
        //   if (open && isEnabled) {
        //     openEyeDropper();
        //   }
        // }}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(ColorPickerField);
