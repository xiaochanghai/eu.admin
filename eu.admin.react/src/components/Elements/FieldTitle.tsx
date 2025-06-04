import React from "react";
import { Tooltip } from "antd";
import { Icon } from "@/components";

const FieldTitle: React.FC<any> = props => {
  const { FormTitle, IsTooltip, TooltipContent } = props;
  return (
    <>
      {FormTitle}
      {IsTooltip && (
        <Tooltip title={TooltipContent}>
          <span>
            <Icon name="InfoCircleOutlined" className="ml-5" />
          </span>
        </Tooltip>
      )}
    </>
  );
};

export default React.memo(FieldTitle);
