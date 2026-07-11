import { useEffect, useCallback, useState } from "react";
import { Alert, Spin } from "antd";
import { FormVo } from "@/api/Form";
import { Lang } from "@/workflow/component";
import { materialUis } from "@/workflow/materialUis";
import { WorkflowEditor } from "@/workflow/WorkflowEditor";
import { IWorkFlowNode } from "@/workflow-editor";
import http from "@/api";
import { useDispatch } from "@/redux";
import { SET_START_NODE, SET_FORM_ID } from "@/redux/modules/workflow";
import { message } from "@/hooks/useMessage";

export interface FlowDesignProps {
  moduleId: string;
  formVo: FormVo;
}

const FlowDesign: React.FC<FlowDesignProps> = props => {
  const dispatch = useDispatch();
  const { moduleId, formVo } = props;

  const lang = Lang.cn;
  const themeMode = "light";
  // 从服务端加载的流程节点树，作为 WorkflowEditor 的初始值
  const [flowNode, setFlowNode] = useState<IWorkFlowNode | undefined>();
  const [loading, setLoading] = useState(true);

  // 加载当前模块对应的流程节点（优先草稿 → 已发布 → 节点表重建）
  const fetchFlowNode = useCallback(async () => {
    if (!moduleId) return;
    setLoading(true);
    try {
      const { Data } = await http.get<IWorkFlowNode>(`/api/SmWorkFlow/QueryNodeByModule/${moduleId}`);
      if (Data) {
        setFlowNode(Data);
        dispatch(SET_START_NODE(Data));
      }
    } catch (error) {
      console.error("加载流程节点失败:", error);
      message.error("加载流程配置失败，请刷新重试");
    } finally {
      setLoading(false);
    }
  }, [moduleId, dispatch]);

  // 组件挂载或 moduleId 变化时：
  // 1. 将 SmWorkFlow.ID 写入 Redux（PublishButton 读取）
  // 2. 加载流程节点
  useEffect(() => {
    dispatch(SET_FORM_ID(formVo.id));
    fetchFlowNode();
  }, [moduleId, formVo.id, dispatch, fetchFlowNode]);

  if (loading) {
    return (
      <div style={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100%" }}>
        <Spin size="large" tip="加载流程配置..." />
      </div>
    );
  }

  return (
    <>
      {formVo.fields.length === 0 && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message="未配置可用于流程条件的表单字段"
          description="请先在 PC 申请表单中配置模块字段。流程条件只使用真实 SmModuleColumn 字段。"
        />
      )}
      <WorkflowEditor themeMode={themeMode} lang={lang} materialUis={materialUis} flowNode={flowNode} formVo={formVo} />
    </>
  );
};

export default FlowDesign;
