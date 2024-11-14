import { memo, useCallback } from "react";
import { NodeTitle } from "./NodeTitle";
import { styled } from "styled-components";
import { useMaterialUI } from "../../hooks/useMaterialUI";
import { FormVo } from "@/api/Form";
import { Drawer } from "antd";
import { RootState, useSelector } from "@/redux";
import { useWorkFlow } from "@/workflow-editor/hooks";

const Content = styled.div`
  display: flex;
  flex-flow: column;
`;
export const SettingsPanel = memo((props: { formVo?: FormVo }) => {
  const workFlow = useWorkFlow();
  const selectedId = useSelector((state: RootState) => state.workflow.selectedId);
  const selectedNode = selectedId ? workFlow.getNode(selectedId) : undefined;

  const materialUi = useMaterialUI(selectedNode);
  const handelClose = () => {
    workFlow.selectNode(undefined);
  };

  const handleNameChange = useCallback(
    (name?: string) => {
      selectedNode && workFlow.modifyNodeName(selectedNode, name ?? "");
    },
    [workFlow, selectedNode]
  );

  const handleSettingsChange = useCallback(
    (value: any) => {
      if (
        selectedNode?.nodeType === "approver" || //审核节点
        selectedNode?.nodeType === "audit" || //办理节点
        selectedNode?.nodeType === "start" || //开始节点
        selectedNode?.nodeType === "notifier" //抄送节点
      )
        workFlow.changeNode({ ...selectedNode, approverSettings: { auditList: value } });
      // dispatch(CHANGE_NODE({ ...selectedNode, approverSettings: { auditList: value } }));
      else if (selectedNode?.nodeType === "condition") workFlow.changeNode({ ...selectedNode, conditions: value }); //条件节点
    },
    [selectedNode]
  );

  return (
    <Drawer
      title={selectedNode && <NodeTitle node={selectedNode} onNameChange={handleNameChange} />}
      placement="right"
      width={656}
      closable={true}
      // footer={
      //   <Button size="small" icon={<CloseOutlined />} onClick={handelClose} />
      // }
      onClose={handelClose}
      // visible={!!selectedNode}
      open={!!selectedNode}
      // open=
    >
      {/* 不同类型的节点右侧使用不同的面板，条件类型节点使用conditions数据 */}
      <Content className="settings-panel-content">
        {materialUi?.settersPanel && (
          <materialUi.settersPanel
            value={selectedNode?.nodeType === "condition" ? selectedNode?.conditions : selectedNode?.approverSettings}
            onChange={handleSettingsChange}
            formVo={props.formVo}
          />
        )}
      </Content>
    </Drawer>
  );
});
