import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  ApartmentOutlined,
  DeleteOutlined,
  FilterOutlined,
  InboxOutlined,
  PlayCircleOutlined,
  PlusOutlined,
  RocketOutlined,
  SaveOutlined,
  StopOutlined
} from "@ant-design/icons";
import {
  Alert,
  Button,
  Collapse,
  Empty,
  Flex,
  Form,
  Input,
  InputNumber,
  List,
  message,
  Modal,
  Select,
  Space,
  Spin,
  Tag,
  Timeline,
  Typography
} from "antd";
import type { FormInstance } from "antd";
import {
  cancelOrchestrationRun,
  createOrchestration,
  getOrchestration,
  getOrchestrationErrorMessage,
  getOrchestrationRun,
  getOrchestrationRunDetails,
  listOrchestrationDefinitions,
  publishOrchestration,
  saveOrchestrationDraft,
  setOrchestrationArchived,
  startOrchestrationRun
} from "@/api/modules/agentOrchestration";
import type {
  OrchestrationDefinition,
  OrchestrationEdgeCondition,
  OrchestrationListItem,
  OrchestrationNodeInputMode,
  OrchestrationRunDetails,
  OrchestrationRunRecord,
  OrchestrationStatus
} from "@/api/modules/agentOrchestration";
import { listAgents } from "@/api/modules/agent";
import type { AgentListItem } from "@/api/modules/agent";
import { getModuleInfo } from "@/api/modules/module";
import "./index.less";

const MODULE_CODE = "AG_ORCHESTRATION_MNG";

interface NodeFormValue {
  id: string;
  name: string;
  agentId: string;
  inputMode: OrchestrationNodeInputMode;
  inputTemplate: string;
  maximumRetries: number;
  timeoutSeconds: number;
}

interface EdgeFormValue {
  fromNodeId: string;
  toNodeId: string;
  condition: OrchestrationEdgeCondition;
  conditionValue: string;
  order: number;
}

interface OrchestrationFormValue {
  code: string;
  name: string;
  description: string;
  status: OrchestrationStatus;
  nodes: NodeFormValue[];
  edges: EdgeFormValue[];
}

const statusMeta = {
  Enabled: { color: "success", text: "已启用" },
  Disabled: { color: "default", text: "已停用" },
  Archived: { color: "warning", text: "已归档" }
} as const;

const runStatusMeta = {
  Running: { color: "processing", text: "运行中" },
  Completed: { color: "success", text: "已完成" },
  Failed: { color: "error", text: "失败" },
  Cancelled: { color: "default", text: "已取消" }
} as const;

const newNode = (index: number): NodeFormValue => ({
  id: `node-${index + 1}`,
  name: "",
  agentId: "",
  inputMode: "InitialInput",
  inputTemplate: "",
  maximumRetries: 0,
  timeoutSeconds: 120
});

const emptyForm = (): OrchestrationFormValue => ({
  code: "",
  name: "",
  description: "",
  status: "Enabled",
  nodes: [newNode(0)],
  edges: []
});

const definitionToForm = (value: OrchestrationDefinition): OrchestrationFormValue => ({
  code: value.Code,
  name: value.Name,
  description: value.Description,
  status: value.Status,
  nodes: value.Draft.Nodes.map(node => ({
    id: node.Id,
    name: node.Name,
    agentId: node.AgentId,
    inputMode: node.InputMode,
    inputTemplate: node.InputTemplate,
    maximumRetries: node.MaximumRetries,
    timeoutSeconds: node.TimeoutSeconds
  })),
  edges: value.Draft.Edges.map(edge => ({
    fromNodeId: edge.FromNodeId,
    toNodeId: edge.ToNodeId,
    condition: edge.Condition,
    conditionValue: edge.ConditionValue,
    order: edge.Order
  }))
});

const prettyPayload = (value?: string | null) => {
  if (!value) return "（空）";
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
};

const getHttpStatus = (error: unknown) => {
  if (typeof error !== "object" || error === null) return undefined;
  return (error as { response?: { status?: number } }).response?.status;
};

const isRetryablePollError = (error: unknown) => {
  const statusCode = getHttpStatus(error);
  return statusCode === undefined || statusCode === 408 || statusCode === 429 || statusCode >= 500;
};

const maximumPollRetries = 5;

const Payload: React.FC<{ label: string; value?: string | null }> = ({ label, value }) => (
  <div className="orchestration-page__payload">
    <Typography.Text type="secondary">{label}</Typography.Text>
    <pre>{prettyPayload(value)}</pre>
  </div>
);

const applyDefinition = (
  value: OrchestrationDefinition,
  form: FormInstance<OrchestrationFormValue>,
  setCurrent: React.Dispatch<React.SetStateAction<OrchestrationDefinition | null>>,
  setDirty: React.Dispatch<React.SetStateAction<boolean>>
) => {
  form.resetFields();
  form.setFieldsValue(definitionToForm(value));
  setCurrent(value);
  setDirty(false);
};

const OrchestrationPage: React.FC = () => {
  const [form] = Form.useForm<OrchestrationFormValue>();
  const [moduleActions, setModuleActions] = useState<Set<string>>(() => new Set());
  const [items, setItems] = useState<OrchestrationListItem[]>([]);
  const [agents, setAgents] = useState<AgentListItem[]>([]);
  const [status, setStatus] = useState<OrchestrationStatus | undefined>();
  const [current, setCurrent] = useState<OrchestrationDefinition | null>(null);
  const [creating, setCreating] = useState(false);
  const [dirty, setDirty] = useState(false);
  const [listLoading, setListLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [transitioning, setTransitioning] = useState(false);
  const [runInput, setRunInput] = useState("");
  const [activeRun, setActiveRun] = useState<OrchestrationRunRecord | null>(null);
  const [runDetails, setRunDetails] = useState<OrchestrationRunDetails | null>(null);
  const [runLoading, setRunLoading] = useState(false);
  const [cancelLoading, setCancelLoading] = useState(false);
  const [pollStopped, setPollStopped] = useState(false);
  const listRequestRef = useRef(0);
  const detailRequestRef = useRef(0);
  const pollRequestRef = useRef(0);
  const pollTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const statusRef = useRef<OrchestrationStatus | undefined>();

  useEffect(() => {
    let active = true;
    void getModuleInfo(MODULE_CODE)
      .then(({ Data }) => {
        if (active) setModuleActions(new Set(Data.actions || []));
      })
      .catch(error => {
        if (active) message.error(getOrchestrationErrorMessage(error, "编排模块权限加载失败"));
      });
    return () => {
      active = false;
    };
  }, []);

  const clearPoll = useCallback(() => {
    pollRequestRef.current += 1;
    if (pollTimerRef.current) clearTimeout(pollTimerRef.current);
    pollTimerRef.current = null;
  }, []);

  const loadList = useCallback(async (filter = statusRef.current) => {
    const request = ++listRequestRef.current;
    setListLoading(true);
    try {
      const values = await listOrchestrationDefinitions(filter);
      if (request === listRequestRef.current && filter === statusRef.current) setItems(values);
    } catch (error) {
      if (request === listRequestRef.current) message.error(getOrchestrationErrorMessage(error, "编排列表加载失败"));
    } finally {
      if (request === listRequestRef.current) setListLoading(false);
    }
  }, []);

  useEffect(() => {
    statusRef.current = status;
    void loadList(status);
  }, [loadList, status]);

  useEffect(() => {
    let active = true;
    void listAgents()
      .then(values => {
        if (active) setAgents(values.filter(agent => agent.RuntimeStatus === "Enabled" && agent.CurrentPublishedLabel));
      })
      .catch(error => {
        if (active) message.error(getOrchestrationErrorMessage(error, "Agent 列表加载失败"));
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => () => clearPoll(), [clearPoll]);

  const agentOptions = useMemo(
    () => agents.map(agent => ({ label: `${agent.Name || agent.Code} · v${agent.CurrentPublishedLabel}`, value: agent.Id })),
    [agents]
  );

  const openDefinition = useCallback(
    async (id: string) => {
      clearPoll();
      setPollStopped(false);
      const request = ++detailRequestRef.current;
      setDetailLoading(true);
      setCreating(false);
      setActiveRun(null);
      setRunDetails(null);
      try {
        const value = await getOrchestration(id);
        if (request !== detailRequestRef.current) return;
        applyDefinition(value, form, setCurrent, setDirty);
      } catch (error) {
        if (request === detailRequestRef.current) message.error(getOrchestrationErrorMessage(error, "编排详情加载失败"));
      } finally {
        if (request === detailRequestRef.current) setDetailLoading(false);
      }
    },
    [clearPoll, form]
  );

  const withDirtyConfirmation = (action: () => void) => {
    if (!dirty) {
      action();
      return;
    }
    Modal.confirm({
      title: "放弃未保存修改？",
      content: "切换后当前 Draft 修改将丢失。",
      okText: "放弃并继续",
      cancelText: "继续编辑",
      onOk: action
    });
  };

  const beginCreate = () => {
    withDirtyConfirmation(() => {
      detailRequestRef.current += 1;
      clearPoll();
      setPollStopped(false);
      setCurrent(null);
      setCreating(true);
      setActiveRun(null);
      setRunDetails(null);
      form.resetFields();
      form.setFieldsValue(emptyForm());
      setDirty(false);
    });
  };

  const selectDefinition = (id: string) => {
    if (id === current?.Id && !creating) return;
    withDirtyConfirmation(() => void openDefinition(id));
  };

  const save = async (statusOverride?: OrchestrationStatus) => {
    const workspaceRequest = detailRequestRef.current;
    setSaving(true);
    try {
      const values = await form.validateFields();
      let target = current;
      if (!target) {
        target = await createOrchestration({
          code: values.code.trim(),
          name: values.name.trim(),
          description: values.description || ""
        });
        // Creation and draft persistence are separate API operations. Retain the
        // persisted definition while keeping the edited form intact so a rejected
        // graph can be corrected and retried without creating the same code again.
        if (workspaceRequest === detailRequestRef.current) {
          setCurrent(target);
          setCreating(false);
        }
        await loadList();
      }
      const nodes = values.nodes || [];
      const saved = await saveOrchestrationDraft(target.Id, {
        expectedLogicalRevision: target.LogicalRevision,
        name: values.name.trim(),
        description: values.description || "",
        status: statusOverride || target.Status,
        startNodeId: nodes[0]?.id.trim() || "",
        nodes: nodes.map(node => ({
          ...node,
          id: node.id.trim(),
          name: node.name.trim(),
          inputTemplate: node.inputTemplate || ""
        })),
        edges: (values.edges || []).map(edge => ({
          ...edge,
          fromNodeId: edge.fromNodeId.trim(),
          toNodeId: edge.toNodeId.trim(),
          conditionValue: edge.conditionValue || ""
        }))
      });
      if (workspaceRequest === detailRequestRef.current) {
        applyDefinition(saved, form, setCurrent, setDirty);
        setCreating(false);
      }
      await loadList();
      message.success("Draft 已保存");
      return saved;
    } catch (error) {
      message.error(getOrchestrationErrorMessage(error, "Draft 保存失败"));
      return null;
    } finally {
      setSaving(false);
    }
  };

  const publish = async () => {
    const workspaceRequest = detailRequestRef.current;
    setPublishing(true);
    try {
      const saved = await save();
      if (!saved) return;
      const published = await publishOrchestration(saved.Id, saved.LogicalRevision);
      if (workspaceRequest === detailRequestRef.current) {
        applyDefinition(published, form, setCurrent, setDirty);
      }
      await loadList();
      const label = published.PublishedVersions.at(-1)?.Label;
      message.success(label ? `已发布 v${label}` : "编排已发布");
    } catch (error) {
      message.error(getOrchestrationErrorMessage(error, "编排发布失败"));
    } finally {
      setPublishing(false);
    }
  };

  const toggleStatus = async () => {
    if (!current || current.Status === "Archived") return;
    const target = current.Status === "Enabled" ? "Disabled" : "Enabled";
    const saved = await save(target);
    if (saved) message.success(target === "Enabled" ? "编排已启用" : "编排已停用，可以归档");
  };

  const toggleArchived = () => {
    if (!current) return;
    if (dirty) {
      message.warning("存在未保存修改，请先保存后再变更归档状态");
      return;
    }
    const restoring = current.Status === "Archived";
    if (!restoring && current.Status !== "Disabled") {
      message.warning("请先停用编排，再执行归档");
      return;
    }
    Modal.confirm({
      title: restoring ? "恢复该编排？" : "归档该编排？",
      content: restoring ? "恢复后状态为已停用。" : "归档后不可编辑或运行，可稍后恢复。",
      okText: restoring ? "恢复" : "归档",
      cancelText: "取消",
      onOk: async () => {
        setTransitioning(true);
        try {
          const value = await setOrchestrationArchived(current.Id, current.LogicalRevision, !restoring);
          applyDefinition(value, form, setCurrent, setDirty);
          await loadList();
          message.success(restoring ? "编排已恢复" : "编排已归档");
        } catch (error) {
          message.error(getOrchestrationErrorMessage(error, "归档状态变更失败"));
        } finally {
          setTransitioning(false);
        }
      }
    });
  };

  const pollRun = useCallback(async (
    orchestrationId: string,
    runId: string,
    request: number,
    failureCount = 0
  ) => {
    try {
      const run = await getOrchestrationRun(orchestrationId, runId);
      if (request !== pollRequestRef.current) return;
      setActiveRun(run);
      setPollStopped(false);
      if (run.Status === "Running") {
        pollTimerRef.current = setTimeout(() => void pollRun(orchestrationId, runId, request), 800);
        return;
      }
      const details = await getOrchestrationRunDetails(orchestrationId, runId);
      if (request === pollRequestRef.current) setRunDetails(details);
    } catch (error) {
      if (request === pollRequestRef.current) {
        if (isRetryablePollError(error) && failureCount < maximumPollRetries) {
          const delay = Math.min(2000 * 2 ** failureCount, 10000);
          message.warning({
            key: "orchestration-run-poll",
            content: `${getOrchestrationErrorMessage(error, "运行状态读取失败")}，将在 ${delay / 1000} 秒后重试`,
            duration: 2
          });
          pollTimerRef.current = setTimeout(
            () => void pollRun(orchestrationId, runId, request, failureCount + 1),
            delay
          );
        } else {
          setPollStopped(true);
          message.error(getOrchestrationErrorMessage(error, "运行状态读取失败，自动轮询已停止"));
        }
      }
    } finally {
      if (request === pollRequestRef.current) setRunLoading(false);
    }
  }, []);

  const startRun = async () => {
    if (!current || !runInput.trim()) return;
    clearPoll();
    setRunLoading(true);
    setPollStopped(false);
    setRunDetails(null);
    try {
      const run = await startOrchestrationRun(current.Id, runInput.trim());
      setActiveRun(run);
      const request = pollRequestRef.current;
      void pollRun(current.Id, run.Id, request);
    } catch (error) {
      setRunLoading(false);
      message.error(getOrchestrationErrorMessage(error, "编排启动失败"));
    }
  };

  const cancelRun = async () => {
    if (!current || !activeRun || activeRun.Status !== "Running" || cancelLoading) return;
    setCancelLoading(true);
    try {
      await cancelOrchestrationRun(current.Id, activeRun.Id);
      clearPoll();
      const request = pollRequestRef.current;
      setRunLoading(true);
      void pollRun(current.Id, activeRun.Id, request);
    } catch (error) {
      message.error(getOrchestrationErrorMessage(error, "取消运行失败"));
    } finally {
      setCancelLoading(false);
    }
  };

  const resumePoll = () => {
    if (!current || !activeRun) return;
    clearPoll();
    setPollStopped(false);
    setRunLoading(true);
    const request = pollRequestRef.current;
    void pollRun(current.Id, activeRun.Id, request);
  };

  const archived = current?.Status === "Archived";
  const workspaceVisible = creating || current;
  const canAdd = moduleActions.has("Add");
  const canUpdate = moduleActions.has("Update");
  const canModify = creating ? canAdd : canUpdate;
  const canRun = Boolean(canUpdate && current && current.Status === "Enabled" && current.PublishedVersions.length);
  const activeRunMeta = activeRun ? runStatusMeta[activeRun.Status] : null;

  return (
    <div className="orchestration-page">
      <header className="orchestration-page__header">
        <Flex justify="space-between" align="center" gap={16} wrap>
          <div>
            <Typography.Title level={3}>编排控制台</Typography.Title>
            <Typography.Text type="secondary">维护有向无环流程、冻结 Agent 版本并检查节点运行状态</Typography.Text>
          </div>
          {canAdd && <Button type="primary" icon={<PlusOutlined />} onClick={beginCreate}>创建编排</Button>}
        </Flex>
      </header>

      <Alert
        className="orchestration-page__alert"
        type="info"
        showIcon
        message="编排仅支持手动触发；每个节点最多重试 3 次，运行输入、输出、Attempt 和 MCP 调用明细由服务端持久化并脱敏。"
      />

      <div className="orchestration-page__layout">
        <aside className="orchestration-page__catalog">
          <Flex className="orchestration-page__catalog-heading" justify="space-between" align="center" gap={12}>
            <div>
              <Typography.Text strong>编排列表</Typography.Text>
              <Typography.Text type="secondary">共 {items.length} 个</Typography.Text>
            </div>
            <label className="orchestration-page__status-filter">
              <span><FilterOutlined /> 状态</span>
              <Select
                aria-label="编排状态筛选"
                value={status || ""}
                options={[
                  { label: "未归档", value: "" },
                  { label: "已启用", value: "Enabled" },
                  { label: "已停用", value: "Disabled" },
                  { label: "已归档", value: "Archived" }
                ]}
                onChange={value => {
                  const nextStatus = (value || undefined) as OrchestrationStatus | undefined;
                  statusRef.current = nextStatus;
                  setStatus(nextStatus);
                }}
              />
            </label>
          </Flex>
          <Spin spinning={listLoading}>
            <List
              dataSource={items}
              locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无编排" /> }}
              renderItem={item => {
                const meta = statusMeta[item.Status];
                return (
                  <List.Item
                    className={item.Id === current?.Id && !creating ? "orchestration-page__item--active" : undefined}
                    onClick={() => selectDefinition(item.Id)}
                  >
                    <div className="orchestration-page__identity">
                      <Flex justify="space-between" gap={8} align="center">
                        <Typography.Text strong ellipsis>{item.Name || item.Code}</Typography.Text>
                        <Tag color={meta.color}>{meta.text}</Tag>
                      </Flex>
                      <Typography.Text code>{item.Code}</Typography.Text>
                      <Typography.Text type="secondary" ellipsis>{item.Description || "尚未填写说明"}</Typography.Text>
                      <Typography.Text type="secondary">
                        {item.DraftNodeCount} 个节点 · {item.CurrentPublishedLabel ? `v${item.CurrentPublishedLabel}` : "仅 Draft"}
                      </Typography.Text>
                    </div>
                  </List.Item>
                );
              }}
            />
          </Spin>
        </aside>

        <main className="orchestration-page__workspace">
          {!workspaceVisible ? (
            <Empty description="选择一个编排进行管理，或创建新编排" />
          ) : (
            <Spin spinning={detailLoading}>
              <Flex className="orchestration-page__workspace-heading" justify="space-between" align="center" gap={12} wrap>
                <div>
                  <Typography.Title level={4}>{creating ? "创建编排" : current?.Name || current?.Code}</Typography.Title>
                  <Typography.Text type="secondary">
                    {creating ? "先添加节点，再保存 Draft" : `DRAFT ${current?.Draft.Label} · REV ${current?.LogicalRevision}`}
                  </Typography.Text>
                </div>
                {current && <Tag color={statusMeta[current.Status].color}>{statusMeta[current.Status].text}</Tag>}
              </Flex>

              <Form<OrchestrationFormValue>
                form={form}
                layout="vertical"
                initialValues={emptyForm()}
                disabled={archived || !canModify}
                onValuesChange={() => setDirty(true)}
              >
                <Flex gap={12} wrap>
                  <Form.Item
                    className="orchestration-page__field"
                    name="code"
                    label="Code"
                    rules={[
                      { required: true, message: "请输入 Code" },
                      { pattern: /^[a-z0-9-]+$/, message: "仅允许小写字母、数字和短横线" }
                    ]}
                  >
                    <Input disabled={!creating || archived} maxLength={80} placeholder="supplier-review" />
                  </Form.Item>
                  <Form.Item className="orchestration-page__field" name="name" label="名称" rules={[{ required: true, message: "请输入名称" }]}>
                    <Input maxLength={120} />
                  </Form.Item>
                  <Form.Item className="orchestration-page__status" name="status" label="状态">
                    <Select disabled options={Object.entries(statusMeta).map(([value, meta]) => ({ value, label: meta.text }))} />
                  </Form.Item>
                </Flex>
                <Form.Item name="description" label="说明">
                  <Input.TextArea autoSize={{ minRows: 2, maxRows: 5 }} maxLength={1000} showCount />
                </Form.Item>

                <section className="orchestration-page__section">
                  <Flex justify="space-between" align="center" gap={12} wrap>
                    <div className="orchestration-page__section-title"><ApartmentOutlined /><span>Agent 节点</span></div>
                    <Typography.Text type="secondary">首个节点为流程入口；模板支持 {"{{input}}"} 和 {"{{previous}}"}</Typography.Text>
                  </Flex>
                  <Form.List name="nodes">
                    {(fields, { add, remove }) => (
                      <div className="orchestration-page__node-list">
                        {fields.map((field, index) => (
                          <article key={field.key} className="orchestration-page__node">
                            <Flex justify="space-between" align="center">
                              <Typography.Text strong>节点 {index + 1}{index === 0 ? " · 入口" : ""}</Typography.Text>
                              <Button danger type="text" icon={<DeleteOutlined />} disabled={archived || !canModify || fields.length === 1} onClick={() => remove(field.name)}>移除</Button>
                            </Flex>
                            <div className="orchestration-page__node-grid">
                              <Form.Item name={[field.name, "id"]} label="节点 ID" rules={[{ required: true, message: "请输入节点 ID" }]}>
                                <Input placeholder="node-1" />
                              </Form.Item>
                              <Form.Item name={[field.name, "name"]} label="节点名称" rules={[{ required: true, message: "请输入节点名称" }]}>
                                <Input />
                              </Form.Item>
                              <Form.Item name={[field.name, "agentId"]} label="已发布 Agent" rules={[{ required: true, message: "请选择 Agent" }]}>
                                <Select showSearch optionFilterProp="label" options={agentOptions} placeholder="选择已启用且已发布的 Agent" />
                              </Form.Item>
                              <Form.Item name={[field.name, "inputMode"]} label="输入模式">
                                <Select options={[
                                  { value: "InitialInput", label: "初始输入" },
                                  { value: "PreviousOutput", label: "上一节点输出" },
                                  { value: "Template", label: "模板" }
                                ]} />
                              </Form.Item>
                              <Form.Item name={[field.name, "inputTemplate"]} label="输入模板">
                                <Input placeholder="{{previous}}" />
                              </Form.Item>
                              <Form.Item name={[field.name, "maximumRetries"]} label="重试次数" rules={[{ required: true }]}>
                                <InputNumber min={0} max={3} precision={0} />
                              </Form.Item>
                              <Form.Item name={[field.name, "timeoutSeconds"]} label="超时（秒）" rules={[{ required: true }]}>
                                <InputNumber min={5} max={600} precision={0} />
                              </Form.Item>
                            </div>
                          </article>
                        ))}
                        <Button block type="dashed" icon={<PlusOutlined />} disabled={archived || !canModify} onClick={() => add(newNode(fields.length))}>添加 Agent 节点</Button>
                      </div>
                    )}
                  </Form.List>
                </section>

                <section className="orchestration-page__section">
                  <Flex justify="space-between" align="center" gap={12} wrap>
                    <div className="orchestration-page__section-title">连接与条件</div>
                    <Typography.Text type="secondary">按 Order 顺序匹配第一条满足条件的边</Typography.Text>
                  </Flex>
                  <Form.List name="edges">
                    {(fields, { add, remove }) => (
                      <div className="orchestration-page__edge-list">
                        {fields.map(field => (
                          <div key={field.key} className="orchestration-page__edge">
                            <Form.Item name={[field.name, "fromNodeId"]} rules={[{ required: true, message: "请输入起点" }]}><Input placeholder="起点 ID" /></Form.Item>
                            <span>→</span>
                            <Form.Item name={[field.name, "toNodeId"]} rules={[{ required: true, message: "请输入终点" }]}><Input placeholder="终点 ID" /></Form.Item>
                            <Form.Item name={[field.name, "condition"]}>
                              <Select options={[
                                { value: "Always", label: "始终" },
                                { value: "Succeeded", label: "成功" },
                                { value: "Failed", label: "失败" },
                                { value: "OutputContains", label: "输出包含" }
                              ]} />
                            </Form.Item>
                            <Form.Item name={[field.name, "conditionValue"]}><Input placeholder="条件值" /></Form.Item>
                            <Form.Item name={[field.name, "order"]}><InputNumber min={0} precision={0} placeholder="Order" /></Form.Item>
                            <Button danger type="text" icon={<DeleteOutlined />} disabled={archived || !canModify} onClick={() => remove(field.name)} />
                          </div>
                        ))}
                        <Button
                          block
                          type="dashed"
                          icon={<PlusOutlined />}
                          disabled={archived || !canModify}
                          onClick={() => add({ fromNodeId: "", toNodeId: "", condition: "Succeeded", conditionValue: "", order: fields.length })}
                        >
                          添加连接
                        </Button>
                      </div>
                    )}
                  </Form.List>
                </section>
              </Form>

              <Flex className="orchestration-page__actions" gap={10} wrap>
                {!archived && canModify && <Button icon={<SaveOutlined />} loading={saving} disabled={publishing} onClick={() => void save()}>保存 Draft</Button>}
                {!creating && canUpdate && !archived && <Button type="primary" icon={<RocketOutlined />} loading={publishing} disabled={saving} onClick={() => void publish()}>发布版本</Button>}
                {canUpdate && current && !archived && <Button loading={saving} onClick={() => void toggleStatus()}>{current.Status === "Enabled" ? "停用" : "启用"}</Button>}
                {canUpdate && current && <Button icon={<InboxOutlined />} loading={transitioning} onClick={toggleArchived}>{archived ? "恢复" : "归档"}</Button>}
                {dirty && <Typography.Text type="warning">存在未保存修改</Typography.Text>}
              </Flex>

              {current && current.PublishedVersions.length > 0 && (
                <section className="orchestration-page__section orchestration-page__runner">
                  <Flex justify="space-between" align="center" gap={12} wrap>
                    <div>
                      <div className="orchestration-page__section-title"><PlayCircleOutlined /><span>运行编排</span></div>
                      <Typography.Text type="secondary">使用最新发布版本，运行期间每 800 ms 更新节点状态</Typography.Text>
                    </div>
                    {activeRunMeta && <Tag color={activeRunMeta.color}>{activeRunMeta.text}</Tag>}
                  </Flex>
                  <Input.TextArea
                    value={runInput}
                    rows={3}
                    maxLength={32768}
                    disabled={!canRun || activeRun?.Status === "Running"}
                    placeholder="输入本次编排任务"
                    onChange={event => setRunInput(event.target.value)}
                  />
                  <Space>
                    {canUpdate && pollStopped && <Button onClick={resumePoll}>重新读取状态</Button>}
                    {canUpdate && activeRun?.Status === "Running" && <Button danger icon={<StopOutlined />} loading={cancelLoading} onClick={() => void cancelRun()}>取消运行</Button>}
                    <Button type="primary" icon={<PlayCircleOutlined />} loading={runLoading} disabled={!canRun || !runInput.trim() || activeRun?.Status === "Running"} onClick={() => void startRun()}>开始运行</Button>
                  </Space>

                  {activeRun ? (
                    <div className="orchestration-page__trace">
                      <Flex gap={8} align="center" wrap>
                        <Tag color={activeRunMeta?.color}>{activeRunMeta?.text}</Tag>
                        <Typography.Text code>Run {activeRun.Id.slice(0, 8)}</Typography.Text>
                        {activeRun.ErrorCode && <Typography.Text type="danger">{activeRun.ErrorCode}</Typography.Text>}
                      </Flex>
                      <Timeline
                        items={activeRun.Nodes.map(node => ({
                          color: node.Status === "Completed" ? "green" : node.Status === "Failed" ? "red" : node.Status === "Running" ? "blue" : "gray",
                          children: (
                            <div>
                              <Typography.Text strong>{node.NodeName || node.NodeId}</Typography.Text>
                              <br />
                              <Typography.Text type="secondary">
                                {node.Status} · {node.Attempts} attempt(s) · {node.OutputCharacters} chars{node.ErrorCode ? ` · ${node.ErrorCode}` : ""}
                              </Typography.Text>
                            </div>
                          )
                        }))}
                      />
                      {runDetails && (
                        <>
                          <Payload label="编排输入" value={runDetails.Input} />
                          <Collapse
                            items={activeRun.Nodes.map(node => {
                              const attempts = runDetails.Attempts.filter(attempt => attempt.NodeId === node.NodeId);
                              return {
                                key: node.NodeId,
                                label: `${node.NodeName || node.NodeId} · ${attempts.length} attempt(s)`,
                                children: attempts.length ? attempts.map(attempt => (
                                  <article key={`${attempt.NodeId}-${attempt.Attempt}`} className="orchestration-page__attempt">
                                    <Flex justify="space-between"><Typography.Text strong>Attempt {attempt.Attempt}</Typography.Text><Tag>{attempt.Status}</Tag></Flex>
                                    <Payload label="节点输入" value={attempt.Input} />
                                    <Payload label="Agent 输出" value={attempt.Output} />
                                    {attempt.ToolCalls.length ? attempt.ToolCalls.map(tool => (
                                      <div key={tool.ToolCallId} className="orchestration-page__tool-call">
                                        <Typography.Text strong>{tool.ToolName || "MCP tool"}</Typography.Text>
                                        <Payload label="调用参数" value={tool.ArgumentsJson} />
                                        <Payload label="原始返回值" value={tool.ResultContent} />
                                      </div>
                                    )) : <Typography.Text type="secondary">本次节点未调用 MCP 工具</Typography.Text>}
                                  </article>
                                )) : <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无 Attempt 明细" />
                              };
                            })}
                          />
                          <Payload label="最终输出" value={runDetails.Output} />
                        </>
                      )}
                    </div>
                  ) : <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="尚未运行" />}
                </section>
              )}
            </Spin>
          )}
        </main>
      </div>
    </div>
  );
};

export default OrchestrationPage;
