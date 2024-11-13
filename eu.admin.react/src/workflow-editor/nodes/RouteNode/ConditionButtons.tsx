import { useCallback } from "react";
import { styled } from "styled-components";
import { useTranslate } from "../../react-locales";
import { IRouteNode, IBranchNode } from "../../interfaces";
import { Tooltip } from "antd";
import { Icon } from "@/components/Icon";
import { useWorkFlow } from "../../hooks";

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
  const t = useTranslate();
  const workFlow = useWorkFlow();

  const handleClose = useCallback(() => {
    node.id && workFlow.removeCondition(parent, node.id);
  }, [node.id, parent, workFlow]);

  const handleClone = useCallback(() => {
    workFlow.cloneCondition(parent, node);
  }, [node, parent, workFlow]);

  return (
    <Container className="mini-bar">
      <Tooltip placement="topRight" title={t("copyCodition")}>
        <i className="icon font-size14" onClick={handleClone}>
          <Icon name="CopyOutlined" />
        </i>
      </Tooltip>
      <i className="icon font-size14 ml-5" onClick={handleClose}>
        <Icon name="CloseOutlined" />
      </i>
    </Container>
  );
};
