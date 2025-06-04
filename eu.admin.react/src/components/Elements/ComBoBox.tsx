import React, { useCallback } from "react";
import { Form } from "antd";
import { ComBoBox } from "@/components";
import FieldTitle from "./FieldTitle";
import { SmLovData } from "@/api/interface";
import { FieldProps } from "@/typings";

const FormItem = Form.Item;

/**
 * ComboBox组件属性接口定义
 */
interface ComboBoxFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 值变更回调函数 */
  onChange?: (value: string, option: any, record?: SmLovData[] | null) => void;
}

/**
 * 下拉选择框组件
 * @param props - 组件属性
 * @returns React组件
 */
const ComboBoxField: React.FC<ComboBoxFieldProps> = props => {
  const { field, disabled, onChange } = props;
  const { DefaultValue, DataIndex, Placeholder, Required, DataSource, Disabled, FormTitle } = field;

  // 合并组件级和字段级的禁用状态
  const isDisabled = disabled || Disabled;

  /**
   * 处理值变更事件
   * @param value - 选中的值
   * @param option - 选中的选项
   * @param record - 选中的记录数据
   */
  const handleChange = useCallback(
    (value: string, option: any, record?: SmLovData[] | null) => {
      onChange?.(value, option, record);
    },
    [onChange]
  );

  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle name="InfoCircleOutlined" className="ml-5" {...field} />}
      rules={[{ required: Required ?? false, message: `请输入${FormTitle}!` }]}
    >
      <ComBoBox
        id={DataSource ?? DataIndex} // 如果没有指定DataSource，则使用DataIndex作为数据源ID
        placeholder={Placeholder}
        defaultValue={DefaultValue}
        disabled={isDisabled}
        onChange={handleChange}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(ComboBoxField);
