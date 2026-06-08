import React from "react";
import { Tooltip } from "antd";
import { Icon } from "@/components";
import { RootState, useSelector } from "@/redux";

/**
 * 字段标题组件属性接口
 */
interface FieldTitleProps {
  /** 表单标题 */
  FormTitle?: string;
  /** 是否显示提示信息 */
  IsTooltip?: boolean;
  /** 提示信息内容 */
  TooltipContent?: string;
  /** 额外的类名 */
  className?: string;
  /** 图标名称 */
  name?: string;
  /** 表单标题 */
  FormTitle_EN?: string;
  /** 提示内容 */
  TooltipContent_EN?: string;
}

/**
 * 字段标题组件
 * 功能：为表单字段提供统一的标题显示和提示信息
 * 特性：
 * 1. 支持显示字段标题
 * 2. 支持可选的提示信息图标
 * 3. 提示信息通过 Tooltip 组件展示
 * 4. 使用 React.memo 优化性能
 *
 * @param props - 组件属性
 * @returns React组件
 */
const FieldTitle: React.FC<FieldTitleProps> = ({ FormTitle, FormTitle_EN, IsTooltip, TooltipContent, TooltipContent_EN, className, name }) => {
  const language = useSelector((state: RootState) => state.global.language);
  return (
    <>
      {language === "en" ? FormTitle_EN || FormTitle : FormTitle}
      {IsTooltip && TooltipContent && (
        <Tooltip title={language === "en" ? TooltipContent_EN || TooltipContent : TooltipContent} placement="top">
          <span>
            <Icon name={name || "InfoCircleOutlined"} className={className || "ml-5"} />
          </span>
        </Tooltip>
      )}
    </>
  );
};

// 使用React.memo优化性能，避免不必要的重渲染
export default React.memo(FieldTitle);
