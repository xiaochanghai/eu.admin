import { useEffect, useCallback, useMemo, useState } from "react";
import { Spin } from "antd";
import { FormVo } from "@/api/Form";
import { FormFieldVo } from "@/api/FormField";
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

const createConditionFields = (entityType: string): FormFieldVo[] =>
  [
    { fieldName: "title", title: "申请标题", fieldType: "string" },
    { fieldName: "amount", title: "申请金额", fieldType: "number" },
    { fieldName: "applyDate", title: "申请日期", fieldType: "date" },
    { fieldName: "status", title: "申请状态", fieldType: "string" },
    { fieldName: "urgent", title: "是否紧急", fieldType: "boolean" }
  ].map((field, index) =>
    ({
      ...field,
      id: `mock-${field.fieldName}`,
      entityType,
      entityFieldName: field.fieldName,
      pathName: field.fieldName,
      javaType: field.fieldType,
      javaTitle: field.title,
      dataType: "basic",
      formId: "mock-workflow-form",
      form: {} as FormVo,
      description: `流程条件模拟字段：${field.title}`,
      sort: index,
      required: false,
      readOnly: false,
      dictCode: null,
      dataIndex: field.fieldName,
      x_hidden: false,
      disabled: false,
      x_read_pretty: false,
      listHide: false,
      listSearch: false,
      listAlign: "left"
    }) as unknown as FormFieldVo
  );

const FlowDesign: React.FC<FlowDesignProps> = props => {
  const dispatch = useDispatch();
  const { moduleId, formVo } = props;

  const lang = Lang.cn;
  const themeMode = "light";
  const workflowFormVo = useMemo(
    () => (formVo.fields?.length ? formVo : { ...formVo, fields: createConditionFields(formVo.entityType || formVo.type) }),
    [formVo]
  );

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
    <WorkflowEditor
      themeMode={themeMode}
      lang={lang}
      materialUis={materialUis}
      flowNode={flowNode}
      formVo={workflowFormVo}
    />
  );
};

export default FlowDesign;
