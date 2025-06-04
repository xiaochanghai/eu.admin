import React, { useCallback } from "react";
import { Form } from "antd";
import { ComboGrid } from "@/components";
import { ModifyType, SmLovData } from "@/api/interface/index";
import FieldTitle from "./FieldTitle";
import { FieldProps } from "@/typings";

const FormItem = Form.Item;

/**
 * ComboGrid组件属性接口定义
 */
interface ComboGridFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 值变更回调函数 */
  onChange?: (value: string, option: any, record?: SmLovData[] | null) => void;
  /** 父级列名，用于联动查询 */
  parentColumn?: string;
  /** 父级ID，用于联动查询 */
  parentId?: string | number;
  /** 修改类型 */
  modifyType?: ModifyType;
}

/**
 * 下拉表格选择框组件
 * @param props - 组件属性
 * @returns React组件
 */
const ComboGridField: React.FC<ComboGridFieldProps> = props => {
  const { field, disabled, onChange, parentColumn, parentId, modifyType } = props;
  const { DataIndex, Placeholder, Required, DataSource, Disabled, ModifyDisabled, FormTitle } = field;

  // 根据条件确定是否禁用组件
  // 1. 如果是编辑模式且设置了ModifyDisabled，则禁用
  // 2. 如果设置了Disabled，则禁用
  // 3. 如果外部传入disabled，则禁用
  const isDisabled = (modifyType === ModifyType.Edit && ModifyDisabled) || Disabled || disabled;

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
      <ComboGrid
        code={DataSource} // 数据源代码，用于获取下拉选项
        disabled={isDisabled}
        onChange={handleChange}
        parentColumn={parentColumn} // 父级列名，用于联动查询
        parentId={parentId} // 父级ID，用于联动查询
        placeholder={Placeholder ?? ""}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(ComboGridField);
