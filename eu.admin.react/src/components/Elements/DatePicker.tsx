import React from "react";
import { DatePicker as AntdDatePicker, Form } from "antd";
import dayjs from "dayjs";
import type { Dayjs } from "dayjs";
import FieldTitle from "./FieldTitle";
import { FieldProps } from "@/typings";

/**
 * DatePicker组件属性接口定义
 */
interface DatePickerFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 值变更回调函数 */
  onChange?: (date: Dayjs | null, dateString: string) => void;
}

/**
 * 日期选择器组件
 * @param props - 组件属性
 * @returns React组件
 */
const DatePickerField: React.FC<DatePickerFieldProps> = props => {
  const { field, disabled } = props;
  const { DefaultValue, DataIndex, Placeholder, Required, Disabled, DataFormate, AllowClear, FormTitle } = field;

  // 确定是否允许清除选择的日期
  const isAllowClear = AllowClear === true;

  return (
    <Form.Item
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
      initialValue={DefaultValue ?? null}
      getValueProps={value => ({ value: value && dayjs(value) })}
    >
      <AntdDatePicker
        disabled={disabled ?? Disabled}
        format={DataFormate}
        placeholder={Placeholder}
        allowClear={isAllowClear}
        style={{ width: "100%" }} // 确保组件宽度一致
      />
    </Form.Item>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(DatePickerField);
