import React from "react";
import * as Icons from "@ant-design/icons";

interface IconProps {
  name: string;
  className?: string;
}

export const Icon: React.FC<IconProps> = React.memo(({ name, className }) => {
  const customIcons: { [key: string]: any } = Icons;

  if (!name) return;
  if (customIcons[name]) return React.createElement(customIcons[name], { className });
  return <i className={"iconfont ant-menu-item-icon icon-" + name}></i>;
});
