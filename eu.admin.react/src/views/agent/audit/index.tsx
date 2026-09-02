import { AuditOutlined, ReloadOutlined } from "@ant-design/icons";
import { Alert, Button, Card, Drawer, Select, Space, Table, Tag, Typography, type TableColumnsType } from "antd";
import { useCallback, useEffect, useState } from "react";
import { getAgentAuditErrorMessage, listAgentOperationAudits, type AgentOperationAudit } from "@/api/modules/agentAudit";
import { getModuleInfo } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import "./index.less";

const MODULE_CODE = "AG_OPERATION_AUDIT_MNG";
const formatTime = (value: string) => Number.isNaN(Date.parse(value)) ? "-" : new Intl.DateTimeFormat("zh-CN", { dateStyle: "medium", timeStyle: "medium" }).format(new Date(value));

const OperationAuditPage = () => {
  const [take, setTake] = useState(100);
  const [items, setItems] = useState<AgentOperationAudit[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [selected, setSelected] = useState<AgentOperationAudit>();

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try { setItems(await listAgentOperationAudits(take)); }
    catch (loadError) { const text = getAgentAuditErrorMessage(loadError, "操作审计记录加载失败"); setError(text); message.error(text); }
    finally { setLoading(false); }
  }, [take]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    let active = true;
    void getModuleInfo(MODULE_CODE).catch(moduleError => { if (active) message.error(getAgentAuditErrorMessage(moduleError, "操作审计模块权限加载失败")); });
    return () => { active = false; };
  }, []);

  const columns: TableColumnsType<AgentOperationAudit> = [
    { title: "时间", dataIndex: "OccurredAtUtc", width: 180, render: formatTime },
    { title: "请求", key: "request", width: 320, render: (_, item) => <Space size={6}><Tag color="blue">{item.Method}</Tag><Typography.Text ellipsis={{ tooltip: item.Path }} className="audit-page__path">{item.Path}</Typography.Text></Space> },
    { title: "结果", key: "outcome", width: 180, render: (_, item) => <Space size={6}><Tag color={item.StatusCode >= 400 ? "error" : "success"}>{item.StatusCode}</Tag><Typography.Text>{item.Outcome || "-"}</Typography.Text></Space> },
    { title: "策略", dataIndex: "Policy", width: 160, ellipsis: true },
    { title: "耗时", dataIndex: "DurationMilliseconds", width: 100, render: value => `${value} ms` },
    { title: "用户", dataIndex: "UserId", width: 150, ellipsis: true },
    { title: "错误码", dataIndex: "ErrorCode", width: 180, ellipsis: true, render: value => value || "-" }
  ];

  return <section className="audit-page">
    <header className="audit-page__header"><div><Typography.Title level={3}><AuditOutlined /> 操作审计</Typography.Title><Typography.Text type="secondary">查看当前租户内 Agent API 的受保护操作、策略判定与请求结果。</Typography.Text></div><Space wrap><Select aria-label="审计记录数量" value={take} options={[50, 100].map(value => ({ value, label: `最近 ${value} 条` }))} onChange={setTake} /><Button icon={<ReloadOutlined />} loading={loading} onClick={() => void load()}>刷新</Button></Space></header>
    {error && <Alert className="audit-page__alert" type="error" showIcon message={error} />}
    <Card className="audit-page__table" title="操作记录"><Table<AgentOperationAudit> rowKey="Id" loading={loading} dataSource={items} columns={columns} pagination={false} scroll={{ x: 1250 }} locale={{ emptyText: "暂无可见操作审计记录" }} onRow={record => ({ onClick: () => setSelected(record) })} /></Card>
    <Drawer title="审计详情" width={560} open={Boolean(selected)} onClose={() => setSelected(undefined)}>{selected && <dl className="audit-page__details"><dt>请求时间</dt><dd>{formatTime(selected.OccurredAtUtc)}</dd><dt>请求方法</dt><dd>{selected.Method}</dd><dt>请求路径</dt><dd>{selected.Path}</dd><dt>HTTP 状态</dt><dd>{selected.StatusCode}</dd><dt>结果</dt><dd>{selected.Outcome || "-"}</dd><dt>策略</dt><dd>{selected.Policy || "-"}</dd><dt>耗时</dt><dd>{selected.DurationMilliseconds} ms</dd><dt>用户</dt><dd>{selected.UserId || "-"}</dd><dt>租户</dt><dd>{selected.TenantId || "-"}</dd><dt>关联 ID</dt><dd>{selected.CorrelationId || "-"}</dd><dt>错误码</dt><dd>{selected.ErrorCode || "-"}</dd></dl>}</Drawer>
  </section>;
};

export default OperationAuditPage;
