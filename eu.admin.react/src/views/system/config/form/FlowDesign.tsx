import { useEffect, useCallback, useState, useRef } from "react";
import { Spin } from "antd";
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
  moduleId: string; // 模块ID（SmModules.ID）
  formVo: FormVo; // 映射后的表单模型
}

/** 草稿保存防抖间隔（毫秒） */
const DRAFT_DEBOUNCE_MS = 1500;

const FlowDesign: React.FC<FlowDesignProps> = props => {
  const dispatch = useDispatch();
  const { moduleId, formVo } = props;

  const lang = Lang.cn;
  const themeMode = "light";

  // 从服务端加载的流程节点树，作为 WorkflowEditor 的初始值
  const [flowNode, setFlowNode] = useState<IWorkFlowNode | undefined>();
  const [loading, setLoading] = useState(true);

  // 草稿防抖保存的 timer ref
  const draftTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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

  // 编辑器内容变更：双写 Redux + 防抖保存草稿
  const handleDataChange = useCallback(
    (node?: IWorkFlowNode) => {
      if (!node) return;
      // 实时同步到 Redux，保证 PublishButton 拿到最新数据
      dispatch(SET_START_NODE(node));

      // 防抖保存草稿到后端
      if (draftTimerRef.current) clearTimeout(draftTimerRef.current);
      draftTimerRef.current = setTimeout(async () => {
        try {
          await http.put(`/api/SmWorkFlow/SaveDraft/${moduleId}`, JSON.stringify(node), {
            headers: { "Content-Type": "application/json" }
          });
        } catch (error) {
          console.warn("保存草稿失败:", error);
        }
      }, DRAFT_DEBOUNCE_MS);
    },
    [moduleId, dispatch]
  );

  // 清理防抖 timer
  useEffect(() => {
    return () => {
      if (draftTimerRef.current) clearTimeout(draftTimerRef.current);
    };
  }, []);

  if (loading) {
    return (
      <div style={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100%" }}>
        <Spin size="large" tip="加载流程配置..." />
      </div>
    );
  }

  return (
    <WorkflowEditor
      themeMode={themeMode}
      lang={lang}
      onDataChange={handleDataChange}
      materialUis={materialUis}
      flowNode={flowNode}
      formVo={formVo}
    />
  );
};

export default FlowDesign;
