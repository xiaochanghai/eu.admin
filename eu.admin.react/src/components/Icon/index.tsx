import React from "react";
import * as Icons from "@ant-design/icons";

interface IconProps {
  name: string;
  className?: string;
  style?: React.CSSProperties;
}

// 将图标映射提取到组件外部，避免每次渲染都创建新对象
const customIcons: Record<string, any> = Icons;

export const Icon: React.FC<IconProps> = React.memo(({ name, className, style }) => {
  // 如果没有提供 name，返回 null 而不是 undefined
  if (!name) return null;

  // 如果是 Ant Design 图标，使用 createElement 创建
  if (customIcons[name]) return React.createElement(customIcons[name], { className, style });

  // 否则使用 iconfont 自定义图标
  return <i className={`iconfont ant-menu-item-icon icon-${name}`} style={style}></i>;
});

// 添加 displayName 便于调试
Icon.displayName = "Icon";
