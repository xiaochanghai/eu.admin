import { useCallback } from "react";
// import { CloseOutlined } from "@ant-design/icons";
// import { Button, Tooltip } from "antd";
import { styled } from "styled-components";
// import { useEditorEngine } from "../../hooks";
import { useTranslate } from "../../react-locales";
import { IRouteNode, IBranchNode } from "../../interfaces";
import { Tooltip } from "antd";
import { Icon } from "@/components/Icon";
import { useWorkFlow } from "@/utils/workflow";

const Container = styled.div`
  position: absolute;
  right: -4px;
  top: -4px;
  display: flex;
  opacity: 0.7;
  font-size: 11px;
`;

export const ConditionButtons = (props: { parent: IRouteNode; node: IBranchNode }) => {
  const { parent, node } = props;
  // const store = useEditorEngine();
  const t = useTranslate();
  const workFlow = useWorkFlow();

  const handleClose = useCallback(() => {
    node.id && workFlow.removeCondition(parent, node.id);
  }, [node.id, parent, workFlow]);

  const handleClone = useCallback(() => {
    workFlow.cloneCondition(parent, node);
  }, [node, parent, workFlow]);

  return (
    <Container className="mini-bar space-x-2">
      <Tooltip placement="topRight" title={t("copyCodition")}>
        {/* <i className="text-base  hover:text-blue-500 icon-task-copy" onClick={handleClone} /> */}
        <i className="icon" onClick={handleClone}>
          <Icon name="CopyOutlined" />
        </i>
      </Tooltip>
      {/* <i className="text-base hover:text-blue-500 icon-delete" onClick={handleClose}> */}
      <i className="icon" onClick={handleClose}>
        <Icon name="CloseOutlined" />
      </i>
    </Container>
  );
};
