import React, { useCallback, useMemo } from "react";
import { DatePicker as AntdDatePicker, Form } from "antd";
import dayjs from "dayjs";
import type { Dayjs } from "dayjs";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType } from "@/typings";

/**
 * DatePicker组件属性接口定义
 */
interface DatePickerFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  /** 值变更回调函数 */
  onChange?: (date: Dayjs | null, dateString: string | string[]) => void;
}

/**
 * 日期选择器组件
 * 功能：封装Antd DatePicker组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持多种日期格式
 * 2. 支持必填验证
 * 3. 支持禁用状态和清除功能
 * 4. 自动处理字段标题和提示信息
 * 5. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const DatePickerField: React.FC<DatePickerFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange }) => {
  const { DataIndex, Placeholder, Required, Disabled, ModifyDisabled, DataFormate, AllowClear, FormTitle } = field;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  // 确定是否允许清除选择的日期
  const isAllowClear = useMemo(() => AllowClear === true, [AllowClear]);

  // 处理日期变化
  const handleChange = useCallback(
    (date: Dayjs | null, dateString: string | string[]) => {
      onChange?.(date, dateString);
    },
    [onChange]
  );

  // 自定义验证器，确保传入的值是有效的dayjs对象或空值
  const customValidator = useCallback(
    (_: any, value: any) => {
      // 如果是必填字段但没有值
      if (Required && !value) {
        return Promise.reject(new Error(`请选择${FormTitle}!`));
      }

      // 如果有值，检查是否为有效的dayjs对象
      if (value) {
        // 如果不是dayjs对象，尝试转换
        if (!dayjs.isDayjs(value)) {
          try {
            const converted = dayjs(value);
            if (!converted.isValid()) {
              return Promise.reject(new Error(`日期格式不正确!`));
            }
          } catch (error) {
            return Promise.reject(new Error(`日期格式不正确!`));
          }
        } else if (!value.isValid()) {
          return Promise.reject(new Error(`日期格式不正确!`));
        }
      }

      return Promise.resolve();
    },
    [Required, FormTitle]
  );

  // 验证规则 - 使用自定义验证器避免内部验证冲突
  const validationRules = useMemo(
    () => [
      {
        validator: customValidator
      }
    ],
    [customValidator]
  );

  // 日期格式，默认为 YYYY-MM-DD
  const dateFormat = useMemo(() => DataFormate || "YYYY-MM-DD", [DataFormate]);

  return (
    <Form.Item
      name={DataIndex}
      label={<FieldTitle {...field} />}
      rules={validationRules}
      // 移除initialValue，让Form组件通过initialValues统一管理
      // 使用 getValueFromEvent 确保表单存储值为 dayjs|null
      getValueFromEvent={(date: Dayjs | null) => date}
      // 使用 getValueProps 将任意传入值映射为 dayjs 或 undefined，避免 isValid 报错
      getValueProps={(value: any) => {
        if (value == null) return { value };
        if (dayjs.isDayjs(value)) return { value };
        // 兼容字符串、时间戳、Date
        const converted = value instanceof Date ? dayjs(value) : dayjs(value);
        return { value: converted.isValid() ? converted : undefined };
      }}
    >
      <AntdDatePicker
        disabled={isDisabled}
        format={dateFormat}
        placeholder={Placeholder ?? "请选择日期"}
        allowClear={isAllowClear}
        onChange={handleChange}
        style={{ width: "100%" }}
      />
    </Form.Item>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(DatePickerField);
