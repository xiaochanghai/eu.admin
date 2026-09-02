import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  ExclamationCircleOutlined,
  ReloadOutlined,
  SafetyCertificateOutlined
} from "@ant-design/icons";
import { Button, Card, Descriptions, Empty, Flex, Input, List, Modal, Select, Space, Spin, Tag, Timeline, Typography } from "antd";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  approveToolApproval,
  cancelToolApproval,
  getApprovalErrorMessage,
  getToolApproval,
  listToolApprovals,
  rejectToolApproval,
  resumeToolApproval,
  type ToolApproval,
  type ToolApprovalDetail,
  type ToolApprovalStatus
} from "@/api/modules/agentApproval";
import { getModuleInfo } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import "./index.less";

const MODULE_CODE = "AG_TOOL_APPROVAL_MNG";

const statusMeta: Record<ToolApprovalStatus, { label: string; color: string }> = {
  Pending: { label: "待审批", color: "warning" },
  Approved: { label: "已批准，待恢复", color: "gold" },
  Rejected: { label: "已拒绝", color: "error" },
  Cancelled: { label: "已取消", color: "default" },
  Expired: { label: "已过期", color: "error" },
  Consuming: { label: "执行中", color: "processing" },
  Consumed: { label: "已执行", color: "success" },
  Failed: { label: "执行失败", color: "error" },
  Invalidated: { label: "已失效", color: "error" }
};

const statusOptions = Object.entries(statusMeta).map(([value, item]) => ({ value, label: item.label }));
const formatTime = (value?: string | null) => {
  if (!value || Number.isNaN(Date.parse(value))) return "—";
  return new Intl.DateTimeFormat("zh-CN", { dateStyle: "medium", timeStyle: "medium" }).format(new Date(value));
};
const safeJson = (value: string) => {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return "{}";
  }
};

const ApprovalPage = () => {
  const [moduleActions, setModuleActions] = useState<Set<string>>(() => new Set());
  const [status, setStatus] = useState<ToolApprovalStatus | undefined>("Pending");
  const [items, setItems] = useState<ToolApproval[]>([]);
  const [selectedId, setSelectedId] = useState<string>();
  const [detail, setDetail] = useState<ToolApprovalDetail>();
  const [listLoading, setListLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [action, setAction] = useState<"approve" | "reject" | "cancel">();
  const [reason, setReason] = useState("");
  const [now, setNow] = useState(Date.now());
  const listRevision = useRef(0);
  const detailRevision = useRef(0);

  const canDecide = moduleActions.has("Update");
  const selected = detail?.Approval;
  const remaining = selected?.Status === "Pending" ? Math.max(0, Date.parse(selected.ExpiresAtUtc) - now) : 0;
  const countdown = selected?.Status === "Pending"
    ? remaining > 0
      ? `${Math.floor(remaining / 60000)}:${String(Math.floor(remaining % 60000 / 1000)).padStart(2, "0")} 后过期`
      : "已到期，等待服务端确认"
    : "";

  useEffect(() => {
    let active = true;
    void getModuleInfo(MODULE_CODE)
      .then(({ Data }) => {
        if (active) setModuleActions(new Set(Data.actions || []));
      })
      .catch(error => {
        if (active) message.error(getApprovalErrorMessage(error, "审批模块权限加载失败"));
      });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const loadDetail = useCallback(async (id: string) => {
    const revision = ++detailRevision.current;
    setDetailLoading(true);
    try {
      const value = await getToolApproval(id);
      if (revision === detailRevision.current) setDetail(value);
    } catch (error) {
      if (revision === detailRevision.current) {
        setDetail(undefined);
        message.error(getApprovalErrorMessage(error, "审批详情加载失败"));
      }
    } finally {
      if (revision === detailRevision.current) setDetailLoading(false);
    }
  }, []);

  const loadList = useCallback(async (preferredId?: string) => {
    const revision = ++listRevision.current;
    setListLoading(true);
    try {
      const values = await listToolApprovals(status);
      if (revision !== listRevision.current) return;
      setItems(values);
      const nextId = values.some(item => item.Id === preferredId) ? preferredId : values[0]?.Id;
      setSelectedId(nextId);
      if (nextId) void loadDetail(nextId);
      else setDetail(undefined);
    } catch (error) {
      if (revision === listRevision.current) message.error(getApprovalErrorMessage(error, "审批队列加载失败"));
    } finally {
      if (revision === listRevision.current) setListLoading(false);
    }
  }, [loadDetail, status]);

  useEffect(() => { void loadList(); }, [loadList]);

  const select = (id: string) => {
    setSelectedId(id);
    void loadDetail(id);
  };

  const submitDecision = async () => {
    if (!selected || !action) return;
    setActionLoading(true);
    try {
      if (action === "approve") await approveToolApproval(selected.Id, reason.trim());
      if (action === "reject") await rejectToolApproval(selected.Id, reason.trim());
      if (action === "cancel") await cancelToolApproval(selected.Id, reason.trim());
      message.success(action === "approve" ? "审批已批准，等待恢复原会话。" : "审批决定已保存。");
      setAction(undefined);
      setReason("");
      await loadList(selected.Id);
    } catch (error) {
      message.error(getApprovalErrorMessage(error, "审批操作失败"));
    } finally {
      setActionLoading(false);
    }
  };

  const resume = async () => {
    if (!selected) return;
    setActionLoading(true);
    try {
      const result = await resumeToolApproval(selected.Id);
      message.success(result.Status === "Completed" ? "工具已执行，原会话已完成。" : "原会话已同步终态。");
      await loadList(selected.Id);
    } catch (error) {
      message.error(getApprovalErrorMessage(error, "恢复原会话失败"));
    } finally {
      setActionLoading(false);
    }
  };

  const decisionTitle = useMemo(() => ({ approve: "批准此次调用？", reject: "拒绝此次调用？", cancel: "取消此次申请？" }[action || "approve"]), [action]);

  return <section className="approval-page">
    <header className="approval-page__header">
      <div>
        <Typography.Title level={3}><SafetyCertificateOutlined /> 审批中心</Typography.Title>
        <Typography.Text type="secondary">审核 Mutating 与 HighRisk 工具调用；参数仅显示服务端安全摘要</Typography.Text>
      </div>
      <Space wrap>
        <Select aria-label="审批状态筛选" value={status} allowClear placeholder="全部状态" options={statusOptions} onChange={value => setStatus(value)} />
        <Button icon={<ReloadOutlined />} loading={listLoading} onClick={() => void loadList(selectedId)}>刷新</Button>
      </Space>
    </header>

    <main className="approval-page__layout">
      <Card className="approval-page__queue" title={<Flex justify="space-between"><span>审批队列</span><Typography.Text type="secondary">{items.length} 条</Typography.Text></Flex>}>
        <Spin spinning={listLoading}>
          <List
            locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="当前筛选条件下没有审批" /> }}
            dataSource={items}
            renderItem={item => <List.Item className={`approval-page__queue-item${item.Id === selectedId ? " is-selected" : ""}`} onClick={() => select(item.Id)}>
              <div className="approval-page__queue-title"><Typography.Text strong ellipsis>{item.ToolName || "未命名工具"}</Typography.Text><Tag color={item.Risk === "HighRisk" ? "error" : "warning"}>{item.Risk === "HighRisk" ? "高风险" : "变更操作"}</Tag></div>
              <Tag bordered={false} color={statusMeta[item.Status].color}>{statusMeta[item.Status].label}</Tag>
              <Typography.Text type="secondary" ellipsis>{item.RequesterUserId} · {formatTime(item.RequestedAtUtc)}</Typography.Text>
            </List.Item>}
          />
        </Spin>
      </Card>

      <Card className="approval-page__detail">
        <Spin spinning={detailLoading}>
          {!selected ? <Empty description="选择一条审批以查看安全摘要与决定记录" /> : <>
            <Flex justify="space-between" gap={16} wrap>
              <div><Typography.Text type="secondary">{selected.Risk === "HighRisk" ? "HIGH RISK" : "MUTATING TOOL"}</Typography.Text><Typography.Title level={4}>{selected.ToolName || "未命名工具"}</Typography.Title></div>
              <Space direction="vertical" align="end" size={2}><Tag color={statusMeta[selected.Status].color}>{statusMeta[selected.Status].label}</Tag>{countdown && <Typography.Text type="warning">{countdown}</Typography.Text>}</Space>
            </Flex>
            <div className={`approval-page__warning${selected.Risk === "HighRisk" ? " is-high-risk" : ""}`}><ExclamationCircleOutlined /> {selected.Risk === "HighRisk" ? "高风险调用：申请人不能自批；请核对工具版本、过期时间和参数摘要。" : "批准只针对本次冻结调用；工具版本、Schema 或权限变化会使批准失效。"}</div>
            <Descriptions column={{ xs: 1, md: 2 }} size="small" bordered items={[
              { key: "requester", label: "申请人", children: selected.RequesterUserId },
              { key: "requested", label: "申请时间", children: formatTime(selected.RequestedAtUtc) },
              { key: "expires", label: "过期时间", children: formatTime(selected.ExpiresAtUtc) },
              { key: "tool", label: "工具版本", children: <code>{selected.ToolVersionId}</code> },
              { key: "agent", label: "Agent 版本", children: <code>{selected.AgentVersionId}</code> },
              { key: "run", label: "运行", children: <code>{selected.EntryRunId}</code> },
              { key: "arguments", label: "参数 Hash", children: <code>{selected.ArgumentsSha256}</code> },
              { key: "schema", label: "Schema Hash", children: <code>{selected.ToolSchemaSha256}</code> }
            ]} />
            <section className="approval-page__section"><Typography.Title level={5}>安全参数摘要</Typography.Title><pre>{safeJson(selected.SafeArgumentsSummaryJson)}</pre></section>
            <section className="approval-page__section"><Typography.Title level={5}>决定记录</Typography.Title>{detail.Decisions.length ? <Timeline items={detail.Decisions.map(item => ({ color: statusMeta[item.ToStatus].color, children: <><Typography.Text strong>{statusMeta[item.ToStatus].label}</Typography.Text><br /><Typography.Text type="secondary">{item.DecisionUserId} · {formatTime(item.DecidedAtUtc)}{item.DecisionReason ? ` · ${item.DecisionReason}` : ""}</Typography.Text></> }))} /> : <Typography.Text type="secondary">尚未作出决定。</Typography.Text>}</section>
            <Flex className="approval-page__actions" justify="end" gap={8} wrap>
              {selected.Status === "Pending" && <Button onClick={() => { setAction("cancel"); setReason(""); }}>取消申请</Button>}
              {selected.Status === "Pending" && canDecide && <><Button danger icon={<CloseCircleOutlined />} onClick={() => { setAction("reject"); setReason(""); }}>拒绝</Button><Button type="primary" icon={<CheckCircleOutlined />} onClick={() => { setAction("approve"); setReason(""); }}>批准</Button></>}
              {selected.Status === "Approved" && <Button type="primary" loading={actionLoading} onClick={() => void resume()}>恢复原会话</Button>}
            </Flex>
          </>}
        </Spin>
      </Card>
    </main>
    <Modal open={Boolean(action)} title={decisionTitle} okText={action === "approve" ? "确认批准" : action === "reject" ? "确认拒绝" : "确认取消"} okButtonProps={{ danger: action !== "approve", loading: actionLoading }} onOk={() => void submitDecision()} onCancel={() => !actionLoading && setAction(undefined)}>
      <Typography.Paragraph type="secondary">{action === "approve" ? "批准后仍会重新校验工具版本、Schema 和申请人权限。" : "该决定会写入服务端；外部工具不会因拒绝或取消而执行。"}</Typography.Paragraph>
      <Input.TextArea autoFocus rows={3} maxLength={512} value={reason} onChange={event => setReason(event.target.value)} placeholder={action === "approve" ? "审批说明（可选）" : "请填写原因（可选）"} />
    </Modal>
  </section>;
};

export default ApprovalPage;
