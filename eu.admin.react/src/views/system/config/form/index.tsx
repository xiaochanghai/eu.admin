import React, { useEffect, useState, useCallback } from "react";
import { useSearchParams } from "react-router-dom";
import { Card, Spin, Result, Button, Space, Tabs, Descriptions, } from "antd";
import { ItemType } from "@/api/base";
import { FormVo } from "@/api/Form";
import { FormFieldVo } from "@/api/FormField";
import http from "@/api";
import FlowDesign from "./FlowDesign";
import FormDesign from "./components/FormDesign";
import MobileFormPreview from "./components/MobileFormPreview";
import { Icon } from "@/components";

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

interface SmModuleColumn {
  ID: string;
  DataIndex?: string;
  Title?: string;
  FormTitle?: string;
  FieldType?: string;
  ValueType?: string;
  Required?: boolean;
  HideInForm?: boolean;
  FormTaxisNo?: number;
  ColumnMode?: string;
  [key: string]: any;
}

const resolveFieldType = (column: SmModuleColumn): FormFieldVo["fieldType"] => {
  const type = `${column.ValueType || ""} ${column.FieldType || ""}`.toLowerCase();
  if (type.includes("date") || type.includes("time")) return "date";
  if (type.includes("number") || type.includes("decimal") || type.includes("int") || type.includes("money")) return "number";
  if (type.includes("bool") || type.includes("switch") || type.includes("checkbox")) return "boolean";
  return "string";
};

const mapColumnsToFormFields = (columns: SmModuleColumn[], formId: string, entityType: string): FormFieldVo[] =>
  columns
    .filter(column => !!column.DataIndex && column.ColumnMode !== "list")
    .sort((left, right) => (left.FormTaxisNo || 0) - (right.FormTaxisNo || 0))
    .map((column, index) => {
      const fieldName = column.DataIndex!;
      const fieldType = resolveFieldType(column);
      return {
        id: column.ID,
        fieldName,
        entityFieldName: fieldName,
        pathName: fieldName,
        dataIndex: fieldName,
        title: column.FormTitle || column.Title || fieldName,
        javaTitle: column.FormTitle || column.Title || fieldName,
        fieldType,
        javaType: fieldType,
        entityType,
        dataType: "basic",
        formId,
        form: {} as FormVo,
        description: "",
        sort: column.FormTaxisNo ?? index,
        required: column.Required === true,
        readOnly: column.Disabled === true,
        dictCode: column.DataSource || "",
        x_hidden: column.HideInForm !== false,
        disabled: column.Disabled === true,
        x_read_pretty: false,
        listHide: column.HideInTable !== false,
        listSearch: false,
        listAlign: "left"
      } as unknown as FormFieldVo;
    });

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
interface FormConfigPageProps {
  moduleId?: string;
  onBack?: () => void;
}

const FormConfigPage: React.FC<FormConfigPageProps> = ({ moduleId: moduleIdProp, onBack }) => {
  const [searchParams] = useSearchParams();
  const moduleId = moduleIdProp ?? searchParams.get("moduleId") ?? "";

  const [formVo, setFormVo] = useState<FormVo | null>(null);
  const [formColumns, setFormColumns] = useState<SmModuleColumn[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  const reloadFormColumns = useCallback(async () => {
    if (!moduleId) return;
    const { Data } = await http.get<SmModuleColumn[]>(`/api/SmModule/FormColumn/${moduleId}`);
    const columns = Data || [];
    setFormColumns(columns);
    setFormVo(current =>
      current
        ? {
          ...current,
          fields: mapColumnsToFormFields(columns, current.id, current.entityType || moduleId)
        }
        : current
    );
  }, [moduleId]);

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
      const [workflowResult, columnsResult] = await Promise.all([
        http.get<SmWorkFlowData>(`/api/SmWorkFlow/ForModule/${moduleId}`),
        http.get<SmModuleColumn[]>(`/api/SmModule/FormColumn/${moduleId}`)
      ]);
      if (workflowResult.Data) {
        const columns = columnsResult.Data || [];
        const workflowForm = mapToFormVo(workflowResult.Data);
        workflowForm.entityType = workflowResult.Data.SmModuleId || moduleId;
        workflowForm.fields = mapColumnsToFormFields(columns, workflowForm.id, workflowForm.entityType);
        setFormColumns(columns);
        setFormVo(workflowForm);
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


  return (
    <>
      {onBack && (
        <Space style={{ display: "flex", justifyContent: "flex-end", marginBottom: 10 }}>
          <Button type="default" icon={<Icon name="RollbackOutlined" />} onClick={onBack} />
        </Space>
      )}
      <Card>

        <Descriptions title="表单配置" />

        <Tabs
          defaultActiveKey="workflow"
          onChange={key => {
            if (key !== "pc-form") reloadFormColumns();
          }}
          items={[
            {
              key: "workflow",
              label: "流程设计",
              children: <FlowDesign moduleId={moduleId} formVo={formVo} />
            },
            {
              key: "pc-form",
              label: "PC 申请表单",
              children: <FormDesign ModuleId={moduleId} embedded />
            },
            {
              key: "mobile-form",
              label: "移动端申请表单",
              children: <MobileFormPreview fields={formColumns} />
            }
          ]}
        />
      </Card></>
  );
};

export default FormConfigPage;
