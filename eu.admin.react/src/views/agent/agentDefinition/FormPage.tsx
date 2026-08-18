import React, { useCallback, useEffect, useImperativeHandle, useState } from "react";
import {
  Alert,
  Button,
  Descriptions,
  Divider,
  Empty,
  Flex,
  Form,
  Input,
  List,
  Select,
  Space,
  Spin,
  Tabs,
  Tag,
  Typography
} from "antd";
import { DownloadOutlined, RocketOutlined, SyncOutlined } from "@ant-design/icons";
import { message } from "@/hooks/useMessage";
import {
  AgentDefinition,
  AgentOutputMode,
  AgentRuntimeStatus,
  createAgent,
  exportAgent,
  getAgent,
  getAgentCapabilities,
  getMainAgent,
  KnowledgeReference,
  listAgents,
  listKnowledgeReferences,
  listOrchestrations,
  listPublishedSkills,
  listPublishedTools,
  MainAgentAssignment,
  OrchestrationReference,
  publishAgent,
  PublishedSkillReference,
  PublishedToolReference,
  saveAgentDraft,
  setAgentStatus,
  setMainAgent
} from "@/api/modules/agent";
import { SaveTypeEnum } from "@/typings";
import "./index.less";

interface AgentFormValues {
  code: string;
  name: string;
  description: string;
  instructions: string;
  modelProfileId: string;
  outputMode: AgentOutputMode;
  outputJsonSchema?: string;
  skillVersionIds: string[];
  toolVersionIds: string[];
  knowledgeBaseIds: string[];
  childAgentIds: string[];
  orchestrationIds: string[];
}

interface FormPageProps {
  Id?: string | null;
  IsView?: boolean | null;
  formPageRef: React.RefObject<{ onSave: () => void; onSaveAdd: () => void } | null>;
  onReload?: () => void;
  onDisabled?: (disabled: boolean) => void;
}

interface ReferenceState {
  modelProfiles: string[];
  skills: PublishedSkillReference[];
  tools: PublishedToolReference[];
  knowledgeBases: KnowledgeReference[];
  agents: Awaited<ReturnType<typeof listAgents>>;
  orchestrations: OrchestrationReference[];
}

const emptyReferences: ReferenceState = {
  modelProfiles: [],
  skills: [],
  tools: [],
  knowledgeBases: [],
  agents: [],
  orchestrations: []
};

const FormPage: React.FC<FormPageProps> = ({ Id, IsView, formPageRef, onReload, onDisabled }) => {
  const [form] = Form.useForm<AgentFormValues>();
  const outputMode = Form.useWatch("outputMode", form);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [dirty, setDirty] = useState(false);
  const [agent, setAgent] = useState<AgentDefinition | null>(null);
  const [references, setReferences] = useState<ReferenceState>(emptyReferences);
  const [mainAssignment, setMainAssignmentState] = useState<MainAgentAssignment | null>(null);

  const archived = agent?.RuntimeStatus === "Archived";
  const readOnly = Boolean(IsView || archived);
  const latestVersion = agent?.PublishedVersions.at(-1);
  const isMainAgent = Boolean(agent && mainAssignment?.AgentId === agent.Id);
  const isCurrentMainVersion = Boolean(isMainAgent && latestVersion?.Id === mainAssignment?.AgentVersionId);

  const fillAgent = useCallback((value: AgentDefinition) => {
    setAgent(value);
    form.setFieldsValue({
      code: value.Code,
      name: value.Name,
      description: value.Description,
      instructions: value.Draft.Instructions,
      modelProfileId: value.Draft.ModelProfileId || undefined,
      outputMode: value.Draft.OutputMode || "Text",
      outputJsonSchema: value.Draft.OutputJsonSchema || undefined,
      skillVersionIds: value.Draft.SkillVersionIds || [],
      toolVersionIds: value.Draft.ToolVersionIds || [],
      knowledgeBaseIds: value.Draft.KnowledgeBaseIds || [],
      childAgentIds: value.Draft.ChildAgentIds || [],
      orchestrationIds: value.Draft.OrchestrationIds || []
    });
    setDirty(false);
    onDisabled?.(true);
  }, [form, onDisabled]);

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError("");
    try {
      const [capabilities, skills, tools, knowledgeBases, agents, orchestrations, currentAgent] = await Promise.all([
        getAgentCapabilities(),
        listPublishedSkills(),
        listPublishedTools(),
        listKnowledgeReferences(),
        listAgents("Enabled"),
        listOrchestrations(),
        Id ? getAgent(Id) : Promise.resolve(null)
      ]);
      setReferences({
        modelProfiles: capabilities.ModelProfileIds || [],
        skills,
        tools,
        knowledgeBases,
        agents,
        orchestrations: orchestrations.filter(item => item.Status === "Enabled" && item.CurrentPublishedLabel)
      });
      if (currentAgent) fillAgent(currentAgent);
      else {
        form.setFieldsValue({ outputMode: "Text", skillVersionIds: [], toolVersionIds: [], knowledgeBaseIds: [], childAgentIds: [], orchestrationIds: [] });
        onDisabled?.(false);
      }
      try {
        setMainAssignmentState(await getMainAgent());
      } catch {
        setMainAssignmentState(null);
      }
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : "Agent 配置加载失败");
    } finally {
      setLoading(false);
    }
  }, [Id, fillAgent, form, onDisabled]);

  useEffect(() => {
    void load();
  }, [load]);

  const save = useCallback(async (saveType = SaveTypeEnum.Save) => {
    if (readOnly || submitting) return;
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      let current = agent;
      if (!current) {
        current = await createAgent({ code: values.code.trim(), name: values.name.trim(), description: values.description || "" });
        setAgent(current);
        onReload?.();
      }
      const updated = await saveAgentDraft(current.Id, {
        expectedLogicalRevision: current.LogicalRevision,
        name: values.name.trim(),
        description: values.description || "",
        instructions: values.instructions || "",
        modelProfileId: values.modelProfileId,
        outputMode: values.outputMode,
        outputJsonSchema: values.outputMode === "Structured" ? values.outputJsonSchema?.trim() || null : null,
        skillVersionIds: values.skillVersionIds || [],
        toolVersionIds: values.toolVersionIds || [],
        knowledgeBaseIds: values.knowledgeBaseIds || [],
        childAgentIds: values.childAgentIds || [],
        orchestrationIds: values.orchestrationIds || []
      });
      fillAgent(updated);
      onReload?.();
      message.success(agent ? "Agent Draft 已保存" : "Agent 已创建并保存 Draft");
      if (saveType === SaveTypeEnum.SaveAdd) {
        setAgent(null);
        form.resetFields();
        form.setFieldsValue({ outputMode: "Text", skillVersionIds: [], toolVersionIds: [], knowledgeBaseIds: [], childAgentIds: [], orchestrationIds: [] });
        setDirty(false);
        onDisabled?.(false);
      }
    } finally {
      setSubmitting(false);
    }
  }, [agent, fillAgent, form, onDisabled, onReload, readOnly, submitting]);

  useImperativeHandle(formPageRef, () => ({
    onSave: () => void save(),
    onSaveAdd: () => void save(SaveTypeEnum.SaveAdd)
  }), [save]);

  const requireSaved = () => {
    if (!dirty) return true;
    message.warning("请先保存 Draft，再执行发布或状态操作");
    return false;
  };

  const handlePublish = async () => {
    if (!agent || !requireSaved()) return;
    setSubmitting(true);
    try {
      const updated = await publishAgent(agent.Id, agent.LogicalRevision);
      fillAgent(updated);
      if (isMainAgent) setMainAssignmentState(await getMainAgent());
      onReload?.();
      message.success(`已发布 v${updated.PublishedVersions.at(-1)?.Label}`);
    } finally {
      setSubmitting(false);
    }
  };

  const handleStatus = async (next: AgentRuntimeStatus) => {
    if (!agent || !requireSaved()) return;
    setSubmitting(true);
    try {
      const updated = await setAgentStatus(agent.Id, next, agent.LogicalRevision);
      fillAgent(updated);
      onReload?.();
      message.success(next === "Enabled" ? "Agent 已启用" : next === "Disabled" ? "Agent 已停用" : "Agent 已归档");
    } finally {
      setSubmitting(false);
    }
  };

  const handleSetMainAgent = async () => {
    if (!agent || !requireSaved()) return;
    setSubmitting(true);
    try {
      const assignment = await setMainAgent(agent.Id, mainAssignment?.LogicalRevision ?? null);
      setMainAssignmentState(assignment);
      message.success("已设为 Main Agent");
    } finally {
      setSubmitting(false);
    }
  };

  const handleExport = async () => {
    if (!agent || !requireSaved()) return;
    const blob = await exportAgent(agent.Id);
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `${agent.Code}.agent-package.json`;
    link.click();
    URL.revokeObjectURL(url);
  };

  const skillOptions = references.skills.map(item => ({
    value: item.VersionId,
    label: `${item.SkillName || item.SkillCode} · v${item.VersionLabel}`
  }));
  const toolOptions = references.tools.map(item => ({
    value: item.ToolVersionId,
    label: `${item.ToolName} · ${item.ServerCode} · ${item.Risk}`
  }));
  const knowledgeOptions = references.knowledgeBases.map(item => ({
    value: item.KnowledgeBaseId,
    label: `${item.Name || item.Code} · REV ${item.LogicalRevision}`
  }));
  const agentOptions = references.agents.filter(item => item.Id !== agent?.Id && item.CurrentPublishedLabel).map(item => ({
    value: item.Id,
    label: `${item.Name || item.Code} · v${item.CurrentPublishedLabel}`
  }));
  const orchestrationOptions = references.orchestrations.map(item => ({
    value: item.Id,
    label: `${item.Name || item.Code} · v${item.CurrentPublishedLabel}`
  }));

  const basicPanel = (
    <div className="agent-definition-form__section">
      <Flex gap={16} wrap>
        <Form.Item name="code" label="Agent Code" rules={[{ required: true }, { pattern: /^[a-z0-9]+(?:-[a-z0-9]+)*$/, message: "请输入小写 kebab-case" }]} className="agent-definition-form__half">
          <Input disabled={Boolean(agent) || readOnly} maxLength={64} placeholder="例如 business-query" />
        </Form.Item>
        <Form.Item name="name" label="名称" rules={[{ required: true }]} className="agent-definition-form__half">
          <Input disabled={readOnly} maxLength={128} />
        </Form.Item>
      </Flex>
      <Form.Item name="description" label="职责说明">
        <Input.TextArea disabled={readOnly} autoSize={{ minRows: 2, maxRows: 5 }} maxLength={1000} showCount />
      </Form.Item>
      <Form.Item name="instructions" label="Instructions" rules={[{ required: true, message: "发布前必须填写 Instructions" }]}>
        <Input.TextArea disabled={readOnly} autoSize={{ minRows: 9, maxRows: 18 }} maxLength={32768} showCount />
      </Form.Item>
      <Flex gap={16} wrap>
        <Form.Item name="modelProfileId" label="模型配置" rules={[{ required: true }]} className="agent-definition-form__half">
          <Select disabled={readOnly} options={references.modelProfiles.map(value => ({ value, label: value }))} placeholder="选择 Model Profile" />
        </Form.Item>
        <Form.Item name="outputMode" label="输出模式" rules={[{ required: true }]} className="agent-definition-form__half">
          <Select disabled={readOnly} options={[{ value: "Text", label: "Text" }, { value: "Structured", label: "Structured" }]} />
        </Form.Item>
      </Flex>
      {outputMode === "Structured" && (
        <Form.Item
          name="outputJsonSchema"
          label="JSON Schema"
          rules={[{ required: true }, {
            validator: (_, value) => {
              if (!value) return Promise.resolve();
              try { JSON.parse(value); return Promise.resolve(); }
              catch { return Promise.reject(new Error("请输入有效 JSON")); }
            }
          }]}
        >
          <Input.TextArea disabled={readOnly} className="agent-definition-form__code" autoSize={{ minRows: 8, maxRows: 16 }} />
        </Form.Item>
      )}
    </div>
  );

  const bindingsPanel = (
    <div className="agent-definition-form__section agent-definition-form__bindings">
      <Typography.Paragraph type="secondary">
        Draft 保存引用标识；发布时服务端会冻结实际版本、风险分类和知识库修订。
      </Typography.Paragraph>
      <Form.Item name="skillVersionIds" label="Skills">
        <Select mode="multiple" disabled={readOnly} options={skillOptions} optionFilterProp="label" placeholder="选择已发布 Skill 版本" />
      </Form.Item>
      <Form.Item name="toolVersionIds" label="MCP 工具">
        <Select mode="multiple" disabled={readOnly} options={toolOptions} optionFilterProp="label" placeholder="选择已完成风险分类的工具" />
      </Form.Item>
      <Form.Item name="knowledgeBaseIds" label="知识库">
        <Select mode="multiple" disabled={readOnly} options={knowledgeOptions} optionFilterProp="label" placeholder="选择已启用并完成索引的知识库" />
      </Form.Item>
      <Form.Item name="childAgentIds" label="子 Agent">
        <Select mode="multiple" disabled={readOnly} options={agentOptions} optionFilterProp="label" placeholder="选择已启用且已发布的 Agent" />
      </Form.Item>
      <Form.Item name="orchestrationIds" label="编排">
        <Select mode="multiple" disabled={readOnly} options={orchestrationOptions} optionFilterProp="label" placeholder="选择已启用且已发布的编排" />
      </Form.Item>
    </div>
  );

  const versionsPanel = (
    <div className="agent-definition-form__section">
      {agent?.PublishedVersions.length ? (
        <List
          dataSource={[...agent.PublishedVersions].reverse()}
          renderItem={(version, index) => (
            <List.Item>
              <List.Item.Meta
                title={<Space><Typography.Text strong>v{version.Label}</Typography.Text><Tag>{version.OutputMode}</Tag>{index === 0 && <Tag color="blue">最新</Tag>}</Space>}
                description={`${version.ModelProfileId} · ${version.SkillVersionIds.length} Skills · ${version.ToolVersionIds.length} Tools · ${version.KnowledgeBaseIds.length} 知识库`}
              />
            </List.Item>
          )}
        />
      ) : <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="尚未发布版本" />}
    </div>
  );

  const items = [
    { key: "configuration", label: "基础配置", children: basicPanel },
    { key: "bindings", label: "能力绑定", children: bindingsPanel },
    { key: "versions", label: `发布历史${agent ? ` (${agent.PublishedVersions.length})` : ""}`, children: versionsPanel }
  ];

  return (
    <Spin spinning={loading || submitting}>
      <div className="agent-definition-form">
        {loadError && (
          <Alert
            type="error"
            showIcon
            message="Agent 配置加载失败"
            description={loadError}
            action={<Button size="small" onClick={() => void load()}>重试</Button>}
          />
        )}
        {agent && (
          <>
            <Flex justify="space-between" align="center" gap={12} wrap className="agent-definition-form__statusbar">
              <Descriptions size="small" column={{ xs: 1, sm: 2, lg: 4 }}>
                <Descriptions.Item label="状态"><Tag color={agent.RuntimeStatus === "Enabled" ? "success" : agent.RuntimeStatus === "Archived" ? "warning" : "default"}>{agent.RuntimeStatus}</Tag></Descriptions.Item>
                <Descriptions.Item label="Draft">{agent.Draft.Label} · REV {agent.LogicalRevision}</Descriptions.Item>
                <Descriptions.Item label="最新版本">{latestVersion ? `v${latestVersion.Label}` : "尚未发布"}</Descriptions.Item>
                <Descriptions.Item label="部署">{agent.DeploymentTarget} / {agent.Host}</Descriptions.Item>
              </Descriptions>
              <Space wrap>
                <Button icon={<DownloadOutlined />} onClick={() => void handleExport()} disabled={dirty}>导出</Button>
                {agent.RuntimeStatus !== "Archived" && (
                  <Button icon={<RocketOutlined />} type="primary" onClick={() => void handlePublish()} disabled={dirty || readOnly}>发布版本</Button>
                )}
                {agent.RuntimeStatus === "Enabled" ? (
                  <Button onClick={() => void handleStatus("Disabled")} disabled={dirty || IsView === true}>停用</Button>
                ) : agent.RuntimeStatus === "Disabled" ? (
                  <><Button onClick={() => void handleStatus("Enabled")} disabled={dirty || IsView === true}>启用</Button><Button danger onClick={() => void handleStatus("Archived")} disabled={dirty || IsView === true}>归档</Button></>
                ) : (
                  <Button onClick={() => void handleStatus("Disabled")} disabled={IsView === true}>恢复</Button>
                )}
                <Button
                  icon={<SyncOutlined />}
                  onClick={() => void handleSetMainAgent()}
                  disabled={dirty || readOnly || agent.RuntimeStatus !== "Enabled" || !latestVersion || isCurrentMainVersion}
                >
                  {isCurrentMainVersion ? "当前 Main Agent" : isMainAgent ? "更新 Main Agent" : "设为 Main Agent"}
                </Button>
              </Space>
            </Flex>
            {archived && <Alert type="warning" showIcon message="该 Agent 已归档，当前仅允许查看或恢复。" />}
            <Divider />
          </>
        )}
        <Form
          form={form}
          layout="vertical"
          requiredMark="optional"
          onValuesChange={() => {
            setDirty(true);
            onDisabled?.(false);
          }}
        >
          <Tabs items={items} destroyOnHidden={false} />
        </Form>
      </div>
    </Spin>
  );
};

export default FormPage;
