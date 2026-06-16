import React, { useCallback, useMemo } from "react";
import { Form } from "antd";
import FieldTitle from "./FieldTitle";
import FileUpload from "@/components/FileUpload";
import { FieldProps, ModifyType } from "@/typings";

const FormItem = Form.Item;

/**
 * 封面图组件属性接口定义
 */
interface ImageCoverFieldProps {
  /** 字段配置 */
  field: FieldProps;
  /** 是否禁用 */
  disabled?: boolean;
  /** 修改类型（新增/编辑/查看） */
  modifyType?: ModifyType;
  /** 值变更回调函数 */
  onChange?: (fileId: string) => void;
}

/**
 * 封面图表单字段组件
 * 功能：封装ImageCover组件，提供统一的表单字段样式和验证规则
 * 特性：
 * 1. 支持必填验证
 * 2. 支持禁用状态
 * 3. 自动处理字段标题和提示信息
 * 4. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const FileUploadField: React.FC<ImageCoverFieldProps> = ({ field, disabled, modifyType = ModifyType.Edit, onChange }) => {
  const { DefaultValue, DataIndex, Required, Disabled, ModifyDisabled, FormTitle, LabelCol, WrapperCol } = field;

  // 根据修改类型和字段属性设置禁用状态
  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  /**
   * 处理值变更事件
   * @param fileId - 上传后的文件ID
   */
  const handleChange = useCallback(
    (fileId: string) => {
      onChange?.(fileId);
    },
    [onChange]
  );

  // 验证规则
  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `请上传${FormTitle}!`
      }
    ],
    [Required, FormTitle]
  );
  // 标签列配置
  const labelColConfig = useMemo(() => {
    return LabelCol
      ? {
        xs: { span: LabelCol },
        sm: { span: LabelCol },
        md: { span: LabelCol }
      }
      : undefined;
  }, [LabelCol]);

  // 包装列配置
  const wrapperColConfig = useMemo(() => {
    return WrapperCol
      ? {
        xs: { span: WrapperCol },
        sm: { span: WrapperCol },
        md: { span: WrapperCol }
      }
      : undefined;
  }, [WrapperCol]);
  return (
    <FormItem
      name={DataIndex}
      label={<FieldTitle {...field} />}
      labelCol={labelColConfig}
      wrapperCol={wrapperColConfig}
      rules={validationRules}
      initialValue={DefaultValue ?? undefined}
    >
      <FileUpload
        filePath="upload"
        accept=".zip"
        maxFileSize={5}
        disabled={isDisabled}
        onChange={handleChange}
      />
    </FormItem>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(FileUploadField);
