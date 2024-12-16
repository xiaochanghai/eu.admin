import { Tooltip } from "antd";
import { Icon } from "@/components/Icon";

const FieldTitle: React.FC<any> = props => {
  const { FormTitle, IsTooltip, TooltipContent } = props;
  return (
    <>
      {FormTitle}
      {IsTooltip ? (
        <Tooltip title={TooltipContent}>
          <span>
            <Icon name="InfoCircleOutlined" className="ml-5" />
          </span>
        </Tooltip>
      ) : null}
    </>
  );
};

export default FieldTitle;
