import React from "react";
import * as Icons from "@ant-design/icons";

interface IconProps {
  name: string;
  className?: string;
  style?: any;
}

export const Icon: React.FC<IconProps> = React.memo(({ name, className, style }) => {
  const customIcons: { [key: string]: any } = Icons;

  if (!name) return;
  if (customIcons[name]) return React.createElement(customIcons[name], { className, style });
  return <i className={"iconfont ant-menu-item-icon icon-" + name}></i>;
});
