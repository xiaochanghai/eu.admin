import React from "react";
import { Button, Tooltip } from "antd";
import { Icon } from "@/components";
import { ActionButtonProps } from "../types";
import { DEFAULT_ACTION_BUTTON_STYLE } from "../constants";

/**
 * 操作按钮组件
 * 用于表格操作列中的操作按钮
 */
export const ActionButton: React.FC<ActionButtonProps> = React.memo(({ icon, onClick, disabled = false, tooltip }) => {
  const button = (
    <Button
      type="dashed"
      size="small"
      icon={<Icon name={icon} />}
      onClick={onClick}
      disabled={disabled}
      style={DEFAULT_ACTION_BUTTON_STYLE}
    />
  );

  return tooltip ? <Tooltip title={tooltip}>{button}</Tooltip> : button;
});

ActionButton.displayName = "ActionButton";
