import React, { useCallback, useMemo } from "react";
import { DatePicker as AntdDatePicker, Form, TimePicker as AntdTimePicker } from "antd";
import dayjs from "dayjs";
import type { Dayjs } from "dayjs";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";

/**
 * DatePicker组件属性接口定义
 */
interface DatePickerFieldProps {
  field: FieldProps;
  disabled?: boolean;
  modifyType?: ModifyType;
  onChange?: (date: Dayjs | null, dateString: string | string[]) => void;
}

/**
 * 共享的 hooks：计算禁用状态
 */
const useDisabledState = (
  modifyType: ModifyType,
  ModifyDisabled: boolean | undefined,
  Disabled: boolean | undefined,
  disabled: boolean | undefined
) => {
  return useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);
};

/**
 * 共享的 hooks：计算是否允许清除
 */
const useAllowClear = (AllowClear: boolean | undefined) => {
  return useMemo(() => AllowClear === true, [AllowClear]);
};

/**
 * 共享的 hooks：创建验证器
 */
const useValidator = (Required: boolean | undefined, FormTitle: string | undefined, errorType: "日期" | "时间") => {
  return useCallback(
    (_: any, value: any) => {
      if (Required && !value) {
        return Promise.reject(new Error(`请选择${FormTitle}!`));
      }

      if (value) {
        if (!dayjs.isDayjs(value)) {
          try {
            const converted = dayjs(value);
            if (!converted.isValid()) {
              return Promise.reject(new Error(`${errorType}格式不正确!`));
            }
          } catch (error) {
            return Promise.reject(new Error(`${errorType}格式不正确!`));
          }
        } else if (!value.isValid()) {
          return Promise.reject(new Error(`${errorType}格式不正确!`));
        }
      }

      return Promise.resolve();
    },
    [Required, FormTitle, errorType]
  );
};

/**
 * 共享的 hooks：创建变化处理函数
 */
const useChangeHandler = (onChange?: (date: Dayjs | null, dateString: string | string[]) => void) => {
  return useCallback(
    (date: Dayjs | null, dateString: string | string[]) => {
      onChange?.(date, dateString);
    },
    [onChange]
  );
};

/**
 * 共享的 hooks：创建验证规则
 */
const useValidationRules = (validator: (_: any, value: any) => Promise<void>) => {
  return useMemo(() => [{ validator }], [validator]);
};

/**
 * 共享的值转换函数
 */
const getValueProps = (value: any) => {
  if (value == null) return { value };
  if (dayjs.isDayjs(value)) return { value };
  const converted = value instanceof Date ? dayjs(value) : dayjs(value);
  return { value: converted.isValid() ? converted : undefined };
};

/**
 * 共享的事件处理函数
 */
const getValueFromEvent = (date: Dayjs | null) => date;

/**
 * 通用基础组件配置类型
 */
interface BasePickerConfig {
  pickerType: "date" | "time";
  defaultFormat: string;
  defaultPlaceholder: string;
  showTime?: boolean;
}

/**
 * 通用基础 Picker 组件
 */
const BasePickerField: React.FC<DatePickerFieldProps & BasePickerConfig> = ({
  field,
  disabled,
  modifyType = ModifyType.Edit,
  onChange,
  pickerType,
  defaultFormat,
  defaultPlaceholder,
  showTime
}) => {
  const { DataIndex, Placeholder, Required, Disabled, ModifyDisabled, AllowClear, FormTitle } = field;

  const isDisabled = useDisabledState(modifyType, ModifyDisabled, Disabled, disabled);
  const isAllowClear = useAllowClear(AllowClear);
  const customValidator = useValidator(Required, FormTitle, pickerType === "date" ? "日期" : "时间");
  const handleChange = useChangeHandler(onChange);
  const validationRules = useValidationRules(customValidator);
  const format = useMemo(() => field.DataFormate ?? defaultFormat, [field.DataFormate, defaultFormat]);

  const PickerComponent = pickerType === "date" ? AntdDatePicker : AntdTimePicker;

  return (
    <Form.Item
      name={DataIndex}
      label={<FieldTitle {...field} />}
      rules={validationRules}
      getValueFromEvent={getValueFromEvent}
      getValueProps={getValueProps}
    >
      <PickerComponent
        disabled={isDisabled}
        format={format}
        placeholder={Placeholder ?? defaultPlaceholder}
        allowClear={isAllowClear}
        onChange={handleChange}
        showTime={showTime}
        style={{ width: "100%" }}
      />
    </Form.Item>
  );
};

/**
 * Date picker field.
 */
export const DatePickerField: React.FC<DatePickerFieldProps> = props => {
  return (
    <BasePickerField {...props} pickerType="date" defaultFormat="YYYY-MM-DD" defaultPlaceholder="请选择日期" showTime={false} />
  );
};

/**
 * Date time picker field.
 */
export const DateTimePickerField: React.FC<DatePickerFieldProps> = props => {
  return (
    <BasePickerField
      {...props}
      pickerType="date"
      defaultFormat="YYYY-MM-DD HH:mm:ss"
      defaultPlaceholder="请选择日期时间"
      showTime={true}
    />
  );
};

/**
 * Time picker field.
 */
export const TimePickerField: React.FC<DatePickerFieldProps> = props => {
  return <BasePickerField {...props} pickerType="time" defaultFormat="HH:mm:ss" defaultPlaceholder="请选择时间" />;
};
