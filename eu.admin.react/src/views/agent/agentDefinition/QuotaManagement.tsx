import { useEffect, useRef, useState } from "react";
import { Alert, Button, Checkbox, Collapse, Input, Popconfirm, Select, Space, Typography } from "antd";
import {
  AgentQuotaPending,
  AgentQuotaPolicy,
  AgentQuotaPolicyInput,
  AgentQuotaReconcileInput,
  getAgentQuotaManagementAccess,
  getAgentQuotaPending,
  getAgentQuotaPolicy,
  parseQuotaTokenInput,
  reconcileAgentQuota,
  saveAgentQuotaPolicy
} from "@/api/modules/agent";
import { store, useSelector } from "@/redux";

// 父级按 Token remount，所有请求仍在写回前检查真实会话，防止登出/切换用户后串数据。
const QuotaManagement = ({ onChanged }: { onChanged: () => void }) => {
  const token = useSelector(state => state.user.token);
  const [access, setAccess] = useState<{ token: string; allowed: boolean; error?: string }>();
  const allowed = access?.token === token && access.allowed;
  const [policy, setPolicy] = useState<AgentQuotaPolicy>();
  const [pending, setPending] = useState<AgentQuotaPending[]>([]);
  const [defaults, setDefaults] = useState(true);
  const [daily, setDaily] = useState("");
  const [monthly, setMonthly] = useState("");
  const [reservation, setReservation] = useState("");
  const [reason, setReason] = useState("");
  const [evidence, setEvidence] = useState("");
  const [selected, setSelected] = useState<string>();
  const [actual, setActual] = useState("");
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState("");
  const request = useRef<AbortController>();
  const mutation = useRef<{ key: string; id: string }>();
  const writing = useRef(false);

  const current = (controller: AbortController) =>
    request.current === controller && !controller.signal.aborted && store.getState().user.token === token;
  useEffect(() => {
    setAccess(undefined);
    setPolicy(undefined);
    setPending([]);
    setSelected(undefined);
    setBusy(false);
    mutation.current = undefined;
    const controller = new AbortController();
    request.current = controller;
    void getAgentQuotaManagementAccess(controller.signal)
      .then(value => {
        if (current(controller)) setAccess({ token, allowed: value });
      })
      .catch(() => {
        if (current(controller))
          setAccess({ token, allowed: false, error: "额度管理权限暂时无法确认，管理入口不可用；请关闭面板后重开重试。" });
      });
    return () => {
      request.current?.abort();
      request.current = undefined;
    };
  }, [token]);

  const load = async () => {
    if (busy || writing.current || !allowed || store.getState().user.token !== token) return;
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    setBusy(true);
    setPolicy(undefined);
    setPending([]);
    setSelected(undefined);
    setFeedback("");
    try {
      const [next, records] = await Promise.all([
        getAgentQuotaPolicy(controller.signal),
        getAgentQuotaPending(controller.signal)
      ]);
      if (!current(controller)) return;
      setPolicy(next);
      setPending(records);
      setDefaults(next.UseDefaults);
      setDaily(next.DailyTotalTokens ?? "");
      setMonthly(next.MonthlyTotalTokens ?? "");
      setReservation(next.RequestReservationTokens ?? "");
      // 读取状态是一次显式核实，旧失败命令不继续与新版本混用。
      mutation.current = undefined;
    } catch (error) {
      if (current(controller)) setFeedback(error instanceof Error ? error.message : "读取失败");
    } finally {
      if (current(controller)) setBusy(false);
    }
  };

  const apply = async (kind: "policy" | "reconcile") => {
    if (busy || writing.current || !allowed || store.getState().user.token !== token || !policy) return;
    writing.current = true;
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    setBusy(true);
    setFeedback("");
    try {
      if (!reason.trim() || reason.length > 500 || /[\u0000-\u001f\u007f]/.test(reason))
        throw new Error("请填写不含控制字符的修改原因（最多 500 字）");
      const idFor = (content: object) => {
        const key = JSON.stringify({ kind, content });
        if (mutation.current?.key !== key) mutation.current = { key, id: crypto.randomUUID() };
        return mutation.current.id;
      };
      if (kind === "policy") {
        const content = {
          ExpectedRevision: policy.Revision,
          UseDefaults: defaults,
          DailyTotalTokens: defaults ? null : parseQuotaTokenInput(daily),
          MonthlyTotalTokens: defaults ? null : parseQuotaTokenInput(monthly),
          RequestReservationTokens: defaults ? null : parseQuotaTokenInput(reservation),
          Reason: reason.trim()
        };
        if (
          !defaults &&
          (!content.RequestReservationTokens ||
            (!content.DailyTotalTokens && !content.MonthlyTotalTokens) ||
            [content.DailyTotalTokens, content.MonthlyTotalTokens].some(
              limit => limit !== null && BigInt(limit) < BigInt(content.RequestReservationTokens ?? "0")
            ))
        )
          throw new Error("至少填写一个周期上限和单次预占量，预占量不能超过周期上限");
        const input: AgentQuotaPolicyInput = { ...content, OperationId: idFor(content) };
        const next = await saveAgentQuotaPolicy(input, controller.signal);
        if (!current(controller)) return;
        setPolicy(next);
      } else {
        if (!selected || !confirmed || !evidence.trim() || evidence.length > 256 || /[\u0000-\u001f\u007f]/.test(evidence))
          throw new Error("请选择记录并确认真实用量，填写核查依据（最多 256 字）");
        const amount = parseQuotaTokenInput(actual, true);
        if (amount === null) throw new Error("实际用量不能为空；确认真实零用量才可填写 0");
        const content = { ActualTokens: amount, Confirmed: true, Reason: reason.trim(), EvidenceReference: evidence.trim() };
        const input: AgentQuotaReconcileInput = { ...content, OperationId: idFor({ selected, ...content }) };
        await reconcileAgentQuota(selected, input, controller.signal);
        if (!current(controller)) return;
        setPending(records => records.filter(record => record.Id !== selected));
        setSelected(undefined);
        setActual("");
        setConfirmed(false);
      }
      mutation.current = undefined;
      setFeedback("已提交并保存审计记录。");
      onChanged();
    } catch (error) {
      if (current(controller))
        setFeedback(
          `${error instanceof Error ? error.message : "操作失败"}。提交状态可能未知，保持相同参数重试会复用操作标识；也可重新读取核实。`
        );
    } finally {
      writing.current = false;
      if (current(controller)) setBusy(false);
    }
  };

  if (store.getState().user.token !== token || access?.token !== token) return null;
  if (access.error) return <Alert type="warning" showIcon message={access.error} />;
  if (!allowed) return null;
  return (
    <Collapse
      items={[
        {
          key: "quota-management",
          label: "额度管理与人工对账（当前租户）",
          children: (
            <Space direction="vertical" size="middle" style={{ width: "100%" }}>
              <Alert type="warning" showIcon message="修改上限不会重置用量。对账必须依据真实供应商记录，不能把未知消耗填成 0。" />
              <Button loading={busy} onClick={() => void load()}>
                读取当前租户额度
              </Button>
              {feedback && <Alert type="info" showIcon message={feedback} />}
              {policy && (
                <>
                  <Typography.Text>设置版本：{policy.Revision}（降低额度可能立即阻止新请求）</Typography.Text>
                  <Checkbox checked={defaults} disabled={busy} onChange={event => setDefaults(event.target.checked)}>
                    使用宿主默认额度
                  </Checkbox>
                  <Input
                    aria-label="日 Token 上限"
                    placeholder="日 Token 上限，留空不启用"
                    value={daily}
                    disabled={busy || defaults}
                    onChange={event => setDaily(event.target.value)}
                  />
                  <Input
                    aria-label="月 Token 上限"
                    placeholder="月 Token 上限，留空不启用"
                    value={monthly}
                    disabled={busy || defaults}
                    onChange={event => setMonthly(event.target.value)}
                  />
                  <Input
                    aria-label="单次预占量"
                    placeholder="单次预占量"
                    value={reservation}
                    disabled={busy || defaults}
                    onChange={event => setReservation(event.target.value)}
                  />
                  <Input.TextArea
                    aria-label="修改或对账原因"
                    placeholder="修改或对账原因（必填）"
                    maxLength={500}
                    value={reason}
                    disabled={busy}
                    onChange={event => setReason(event.target.value)}
                  />
                  <Popconfirm
                    title="确认修改当前集团、公司的共享额度？"
                    description="不会清空已结算或预占的 Token。"
                    disabled={busy}
                    onConfirm={() => apply("policy")}
                  >
                    <Button disabled={busy || !reason.trim()} loading={busy}>
                      应用额度设置
                    </Button>
                  </Popconfirm>
                  <Typography.Text strong>待核查预占（最多 100 条）</Typography.Text>
                  <Typography.Text type="secondary">
                    状态 0/1 可能仍活跃，仅有已落库且超过 5 分钟的运行终态才可核实处理；状态 3 为已报告未知用量。
                  </Typography.Text>
                  <Select
                    aria-label="待对账预占记录"
                    placeholder={pending.length ? "选择待对账记录" : "暂无待对账记录"}
                    style={{ width: "100%" }}
                    value={selected}
                    disabled={busy}
                    onChange={setSelected}
                    options={pending.map(record => ({
                      value: record.Id,
                      label: `${record.Id} · 用户 ${record.ConsumerUserId} · 状态 ${record.State} · 预占 ${BigInt(record.ReservedTokens).toLocaleString("zh-CN")}`
                    }))}
                  />
                  <Input
                    aria-label="核实实际总 Token"
                    placeholder="核实的实际总 Token（真实零用量填写 0）"
                    value={actual}
                    disabled={busy}
                    onChange={event => setActual(event.target.value)}
                  />
                  <Input
                    aria-label="核查依据引用"
                    placeholder="供应商账单/核查记录引用，不要粘贴凭据或账单正文"
                    maxLength={256}
                    value={evidence}
                    disabled={busy}
                    onChange={event => setEvidence(event.target.value)}
                  />
                  <Checkbox checked={confirmed} disabled={busy} onChange={event => setConfirmed(event.target.checked)}>
                    请求已终止，实际总用量已按依据核实
                  </Checkbox>
                  <Popconfirm
                    title="确认用核实用量结算原始周期？"
                    description="将释放原预占、计入实际消耗，并追加不可覆盖的审计。"
                    disabled={busy}
                    onConfirm={() => apply("reconcile")}
                  >
                    <Button
                      disabled={busy || !selected || !confirmed || !reason.trim() || !evidence.trim() || !actual.trim()}
                      loading={busy}
                    >
                      提交人工对账
                    </Button>
                  </Popconfirm>
                </>
              )}
            </Space>
          )
        }
      ]}
    />
  );
};

export default QuotaManagement;
