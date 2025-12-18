import React, { useCallback, useMemo } from "react";
import { Form } from "antd";
import { ComboGrid } from "@/components";
import FieldTitle from "./FieldTitle";
import { FieldProps, SmLovData, ModifyType } from "@/typings";

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
 * 功能：封装ComboGrid组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持必填验证
 * 2. 支持禁用状态
 * 3. 支持父子级联动查询
 * 4. 自动处理字段标题和提示信息
 * 5. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const ComboGridField: React.FC<ComboGridFieldProps> = ({
  field,
  disabled,
  onChange,
  parentColumn,
  parentId,
  modifyType = ModifyType.Edit
}) => {
  const { DataIndex, Placeholder, Required, DataSource, Disabled, ModifyDisabled, FormTitle } = field;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  /**
   * 处理值变更事件
   * @param value - 选中的值
   * @param option - 选中的选项
   * @param record - 选中的记录数据
   */
  const handleChange = useCallback(
    (value: string | null, option: any, record?: SmLovData[] | null) => {
      onChange?.(value ?? "", option, record);
    },
    [onChange]
  );

  // 验证规则
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `请选择${FormTitle}!`
      }
    ],
    [Required, FormTitle]
  );

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules}>
      <ComboGrid
        code={DataSource} // 数据源代码，用于获取下拉选项
        disabled={isDisabled}
        onChange={handleChange}
        parentColumn={parentColumn} // 父级列名，用于联动查询
        parentId={parentId} // 父级ID，用于联动查询
        placeholder={Placeholder ?? "请选择"}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(ComboGridField);
