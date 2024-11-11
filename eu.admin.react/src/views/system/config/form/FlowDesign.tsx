import { FormVo } from "@/api/Form";
import { Lang } from "@/workflow/component";
import { materialUis } from "@/workflow/materialUis";
import { WorkflowEditor } from "@/workflow/WorkflowEditor";
import { IWorkFlowNode } from "@/workflow-editor";
import { useState } from "react";

export interface FlowSettingProps {
  type: string; // dto模型
  formVo: FormVo; // 模型信息
  onDataChange: (flowJSON: string) => void;
}
export default (props: FlowSettingProps) => {
  const { formVo, onDataChange } = props;
  const lang = Lang.cn;
  const themeMode = "light";
  const [flowNode] = useState<IWorkFlowNode>();
  // setFlowNode("");
  return (
    <WorkflowEditor
      themeMode={themeMode}
      lang={lang}
      onDataChange={node => {
        if (flowNode !== node) {
          onDataChange(JSON.stringify(node));
        }
      }}
      //dlc 卡片，setting 和校验的配置信息
      materialUis={materialUis}
      flowNode={flowNode}
      formVo={formVo}
    />
  );
};
