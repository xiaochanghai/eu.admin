import { useCallback, useEffect, useRef, useState } from "react";
import { Alert, Button, Descriptions, Flex, Space, Spin, Tag, Typography } from "antd";
import { SyncOutlined } from "@ant-design/icons";
import { AgentUserTokenQuotaBalance, getCurrentUserTokenQuota } from "@/api/modules/agent";
import { store, useSelector } from "@/redux";
import { formatRunDate } from "./runHistoryPresentation";
import QuotaManagement from "./QuotaManagement";

interface UserTokenQuotaProps {
  agentId: string;
  revision: number;
}

// API 边界已校验为非负十进制 long 字符串，格式化不经过 Number。
export const formatQuotaTokens = (value: string | null) => value === null ? "未知" : BigInt(value).toLocaleString("zh-CN");

const UserTokenQuota = ({ agentId, revision }: UserTokenQuotaProps) => {
  const token = useSelector(state => state.user.token);
  const [snapshot, setSnapshot] = useState<{ token: string; balance?: AgentUserTokenQuotaBalance; loading: boolean; error: string }>();
  const request = useRef<AbortController>();
  const refresh = useCallback(async () => {
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    const isCurrent = () => request.current === controller && !controller.signal.aborted && store.getState().user.token === token;
    // 刷新期间也不显示旧的可用余额，避免把过期快照当作准入许可。
    setSnapshot({ token, loading: true, error: "" });
    try {
      const balance = await getCurrentUserTokenQuota(controller.signal);
      if (isCurrent()) setSnapshot({ token, balance, loading: false, error: "" });
    } catch (error) {
      if (isCurrent()) setSnapshot({ token, loading: false, error: error instanceof Error ? error.message : "租户额度读取失败" });
    } finally {
      if (request.current === controller) request.current = undefined;
    }
  }, [token]);

  useEffect(() => {
    void refresh();
    return () => {
      request.current?.abort();
      request.current = undefined;
    };
  }, [refresh, agentId, revision]);

  const current = snapshot?.token === token ? snapshot : undefined;
  const balance = current?.balance;
  const loading = !current || current.loading;
  return (
    <section aria-label="当前租户共享 Token 额度">
      <Flex justify="space-between" align="center" gap={8}>
        <Typography.Title level={5}>当前租户共享 Token 额度</Typography.Title>
        <Button size="small" icon={<SyncOutlined />} loading={loading} onClick={() => void refresh()}>刷新额度</Button>
      </Flex>
      <Typography.Paragraph type="secondary">同一集团、公司内，所有用户和 Agent 共用日/月额度。此处是只读快照，不是账单或请求授权。</Typography.Paragraph>
      {current?.error && <Alert type="error" showIcon message="租户额度读取失败，余额未知" description={current.error} />}
      <Spin spinning={loading}>
        {balance && !balance.Enabled && <Alert type="info" showIcon message="租户共享额度未启用" />}
        {balance?.Enabled && (
          <Space direction="vertical" size="middle" style={{ width: "100%" }}>
            {!balance.CanReserve && (
              <Alert type="warning" showIcon message="当前不能预占新的模型请求：至少一个周期额度不足或已冻结。" />
            )}
            <Typography.Text type="secondary">
              时区：{balance.TimeZoneId} · 单次预占：{formatQuotaTokens(balance.RequestReservationTokens)} Token · 快照：{formatRunDate(balance.EvaluatedAtUtc)}
            </Typography.Text>
            {balance.Periods.map(period => (
              <Descriptions key={period.Kind} size="small" column={2} title={
                <Space wrap>
                  <Typography.Text>{period.Kind === "Daily" ? "日额度" : "月额度"}</Typography.Text>
                  <Tag color={period.HasUnknownUsage ? "error" : period.CanReserve ? "success" : "warning"}>
                    {period.HasUnknownUsage ? "未知用量，已冻结" : period.CanReserve ? "可预占" : "不足单次预占量"}
                  </Tag>
                </Space>
              }>
                <Descriptions.Item label="上限">{formatQuotaTokens(period.LimitTokens)}</Descriptions.Item>
                <Descriptions.Item label="已结算">{formatQuotaTokens(period.UsedTokens)}</Descriptions.Item>
                <Descriptions.Item label="预占中">{formatQuotaTokens(period.ReservedTokens)}</Descriptions.Item>
                <Descriptions.Item label="可用余额">{formatQuotaTokens(period.RemainingTokens)}</Descriptions.Item>
                <Descriptions.Item label="周期结束">{formatRunDate(period.EndUtc)}（不含，{balance.TimeZoneId} 周期）</Descriptions.Item>
              </Descriptions>
            ))}
            {balance.Periods.some(period => period.HasUnknownUsage) && (
              <Alert type="warning" showIcon message="存在未确认消耗，已结算量不是完整用量。需维护者核对后处理，不会自动退还预占。" />
            )}
          </Space>
        )}
        {loading && <Typography.Paragraph type="secondary">正在读取租户额度…</Typography.Paragraph>}
      </Spin>
      <QuotaManagement key={token} onChanged={() => void refresh()} />
    </section>
  );
};

export default UserTokenQuota;
