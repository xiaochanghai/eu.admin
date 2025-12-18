import { useEffect, useCallback } from "react";
import { FormVo } from "@/api/Form";
import { Lang } from "@/workflow/component";
import { materialUis } from "@/workflow/materialUis";
import { WorkflowEditor } from "@/workflow/WorkflowEditor";
import { IWorkFlowNode } from "@/workflow-editor";
import { useState } from "react";
import http from "@/api";
import { useDispatch } from "@/redux";
import { SET_START_NODE } from "@/redux/modules/workflow";

export interface FlowSettingProps {
  type: string; // dto模型
  formVo: FormVo; // 模型信息
  onDataChange: (flowJSON: string) => void;
}
export default (props: FlowSettingProps) => {
  const dispatch = useDispatch();

  const { formVo, onDataChange } = props;
  const { id } = formVo;

  const lang = Lang.cn;
  const themeMode = "light";
  const [flowNode] = useState<IWorkFlowNode>();
  // setFlowNode("");

  const fetchModuleInfo = useCallback(async () => {
    try {
      let { Data } = await http.get<any>(`/api/SmWorkFlow/QueryNode/${id}`);

      if (Data) dispatch(SET_START_NODE(Data));
    } catch (error) {}
  }, [dispatch]);

  // 组件挂载或moduleCode变化时，检查并获取模块信息
  useEffect(() => {
    fetchModuleInfo();
  }, [fetchModuleInfo]);
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
