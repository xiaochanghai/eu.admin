import { useCallback } from "react";
import { styled } from "styled-components";
import { Button } from "antd";
import { Icon } from "@/components";
import { useWorkFlow } from "@/workflow-editor/hooks";

const CloseStyledButton = styled(Button)`
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 14px;
  display: flex;
  justify-content: center;
  align-items: center;
`;
export const CloseButton = (props: { nodeId?: string }) => {
  const { nodeId } = props;
  const workFlow = useWorkFlow();

  const handleClose = useCallback(() => {
    workFlow.removeNode(nodeId);
  }, [nodeId, workFlow]);

  return (
    <CloseStyledButton
      className="close"
      // type="text"
      size="small"
      // shape="circle"
      icon={<Icon name="CloseOutlined" />}
      onClick={handleClose}
    />
  );
};
