import { memo, useCallback } from "react";
import { MiniFloatContainer } from "../ZoomBar";
import { undoIcon, redoIcon } from "../../icons";
import { useRedoList } from "../../hooks/useRedoList";
import { useUndoList } from "../../hooks/useUndoList";
import { Button, Space } from "antd";
import { useWorkFlow } from "@/workflow-editor/hooks";

export const OperationBar = memo((props: { float?: boolean }) => {
  const { float } = props;
  const redoList = useRedoList();
  const undoList = useUndoList();
  const workFlow = useWorkFlow();

  const handleUndo = useCallback(() => {
    workFlow.undo();
  }, [workFlow]);

  const handleRedo = useCallback(() => {
    workFlow.redo();
  }, [workFlow]);

  return (
    <MiniFloatContainer className={"workflow-operation-bar" + (float ? " float" : "")}>
      <Space>
        <Button
          // type={"text"}
          type="dashed"
          size="small"
          icon={undoIcon}
          disabled={undoList.length === 0}
          onClick={handleUndo}
        />
        <Button
          // type={"text"}
          type="dashed"
          size="small"
          disabled={redoList.length === 0}
          icon={redoIcon}
          onClick={handleRedo}
        />
      </Space>
    </MiniFloatContainer>
  );
});
