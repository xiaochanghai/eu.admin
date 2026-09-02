import {
  CheckCircleOutlined,
  ExperimentOutlined,
  InboxOutlined,
  PlayCircleOutlined,
  PlusOutlined,
  ReloadOutlined,
  SaveOutlined,
  SendOutlined
} from "@ant-design/icons";
import {
  Alert,
  Button,
  Card,
  Collapse,
  Descriptions,
  Empty,
  Flex,
  Form,
  Input,
  InputNumber,
  List,
  Popconfirm,
  Select,
  Space,
  Spin,
  Switch,
  Table,
  Tag,
  Typography,
  type TableColumnsType
} from "antd";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  compareEvaluationBatches,
  createEvaluationSuite,
  getEvaluationSuite,
  listModelJudgeReports,
  listEvaluationBatches,
  listEvaluationSuites,
  publishEvaluationSuite,
  runEvaluationBatch,
  runModelJudge,
  saveEvaluationDraft,
  setEvaluationArchived,
  type EvaluationBatch,
  type EvaluationCase,
  type EvaluationComparison,
  type EvaluationQualityGate,
  type EvaluationSuite,
  type EvaluationSuiteStatus
} from "@/api/modules/agentEvaluation";
import { getAgent, getAgentCapabilities, listAgents, type AgentDefinition, type AgentListItem } from "@/api/modules/agent";
import { getModuleInfo } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import "./index.less";

const MODULE_CODE = "AG_EVALUATION_MNG";
const MAX_CASES = 100;

interface SuiteFormValues {
  code: string;
  name: string;
  description: string;
  cases: EvaluationCase[];
}

interface CompareFormValues extends EvaluationQualityGate {
  baselineBatchId?: string;
  candidateBatchId?: string;
}

interface JudgeFormValues {
  explicitlyEnabled: boolean;
  modelProfileId?: string;
  relevance: boolean;
  relevanceMinimum: number;
  coherence: boolean;
  coherenceMinimum: number;
}

const statusMeta: Record<EvaluationSuiteStatus, { color: string; text: string }> = {
  Active: { color: "success", text: "启用" },
  Archived: { color: "warning", text: "已归档" }
};

const batchColor: Record<EvaluationBatch["Status"], string> = {
  Running: "processing",
  Completed: "success",
  Cancelled: "default",
  Failed: "error"
};

const newCase = (): EvaluationCase => ({
  Id: crypto.randomUUID(),
  Name: "",
  Input: "",
  TargetAgentId: "",
  TargetAgentVersionId: "",
  Specification: {
    ExpectedStatus: "Completed",
    OutputContains: [],
    OutputExcludes: [],
    RequiredEventKinds: [],
    MaximumToolCalls: null,
    MaximumDurationMilliseconds: null
  }
});

const parseRules = (value: string) => value.split("\n").map(item => item.trim()).filter(Boolean);
const rulesText = (value?: string[]) => (value || []).join("\n");
const errorMessage = (error: unknown, fallback: string) => error instanceof Error && error.message ? error.message : fallback;

const EvaluationPage = () => {
  const [form] = Form.useForm<SuiteFormValues>();
  const [compareForm] = Form.useForm<CompareFormValues>();
  const [judgeForm] = Form.useForm<JudgeFormValues>();
  const [moduleActions, setModuleActions] = useState<Set<string>>(() => new Set());
  const [statusFilter, setStatusFilter] = useState<EvaluationSuiteStatus>();
  const [items, setItems] = useState<EvaluationSuite[]>([]);
  const [current, setCurrent] = useState<EvaluationSuite | null>(null);
  const [creating, setCreating] = useState(false);
  const [batches, setBatches] = useState<EvaluationBatch[]>([]);
  const [agents, setAgents] = useState<AgentListItem[]>([]);
  const [agentDetails, setAgentDetails] = useState<Record<string, AgentDefinition>>({});
  const [comparison, setComparison] = useState<EvaluationComparison | null>(null);
  const [selectedBatchId, setSelectedBatchId] = useState<string>();
  const [modelJudgeEnabled, setModelJudgeEnabled] = useState(false);
  const [modelProfileIds, setModelProfileIds] = useState<string[]>([]);
  const [judgeReports, setJudgeReports] = useState<Awaited<ReturnType<typeof listModelJudgeReports>>>([]);
  const [judgeLoading, setJudgeLoading] = useState(false);
  const [listLoading, setListLoading] = useState(false);
  const [contentLoading, setContentLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [runningVersionId, setRunningVersionId] = useState<string>();
  const [error, setError] = useState("");
  const openSequence = useRef(0);

  const archived = current?.Status === "Archived";
  const canAdd = moduleActions.has("Add");
  const canUpdate = moduleActions.has("Update");

  const applySuite = useCallback((suite: EvaluationSuite) => {
    setCurrent(suite);
    setCreating(false);
    form.setFieldsValue({
      code: suite.Code,
      name: suite.Name,
      description: suite.Description,
      cases: suite.Draft.Cases.map(item => ({
        ...item,
        Specification: {
          ...item.Specification,
          OutputContains: item.Specification.OutputContains || [],
          OutputExcludes: item.Specification.OutputExcludes || [],
          RequiredEventKinds: item.Specification.RequiredEventKinds || []
        }
      }))
    });
  }, [form]);

  const loadList = useCallback(async () => {
    setListLoading(true);
    try {
      setItems(await listEvaluationSuites(statusFilter));
    } catch (loadError) {
      setError(errorMessage(loadError, "评测 Suite 加载失败"));
    } finally {
      setListLoading(false);
    }
  }, [statusFilter]);

  const loadBatches = useCallback(async (suiteId: string) => {
    try {
      setBatches(await listEvaluationBatches(suiteId));
    } catch (loadError) {
      setError(errorMessage(loadError, "评测批次加载失败"));
    }
  }, []);

  const loadAgentDetail = useCallback(async (agentId: string) => {
    if (!agentId || agentDetails[agentId]) return;
    try {
      const agent = await getAgent(agentId);
      setAgentDetails(previous => ({ ...previous, [agentId]: agent }));
    } catch (loadError) {
      setError(errorMessage(loadError, "Agent 发布版本加载失败"));
    }
  }, [agentDetails]);

  useEffect(() => {
    let active = true;
    const initialize = async () => {
      try {
        const [module, enabledAgents, capabilities] = await Promise.all([getModuleInfo(MODULE_CODE), listAgents("Enabled"), getAgentCapabilities()]);
        if (!active) return;
        setModuleActions(new Set(module.Data.actions || []));
        setAgents(enabledAgents);
        setModelJudgeEnabled(Boolean(capabilities.Features?.ModelJudge));
        setModelProfileIds(capabilities.ModelProfileIds || []);
        judgeForm.setFieldsValue({ modelProfileId: capabilities.ModelProfileIds?.[0] });
      } catch (loadError) {
        if (active) setError(errorMessage(loadError, "评测中心初始化失败"));
      }
    };
    void initialize();
    return () => { active = false; };
  }, [judgeForm]);

  useEffect(() => { void loadList(); }, [loadList]);

  const openSuite = useCallback(async (id: string) => {
    const sequence = ++openSequence.current;
    setContentLoading(true);
    setError("");
    setComparison(null);
    setSelectedBatchId(undefined);
    setJudgeReports([]);
    try {
      const [suite, loadedBatches] = await Promise.all([getEvaluationSuite(id), listEvaluationBatches(id)]);
      if (sequence !== openSequence.current) return;
      applySuite(suite);
      setBatches(loadedBatches);
      await Promise.all(suite.Draft.Cases.map(item => loadAgentDetail(item.TargetAgentId)));
    } catch (loadError) {
      if (sequence === openSequence.current) setError(errorMessage(loadError, "评测 Suite 加载失败"));
    } finally {
      if (sequence === openSequence.current) setContentLoading(false);
    }
  }, [applySuite, loadAgentDetail]);

  const startCreate = () => {
    openSequence.current += 1;
    setCurrent(null);
    setCreating(true);
    setBatches([]);
    setComparison(null);
    form.resetFields();
    form.setFieldsValue({ code: "", name: "", description: "", cases: [] });
  };

  const save = async () => {
    try {
      const values = await form.validateFields();
      const cases = (values.cases || []).map(item => ({
        ...item,
        Specification: {
          ...item.Specification,
          OutputContains: item.Specification.OutputContains || [],
          OutputExcludes: item.Specification.OutputExcludes || [],
          RequiredEventKinds: item.Specification.RequiredEventKinds || []
        }
      }));
      setSaving(true);
      setError("");
      const suite = current
        ? await saveEvaluationDraft(current.Id, { expectedLogicalRevision: current.LogicalRevision, name: values.name, description: values.description || "", cases })
        : await createEvaluationSuite({ code: values.code, name: values.name, description: values.description || "" });
      applySuite(suite);
      await loadList();
      message.success(current ? "评测草稿已保存" : "评测 Suite 已创建；可继续添加 Case");
    } catch (saveError) {
      setError(errorMessage(saveError, "评测草稿保存失败"));
    } finally {
      setSaving(false);
    }
  };

  const publish = async () => {
    if (!current) return;
    setSaving(true);
    try {
      const suite = await publishEvaluationSuite(current.Id, current.LogicalRevision);
      applySuite(suite);
      await loadList();
      message.success("评测 Suite 已发布不可变版本");
    } catch (publishError) {
      setError(errorMessage(publishError, "评测 Suite 发布失败"));
    } finally { setSaving(false); }
  };

  const toggleArchived = async () => {
    if (!current) return;
    setSaving(true);
    try {
      const suite = await setEvaluationArchived(current.Id, current.LogicalRevision, !archived);
      applySuite(suite);
      await loadList();
      message.success(archived ? "评测 Suite 已恢复" : "评测 Suite 已归档");
    } catch (archiveError) {
      setError(errorMessage(archiveError, "评测 Suite 状态更新失败"));
    } finally { setSaving(false); }
  };

  const runVersion = async (versionId: string) => {
    if (!current) return;
    setRunningVersionId(versionId);
    try {
      await runEvaluationBatch(current.Id, versionId);
      await loadBatches(current.Id);
      message.success("评测批次已启动，刷新结果可查看进度");
    } catch (runError) {
      setError(errorMessage(runError, "评测批次启动失败"));
    } finally { setRunningVersionId(undefined); }
  };

  const compare = async () => {
    try {
      const values = await compareForm.validateFields();
      if (!values.baselineBatchId || !values.candidateBatchId) return;
      setSaving(true);
      setComparison(await compareEvaluationBatches(values.baselineBatchId, values.candidateBatchId, {
        minimumCandidatePassRate: values.minimumCandidatePassRate,
        maximumPassRateRegression: values.maximumPassRateRegression,
        maximumAverageDurationRegressionPercent: values.maximumAverageDurationRegressionPercent,
        maximumToolCallIncreasePerCase: values.maximumToolCallIncreasePerCase,
        requireNoNewFailures: values.requireNoNewFailures,
        requireSameCaseSet: values.requireSameCaseSet,
        requireStableRoutes: values.requireStableRoutes
      }));
    } catch (compareError) {
      setError(errorMessage(compareError, "质量门禁比对失败"));
    } finally { setSaving(false); }
  };

  const selectedBatch = batches.find(item => item.Id === selectedBatchId);

  const loadJudgeReports = useCallback(async (batchId: string) => {
    setJudgeLoading(true);
    try {
      setJudgeReports(await listModelJudgeReports(batchId));
    } catch (loadError) {
      setError(errorMessage(loadError, "模型裁判报告加载失败"));
    } finally {
      setJudgeLoading(false);
    }
  }, []);

  const selectBatch = (batch: EvaluationBatch) => {
    setSelectedBatchId(batch.Id);
    setJudgeReports([]);
    if (batch.Status === "Completed") void loadJudgeReports(batch.Id);
  };

  const runJudge = async () => {
    if (!selectedBatch || !current) return;
    try {
      const values = await judgeForm.validateFields();
      const evaluators = [values.relevance && "Relevance", values.coherence && "Coherence"].filter(Boolean) as string[];
      if (!values.explicitlyEnabled || evaluators.length === 0 || !values.modelProfileId) {
        message.warning("请明确确认执行模型裁判，并至少选择一项指标和模型配置");
        return;
      }
      setJudgeLoading(true);
      const report = await runModelJudge(selectedBatch.Id, {
        explicitlyEnabled: true,
        modelProfileId: values.modelProfileId,
        evaluators,
        minimumScores: {
          ...(values.relevance ? { Relevance: values.relevanceMinimum } : {}),
          ...(values.coherence ? { Coherence: values.coherenceMinimum } : {})
        }
      });
      setJudgeReports(previous => [report, ...previous.filter(item => item.Id !== report.Id)]);
      message.success(report.AdvisoryPassed ? "模型裁判 advisory 通过" : "模型裁判 advisory 未通过");
    } catch (judgeError) {
      setError(errorMessage(judgeError, "模型裁判执行失败"));
    } finally {
      setJudgeLoading(false);
    }
  };

  const batchColumns = useMemo<TableColumnsType<EvaluationBatch>>(() => [
    { title: "批次", dataIndex: "Id", render: value => <Typography.Text code>{String(value).slice(0, 8)}</Typography.Text> },
    { title: "版本", dataIndex: "SuiteVersionId", render: value => <Typography.Text code>{String(value).slice(0, 8)}</Typography.Text> },
    { title: "状态", dataIndex: "Status", render: value => <Tag color={batchColor[value as EvaluationBatch["Status"]]}>{value}</Tag> },
    { title: "用例", dataIndex: "Cases", render: value => Array.isArray(value) ? value.length : 0 },
    { title: "开始时间", dataIndex: "StartedAtUtc", render: value => new Date(value).toLocaleString() }
  ], []);
  const completedBatches = batches.filter(item => item.Status === "Completed");

  return <div className="evaluation-page">
    <Flex justify="space-between" align="center" wrap gap={16} className="evaluation-page__header">
      <div><Typography.Title level={3}>评测中心</Typography.Title><Typography.Text type="secondary">维护回归 Case、发布不可变版本、运行批次并用质量门禁比较结果。</Typography.Text></div>
      <Space wrap>
        <Select<EvaluationSuiteStatus | undefined> allowClear placeholder="全部状态" value={statusFilter} options={Object.entries(statusMeta).map(([value, meta]) => ({ value, label: meta.text }))} onChange={setStatusFilter} className="evaluation-page__status-filter" />
        <Button icon={<ReloadOutlined />} loading={listLoading} onClick={() => void loadList()}>刷新</Button>
        {canAdd && <Button type="primary" icon={<PlusOutlined />} onClick={startCreate}>新建 Suite</Button>}
      </Space>
    </Flex>
    {error && <Alert className="evaluation-page__alert" type="error" showIcon closable message={error} onClose={() => setError("")} />}
    <div className="evaluation-page__layout">
      <aside className="evaluation-page__catalog">
        <div className="evaluation-page__section-title"><ExperimentOutlined /> 评测 Suite <Tag>{items.length}</Tag></div>
        <List loading={listLoading} dataSource={items} locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无评测 Suite" /> }} renderItem={item => <List.Item className={item.Id === current?.Id ? "evaluation-page__suite--active" : undefined} onClick={() => void openSuite(item.Id)}><div className="evaluation-page__suite-summary"><Typography.Text strong>{item.Name || item.Code}</Typography.Text><Typography.Text type="secondary" code>{item.Code}</Typography.Text><Tag color={statusMeta[item.Status].color}>{statusMeta[item.Status].text}</Tag></div></List.Item>} />
      </aside>
      <main className="evaluation-page__workspace">
        {!current && !creating ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="选择一个评测 Suite，或新建 Suite 开始维护" /> : <Spin spinning={contentLoading || saving}>
          <Flex justify="space-between" align="center" wrap gap={12} className="evaluation-page__workspace-heading">
            <div><Typography.Title level={4}>{current?.Name || "新建评测 Suite"}</Typography.Title>{current && <Typography.Text type="secondary">REV {current.LogicalRevision} · 已发布 {current.PublishedVersions.length} 个版本</Typography.Text>}</div>
            {current && <Space wrap><Tag color={statusMeta[current.Status].color}>{statusMeta[current.Status].text}</Tag>{canUpdate && !archived && <Button icon={<SendOutlined />} onClick={() => void publish()}>发布版本</Button>}{canUpdate && <Popconfirm title={archived ? "恢复此评测 Suite？" : "归档此评测 Suite？"} onConfirm={() => void toggleArchived()}><Button danger={!archived} icon={<InboxOutlined />}>{archived ? "恢复" : "归档"}</Button></Popconfirm>}</Space>}
          </Flex>
          {archived && <Alert type="warning" showIcon message="此评测 Suite 已归档；可查看历史或恢复，不能继续编辑、发布和运行。" />}
          <section className="evaluation-page__section">
            <div className="evaluation-page__section-title">Suite 草稿</div>
            <Form<SuiteFormValues> form={form} layout="vertical" disabled={saving || (current ? archived || !canUpdate : !canAdd)}>
              <Flex wrap gap={16}><Form.Item className="evaluation-page__half" name="code" label="Suite Code" rules={[{ required: true }, { pattern: /^[a-z0-9]+(?:-[a-z0-9]+)*$/, message: "请输入小写 kebab-case" }]}><Input maxLength={128} disabled={Boolean(current)} /></Form.Item><Form.Item className="evaluation-page__half" name="name" label="名称" rules={[{ required: true }]}><Input maxLength={256} /></Form.Item></Flex>
              <Form.Item name="description" label="说明"><Input.TextArea autoSize={{ minRows: 2, maxRows: 4 }} maxLength={1000} showCount /></Form.Item>
              {current && <Form.List name="cases">{(fields, { add, remove }) => <div className="evaluation-page__case-list">
                <Flex justify="space-between" align="center" wrap gap={12}><div className="evaluation-page__section-title">回归 Case <Tag>{fields.length} / {MAX_CASES}</Tag></div>{!archived && canUpdate && <Button icon={<PlusOutlined />} disabled={fields.length >= MAX_CASES} onClick={() => add(newCase())}>添加 Case</Button>}</Flex>
                {fields.length === 0 && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="请添加回归 Case" />}
                {fields.map((field, index) => <Card key={field.key} size="small" title={`Case ${index + 1}`} extra={!archived && canUpdate && <Button type="link" danger onClick={() => remove(field.name)}>移除</Button>} className="evaluation-page__case-card">
                  <Form.Item name={[field.name, "Id"]} hidden><Input /></Form.Item>
                  <Flex wrap gap={16}><Form.Item className="evaluation-page__half" name={[field.name, "Name"]} label="Case 名称" rules={[{ required: true }]}><Input maxLength={256} /></Form.Item><Form.Item className="evaluation-page__half" name={[field.name, "TargetAgentId"]} label="目标 Agent" rules={[{ required: true }]}><Select showSearch optionFilterProp="label" options={agents.map(agent => ({ value: agent.Id, label: `${agent.Name} (${agent.Code})` }))} onChange={value => { void loadAgentDetail(value); form.setFieldValue(["cases", field.name, "TargetAgentVersionId"], undefined); }} /></Form.Item></Flex>
                  <Form.Item noStyle shouldUpdate={(previous, next) => previous.cases?.[field.name]?.TargetAgentId !== next.cases?.[field.name]?.TargetAgentId}>{({ getFieldValue }) => { const targetId = getFieldValue(["cases", field.name, "TargetAgentId"]); const versions = agentDetails[targetId]?.PublishedVersions || []; return <Form.Item name={[field.name, "TargetAgentVersionId"]} label="已发布 Agent 版本" rules={[{ required: true }]}><Select disabled={!targetId} options={versions.map(version => ({ value: version.Id, label: version.Label }))} /></Form.Item>; }}</Form.Item>
                  <Form.Item name={[field.name, "Input"]} label="输入" rules={[{ required: true }]}><Input.TextArea autoSize={{ minRows: 2, maxRows: 5 }} maxLength={10000} /></Form.Item>
                  <Collapse size="small" items={[{ key: "assertions", label: "断言规则", children: <>
                    <Form.Item name={[field.name, "Specification", "ExpectedStatus"]} label="期望运行状态"><Select allowClear options={["Completed", "Failed", "Cancelled"].map(value => ({ value, label: value }))} /></Form.Item>
                    <Flex wrap gap={16}><Form.Item className="evaluation-page__half" name={[field.name, "Specification", "OutputContains"]} label="输出必须包含" getValueProps={value => ({ value: rulesText(value) })} getValueFromEvent={event => parseRules(event.target.value)}><Input.TextArea autoSize={{ minRows: 2, maxRows: 4 }} placeholder="每行一条" /></Form.Item><Form.Item className="evaluation-page__half" name={[field.name, "Specification", "OutputExcludes"]} label="输出不得包含" getValueProps={value => ({ value: rulesText(value) })} getValueFromEvent={event => parseRules(event.target.value)}><Input.TextArea autoSize={{ minRows: 2, maxRows: 4 }} placeholder="每行一条" /></Form.Item></Flex>
                    <Flex wrap gap={16}><Form.Item className="evaluation-page__half" name={[field.name, "Specification", "RequiredEventKinds"]} label="必须事件类型" getValueProps={value => ({ value: rulesText(value) })} getValueFromEvent={event => parseRules(event.target.value)}><Input.TextArea autoSize={{ minRows: 2, maxRows: 4 }} placeholder="每行一条" /></Form.Item><Flex gap={16} className="evaluation-page__half"><Form.Item name={[field.name, "Specification", "MaximumToolCalls"]} label="最多工具调用"><InputNumber min={0} max={1000} /></Form.Item><Form.Item name={[field.name, "Specification", "MaximumDurationMilliseconds"]} label="最长耗时(ms)"><InputNumber min={1} max={3600000} /></Form.Item></Flex></Flex>
                  </> }]} />
                </Card>)}
              </div>}</Form.List>}
              {!archived && (current ? canUpdate : canAdd) && <Button type="primary" icon={<SaveOutlined />} onClick={() => void save()} loading={saving}>{current ? "保存草稿" : "创建 Suite"}</Button>}
            </Form>
          </section>
          {current && <>
            <section className="evaluation-page__section"><div className="evaluation-page__section-title"><CheckCircleOutlined /> 已发布版本与运行</div><List dataSource={current.PublishedVersions} locale={{ emptyText: "尚未发布版本" }} renderItem={version => <List.Item actions={!archived ? [<Button key="run" type="primary" icon={<PlayCircleOutlined />} loading={runningVersionId === version.Id} onClick={() => void runVersion(version.Id)}>运行</Button>] : undefined}><List.Item.Meta title={version.Label} description={`发布于 ${new Date(version.PublishedAtUtc).toLocaleString()} · ${version.Cases.length} 个 Case`} /></List.Item>} /></section>
            <section className="evaluation-page__section">
              <Flex justify="space-between" align="center" wrap gap={12}><div className="evaluation-page__section-title">运行批次</div><Button icon={<ReloadOutlined />} onClick={() => void loadBatches(current.Id)}>刷新结果</Button></Flex>
              <Table rowKey="Id" size="small" columns={batchColumns} dataSource={batches} pagination={false} onRow={batch => ({ onClick: () => selectBatch(batch), className: batch.Id === selectedBatchId ? "evaluation-page__batch--selected" : "" })} expandable={{ expandedRowRender: batch => <List size="small" dataSource={batch.Cases} renderItem={item => <List.Item><Space wrap><Tag color={item.Status === "Passed" ? "success" : item.Status === "Failed" ? "error" : "default"}>{item.Status}</Tag><Typography.Text>{item.CaseName}</Typography.Text><Typography.Text type="secondary">工具 {item.ToolCallCount} · {item.DurationMilliseconds ?? "-"} ms</Typography.Text>{item.ErrorCode && <Typography.Text type="danger">{item.ErrorCode}</Typography.Text>}</Space></List.Item>} /> }} />
              {selectedBatch && <div className="evaluation-page__judge-panel">
                <Flex justify="space-between" align="center" wrap gap={12}><div className="evaluation-page__section-title">模型裁判 · Batch {selectedBatch.Id.slice(0, 8)}</div>{selectedBatch.Status === "Completed" && <Button size="small" icon={<ReloadOutlined />} loading={judgeLoading} onClick={() => void loadJudgeReports(selectedBatch.Id)}>刷新报告</Button>}</Flex>
                {selectedBatch.Status !== "Completed" ? <Alert type="info" showIcon message="只有已完成的批次可以运行模型裁判。" /> : <>
                  <Alert type={modelJudgeEnabled ? "info" : "warning"} showIcon message={modelJudgeEnabled ? "模型裁判为可选 advisory；执行前需要明确确认。" : "当前 Host 未启用模型裁判，仅可查看已保存报告。"} />
                  {modelJudgeEnabled && !archived && <Form<JudgeFormValues> form={judgeForm} layout="vertical" initialValues={{ explicitlyEnabled: false, modelProfileId: modelProfileIds[0], relevance: true, relevanceMinimum: 0.7, coherence: true, coherenceMinimum: 0.7 }} className="evaluation-page__judge-form"><Flex wrap gap={16}><Form.Item name="modelProfileId" label="裁判模型" rules={[{ required: true }]}><Select options={modelProfileIds.map(value => ({ value, label: value }))} /></Form.Item><Form.Item name="explicitlyEnabled" label="执行确认" valuePropName="checked"><Switch checkedChildren="明确执行" unCheckedChildren="未确认" /></Form.Item></Flex><Flex wrap gap={16}><Form.Item name="relevance" label="相关性" valuePropName="checked"><Switch /></Form.Item><Form.Item name="relevanceMinimum" label="相关性最低分"><InputNumber min={0} max={1} step={0.05} /></Form.Item><Form.Item name="coherence" label="连贯性" valuePropName="checked"><Switch /></Form.Item><Form.Item name="coherenceMinimum" label="连贯性最低分"><InputNumber min={0} max={1} step={0.05} /></Form.Item></Flex><Button type="primary" loading={judgeLoading} onClick={() => void runJudge()}>运行模型裁判</Button></Form>}
                  <List className="evaluation-page__judge-reports" loading={judgeLoading} dataSource={judgeReports} locale={{ emptyText: "尚无模型裁判报告" }} renderItem={report => <List.Item><List.Item.Meta title={<Space><Tag color={report.AdvisoryPassed ? "success" : "error"}>{report.AdvisoryPassed ? "advisory 通过" : "advisory 未通过"}</Tag><Typography.Text>{report.ModelProfileId}</Typography.Text></Space>} description={<Collapse size="small" items={[{ key: "metrics", label: `${report.Cases.length} 个 Case · ${new Date(report.FinishedAtUtc).toLocaleString()}`, children: <List size="small" dataSource={report.Cases} renderItem={item => <List.Item><Typography.Text>{item.CaseName}</Typography.Text><Space wrap>{item.Metrics.map(metric => <Tag key={metric.Name} color={metric.Passed ? "success" : "error"}>{metric.Name}: {metric.Score ?? "-"}/{metric.MinimumScore}</Tag>)}</Space></List.Item>} /> }]} />} /></List.Item>} />
                </>}
              </div>}
            </section>
            <section className="evaluation-page__section"><div className="evaluation-page__section-title">质量门禁对比</div><Form<CompareFormValues> form={compareForm} layout="vertical" initialValues={{ minimumCandidatePassRate: 1, maximumPassRateRegression: 0, requireNoNewFailures: true, requireSameCaseSet: true, requireStableRoutes: false }}><Flex wrap gap={16}><Form.Item className="evaluation-page__half" name="baselineBatchId" label="基线批次" rules={[{ required: true }]}><Select options={completedBatches.map(batch => ({ value: batch.Id, label: `${batch.Id.slice(0, 8)} · ${new Date(batch.StartedAtUtc).toLocaleString()}` }))} /></Form.Item><Form.Item className="evaluation-page__half" name="candidateBatchId" label="候选批次" rules={[{ required: true }]}><Select options={completedBatches.map(batch => ({ value: batch.Id, label: `${batch.Id.slice(0, 8)} · ${new Date(batch.StartedAtUtc).toLocaleString()}` }))} /></Form.Item></Flex><Flex wrap gap={16}><Form.Item name="minimumCandidatePassRate" label="候选最低通过率"><InputNumber min={0} max={1} step={0.01} /></Form.Item><Form.Item name="maximumPassRateRegression" label="最大通过率回退"><InputNumber min={0} max={1} step={0.01} /></Form.Item><Form.Item name="maximumAverageDurationRegressionPercent" label="最大耗时回退(%)"><InputNumber min={0} max={10000} /></Form.Item><Form.Item name="maximumToolCallIncreasePerCase" label="每 Case 工具调用增量"><InputNumber min={0} max={1000} /></Form.Item></Flex><Flex wrap gap={20}><Form.Item name="requireNoNewFailures" valuePropName="checked"><Switch checkedChildren="无新增失败" unCheckedChildren="允许新增失败" /></Form.Item><Form.Item name="requireSameCaseSet" valuePropName="checked"><Switch checkedChildren="Case 集一致" unCheckedChildren="允许 Case 变化" /></Form.Item><Form.Item name="requireStableRoutes" valuePropName="checked"><Switch checkedChildren="路由稳定" unCheckedChildren="允许路由变化" /></Form.Item></Flex><Button type="primary" onClick={() => void compare()} disabled={completedBatches.length < 2} loading={saving}>执行质量门禁</Button></Form>{comparison && <Descriptions className="evaluation-page__comparison" bordered size="small" column={1} title={<Tag color={comparison.GatePassed ? "success" : "error"}>{comparison.GatePassed ? "门禁通过" : "门禁未通过"}</Tag>} items={[{ key: "baseline", label: "基线", children: `${comparison.Baseline.PassedCases}/${comparison.Baseline.TotalCases} 通过 (${(comparison.Baseline.PassRate * 100).toFixed(1)}%)` }, { key: "candidate", label: "候选", children: `${comparison.Candidate.PassedCases}/${comparison.Candidate.TotalCases} 通过 (${(comparison.Candidate.PassRate * 100).toFixed(1)}%)` }, { key: "checks", label: "检查", children: <List size="small" dataSource={comparison.GateChecks} renderItem={check => <List.Item><Tag color={check.Passed ? "success" : "error"}>{check.Passed ? "通过" : "失败"}</Tag>{check.Code}: {check.Actual}</List.Item>} /> }]} />}</section>
          </>}
        </Spin>}
      </main>
    </div>
  </div>;
};

export default EvaluationPage;
