import React from "react";
import { CSSProperties } from "react";

interface SvgProps {
  name: string;
  color?: string;
  prefix?: string;
  iconStyle?: CSSProperties;
}

const SvgIcon = (props: SvgProps) => {
  const { name, prefix = "icon", iconStyle = { width: "100px", height: "100px" } } = props;

  const symbolId = `#${prefix}-${name}`;

  return (
    <svg aria-hidden="true" style={iconStyle}>
      <use href={symbolId} />
    </svg>
  );
};

export default React.memo(SvgIcon);
