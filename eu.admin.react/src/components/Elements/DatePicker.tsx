import React, { useCallback, useMemo } from "react";
import { DatePicker as AntdDatePicker, Form, TimePicker as AntdTimePicker } from "antd";
import dayjs from "dayjs";
import type { Dayjs } from "dayjs";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";
import { RootState, useSelector } from "@/redux";
import { useTranslation } from "react-i18next";

/**
 * DatePicker组件属性接口定义
 */
interface DatePickerFieldProps {
  field: FieldProps;
  disabled?: boolean;
  modifyType?: ModifyType;
  onChange?: (date: Dayjs | null, dateString: string | null) => void;
}

type RangeValue = [Dayjs | null, Dayjs | null] | null;

interface RangePickerFieldProps extends Omit<DatePickerFieldProps, "onChange"> {
  onChange?: (dates: RangeValue, dateStrings: [string, string]) => void;
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
const useChangeHandler = (onChange?: (date: Dayjs | null, dateString: string | null) => void) => {
  return useCallback(
    (date: Dayjs | null, dateString: string | null) => {
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
 * 范围值转换：兼容接口返回的日期字符串和表单内的 Dayjs 值。
 */
const getRangeValueProps = (value: unknown) => {
  if (value == null) return { value };
  if (!Array.isArray(value) || value.length !== 2) return { value: undefined };

  const converted = value.map(item => {
    if (item == null || dayjs.isDayjs(item)) return item;
    const date = dayjs(item);
    return date.isValid() ? date : null;
  }) as [Dayjs | null, Dayjs | null];

  return { value: converted.every(Boolean) ? converted : undefined };
};

const getRangeValueFromEvent = (dates: RangeValue) => dates;

/**
 * 通用基础组件配置类型
 */
interface BasePickerConfig {
  pickerType: "date" | "time";
  defaultFormat: string;
  defaultPlaceholder: string;
  showTime?: boolean;
  rangePickerMode?: "date" | "week" | "month" | "quarter";
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
  const { DataIndex, Placeholder, Placeholder_EN, Required, Disabled, ModifyDisabled, AllowClear, FormTitle } = field;
  const language = useSelector((state: RootState) => state.global.language);
  const placeholder = (language === "en" ? Placeholder_EN : Placeholder) || defaultPlaceholder;

  const isDisabled = useDisabledState(modifyType, ModifyDisabled, Disabled, disabled);
  const isAllowClear = useAllowClear(AllowClear);
  const formTitle = language === "en" ? field.FormTitle_EN || FormTitle : FormTitle;
  const customValidator = useValidator(Required, formTitle, pickerType === "date" ? "日期" : "时间");
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
        placeholder={placeholder}
        allowClear={isAllowClear}
        onChange={handleChange}
        showTime={showTime}
        style={{ width: "100%" }}
      />
    </Form.Item>
  );
};

/**
 * 通用范围 Picker 组件。
 */
const BaseRangePickerField: React.FC<RangePickerFieldProps & BasePickerConfig> = ({
  field,
  disabled,
  modifyType = ModifyType.Edit,
  onChange,
  pickerType,
  defaultFormat,
  defaultPlaceholder,
  showTime,
  rangePickerMode = "date"
}) => {
  const { DataIndex, Required, Disabled, ModifyDisabled, AllowClear, FormTitle } = field;
  const language = useSelector((state: RootState) => state.global.language);
  const { t } = useTranslation();
  const isDisabled = useDisabledState(modifyType, ModifyDisabled, Disabled, disabled);
  const isAllowClear = useAllowClear(AllowClear);
  const formTitle = language === "en" ? field.FormTitle_EN || FormTitle : FormTitle;
  const format = useMemo(() => field.DataFormate ?? defaultFormat, [field.DataFormate, defaultFormat]);
  const placeholders = useMemo<[string, string]>(() => {
    if (field.Placeholder || field.Placeholder_EN) {
      const customPlaceholder = (language === "en" ? field.Placeholder_EN : field.Placeholder) || defaultPlaceholder;
      return [customPlaceholder, customPlaceholder];
    }

    if (pickerType === "time") {
      return [t("formOption.selectStartTimePlaceholder"), t("formOption.selectEndTimePlaceholder")];
    }
    if (rangePickerMode === "week") {
      return [t("formOption.selectStartWeekPlaceholder"), t("formOption.selectEndWeekPlaceholder")];
    }
    if (rangePickerMode === "month") {
      return [t("formOption.selectStartMonthPlaceholder"), t("formOption.selectEndMonthPlaceholder")];
    }
    if (rangePickerMode === "quarter") {
      return [t("formOption.selectStartQuarterPlaceholder"), t("formOption.selectEndQuarterPlaceholder")];
    }
    return [t("formOption.selectStartDatePlaceholder"), t("formOption.selectEndDatePlaceholder")];
  }, [defaultPlaceholder, field.Placeholder, field.Placeholder_EN, language, pickerType, rangePickerMode, t]);

  const validationRules = useMemo(
    () => [
      {
        validator: (_: unknown, value: unknown) => {
          if (Required && (!Array.isArray(value) || value.length !== 2 || !value[0] || !value[1])) {
            return Promise.reject(new Error(`请选择${formTitle}!`));
          }
          return Promise.resolve();
        }
      }
    ],
    [Required, formTitle]
  );

  const handleChange = useCallback(
    (dates: RangeValue, dateStrings: [string, string]) => {
      onChange?.(dates, dateStrings);
    },
    [onChange]
  );

  const commonProps = {
    disabled: isDisabled,
    format,
    placeholder: placeholders,
    allowClear: isAllowClear,
    onChange: handleChange,
    style: { width: "100%" }
  };

  return (
    <Form.Item
      name={DataIndex}
      label={<FieldTitle {...field} />}
      rules={validationRules}
      getValueFromEvent={getRangeValueFromEvent}
      getValueProps={getRangeValueProps}
    >
      {pickerType === "date" ? (
        <AntdDatePicker.RangePicker {...commonProps} picker={rangePickerMode} showTime={showTime} />
      ) : (
        <AntdTimePicker.RangePicker {...commonProps} />
      )}
    </Form.Item>
  );
};

/**
 * Date picker field.
 */
export const DatePickerField: React.FC<DatePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BasePickerField {...props} pickerType="date" defaultFormat="YYYY-MM-DD" defaultPlaceholder={t("formOption.selectDatePlaceholder")} showTime={false} />
  );
};

/**
 * Date time picker field.
 */
export const DateTimePickerField: React.FC<DatePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BasePickerField
      {...props}
      pickerType="date"
      defaultFormat="YYYY-MM-DD HH:mm:ss"
      defaultPlaceholder={t("formOption.selectDateTimePlaceholder")}
      showTime={true}
    />
  );
};

/**
 * Time picker field.
 */
export const TimePickerField: React.FC<DatePickerFieldProps> = props => {
  const { t } = useTranslation();
  return <BasePickerField {...props} pickerType="time" defaultFormat="HH:mm:ss" defaultPlaceholder={t("formOption.selectTimePlaceholder")} />;
};

/**
 * Date range picker field.
 */
export const DateRangePickerField: React.FC<RangePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BaseRangePickerField
      {...props}
      pickerType="date"
      defaultFormat="YYYY-MM-DD"
      defaultPlaceholder={t("formOption.selectDatePlaceholder")}
      showTime={false}
    />
  );
};

/**
 * Week range picker field.
 */
export const WeekRangePickerField: React.FC<RangePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BaseRangePickerField
      {...props}
      pickerType="date"
      rangePickerMode="week"
      defaultFormat="YYYY-wo"
      defaultPlaceholder={t("formOption.selectWeekPlaceholder")}
    />
  );
};

/**
 * Month range picker field.
 */
export const MonthRangePickerField: React.FC<RangePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BaseRangePickerField
      {...props}
      pickerType="date"
      rangePickerMode="month"
      defaultFormat="YYYY-MM"
      defaultPlaceholder={t("formOption.selectMonthPlaceholder")}
    />
  );
};

/**
 * Quarter range picker field.
 */
export const QuarterRangePickerField: React.FC<RangePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BaseRangePickerField
      {...props}
      pickerType="date"
      rangePickerMode="quarter"
      defaultFormat="YYYY-[Q]Q"
      defaultPlaceholder={t("formOption.selectQuarterPlaceholder")}
    />
  );
};

/**
 * Date time range picker field.
 */
export const DateTimeRangePickerField: React.FC<RangePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BaseRangePickerField
      {...props}
      pickerType="date"
      defaultFormat="YYYY-MM-DD HH:mm:ss"
      defaultPlaceholder={t("formOption.selectDateTimePlaceholder")}
      showTime
    />
  );
};

/**
 * Time range picker field.
 */
export const TimeRangePickerField: React.FC<RangePickerFieldProps> = props => {
  const { t } = useTranslation();
  return (
    <BaseRangePickerField
      {...props}
      pickerType="time"
      defaultFormat="HH:mm:ss"
      defaultPlaceholder={t("formOption.selectTimePlaceholder")}
    />
  );
};
