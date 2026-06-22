import React, { useEffect, useState, useCallback } from "react";
import { useSearchParams } from "react-router-dom";
import { Spin, Result, Button } from "antd";
import { ItemType } from "@/api/base";
import { FormVo } from "@/api/Form";
import http from "@/api";
import FlowDesign from "./FlowDesign";

/**
 * SmWorkFlow API 返回的数据结构（仅列出前端用到的字段）
 */
interface SmWorkFlowData {
  ID: string;
  SmModuleId: string;
  FlowCode: string;
  FlowName: string;
  Remark: string;
}

/**
 * 将后端 SmWorkFlow 实体映射为前端 FormVo 结构
 * WorkflowEditor 主要使用 formVo.id / title / name / type 这几个字段
 */
const mapToFormVo = (wf: SmWorkFlowData): FormVo => ({
  id: wf.ID,
  title: wf.FlowName || "",
  name: wf.FlowName || "",
  type: wf.FlowCode || "",
  itemType: ItemType.entity,
  entityType: "",
  sort: 0,
  icon: "",
  modelSize: 2,
  pageSize: 10,
  version: 0,
  listApiPath: "",
  saveApiPath: "",
  prefixNo: "",
  sysMenuId: wf.SmModuleId || "",
  formDesc: wf.Remark || "",
  itemName: "",
  helpDoc: "",
  typeParentsStr: "",
  flowJson: "",
  unpublishJson: "{}",
  unpublishForm: "",
  orders: "",
  custom: false,
  state: "1",
  supportFilter: false,
  supportNo: false,
  rules: [],
  fields: [],
  formTabDtos: [],
  resources: []
});

/**
 * 流程设计器页面
 * URL: /system/config/form?moduleId=xxx
 *
 * 加载流程：
 *   1. 根据 moduleId 获取（或自动初始化）SmWorkFlow
 *   2. 将 SmWorkFlow 映射为 FormVo 供 WorkflowEditor 使用
 *   3. FlowDesign 内部再根据 moduleId 加载流程节点树
 */
const FormConfigPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const moduleId = searchParams.get("moduleId") ?? "";

  const [formVo, setFormVo] = useState<FormVo | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  // 根据 moduleId 加载 SmWorkFlow
  const fetchWorkflow = useCallback(async () => {
    if (!moduleId) {
      setLoading(false);
      setLoadError(true);
      return;
    }
    setLoading(true);
    setLoadError(false);
    try {
      const { Data } = await http.get<SmWorkFlowData>(`/api/SmWorkFlow/ForModule/${moduleId}`);
      if (Data) {
        setFormVo(mapToFormVo(Data));
      } else {
        setLoadError(true);
      }
    } catch (error) {
      console.error("加载工作流失败:", error);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, [moduleId]);

  useEffect(() => {
    fetchWorkflow();
  }, [fetchWorkflow]);

  // 加载中
  if (loading) {
    return (
      <div style={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100%" }}>
        <Spin size="large" tip="加载工作流配置..." />
      </div>
    );
  }

  // 加载失败或无 moduleId
  if (loadError || !formVo) {
    return (
      <Result
        status="error"
        title="加载失败"
        subTitle={moduleId ? "无法获取工作流信息，请检查模块 ID 是否正确" : "缺少模块 ID 参数，请从菜单进入此页面"}
        extra={
          <Button type="primary" onClick={fetchWorkflow}>
            重试
          </Button>
        }
      />
    );
  }

  return <FlowDesign moduleId={moduleId} formVo={formVo} />;
};

export default FormConfigPage;
