import { useCallback, useEffect, useRef, useState } from "react";
import { Alert, Button, Collapse, Descriptions, Flex, List, Space, Spin, Tag, Typography } from "antd";
import { SyncOutlined } from "@ant-design/icons";
import { AgentRunAuditRecord, listAgentRuns } from "@/api/modules/agent";
import { store, useSelector } from "@/redux";
import UserTokenQuota from "./UserTokenQuota";
import {
  describeRunStatus,
  describeRunUsage,
  formatRunCount,
  formatRunDate,
  formatRunDuration,
  formatRunMilliseconds
} from "./runHistoryPresentation";

interface RunHistoryProps {
  agentId: string;
  revision: number;
}

const historyLimit = 20;
const validOptionalText = (...values: unknown[]) => values.every(value => value == null || typeof value === "string");

const RunHistory = ({ agentId, revision }: RunHistoryProps) => {
  const token = useSelector(state => state.user.token);
  const [history, setHistory] = useState<{
    agentId: string;
    token: string;
    records: AgentRunAuditRecord[];
    loading: boolean;
    error: string;
  }>();
  const request = useRef<AbortController>();
  const refresh = useCallback(async () => {
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    const isCurrent = () => request.current === controller && !controller.signal.aborted && store.getState().user.token === token;
    setHistory(previous => ({
      agentId,
      token,
      records: previous?.agentId === agentId && previous.token === token ? previous.records : [],
      loading: true,
      error: ""
    }));
    try {
      const records = await listAgentRuns(agentId, historyLimit, controller.signal);
      if (!Array.isArray(records) || records.some(record =>
        !record || typeof record.RunId !== "string" || typeof record.Status !== "string" || typeof record.StartedAtUtc !== "string"
        || !validOptionalText(record.AgentVersionId, record.ModelProfileId, record.ErrorCode, record.FinishedAtUtc)
        || (Array.isArray(record.ToolCalls) && record.ToolCalls.some(tool => !tool
          || !validOptionalText(tool.ToolName, tool.Risk, tool.Status, tool.ErrorCode, tool.StartedAtUtc, tool.FinishedAtUtc)))
      )) throw new Error("运行历史响应格式无效");
      if (isCurrent()) setHistory({ agentId, token, records, loading: false, error: "" });
    } catch (error) {
      if (isCurrent()) {
        setHistory(previous => ({
          agentId,
          token,
          records: previous?.records || [],
          loading: false,
          error: error instanceof Error ? error.message : "运行历史读取失败"
        }));
      }
    } finally {
      if (request.current === controller) request.current = undefined;
    }
  }, [agentId, token]);

  useEffect(() => {
    void refresh();
    return () => {
      request.current?.abort();
      request.current = undefined;
    };
  }, [refresh, revision]);

  const current = history?.agentId === agentId && history.token === token ? history : undefined;
  const loading = !current || current.loading;
  const records = current?.records || [];
  return (
    <section className="agent-definition-form__run-events" aria-label="近期运行历史">
      <Flex justify="space-between" align="center" gap={8}>
        <Typography.Title level={5}>近期运行</Typography.Title>
        <Button size="small" icon={<SyncOutlined />} loading={loading} onClick={() => void refresh()}>刷新</Button>
      </Flex>
      <Typography.Paragraph type="secondary">
        最多显示最近 {historyLimit} 条运行，不是全部历史或账单。未知用量不补零，部分统计不可当作完整消耗。
      </Typography.Paragraph>
      {current?.error && (
        <Alert type="error" showIcon message="运行历史读取失败" description={current.error} />
      )}
      {current?.error && records.length > 0 && <Typography.Text type="secondary">以下为上次加载的记录，尚未刷新。</Typography.Text>}
      <Spin spinning={loading}>
        {records.length > 0 ? (
          <Collapse items={records.map(record => {
            const status = describeRunStatus(record.Status);
            const usage = describeRunUsage(record.ModelUsage?.Status);
            return {
              key: record.RunId,
              label: (
                <Space wrap>
                  <Tag color={status.color}>{status.label}</Tag>
                  <Typography.Text>{formatRunDate(record.StartedAtUtc)}</Typography.Text>
                  <Typography.Text>Token：{formatRunCount(record.ModelUsage?.TotalTokens)}</Typography.Text>
                  <Tag color={usage.color}>{usage.label}</Tag>
                </Space>
              ),
              children: (
                <>
                  <Descriptions size="small" column={1}>
                    <Descriptions.Item label="运行标识">{record.RunId}</Descriptions.Item>
                    <Descriptions.Item label="发布版本标识">{record.AgentVersionId || "未知"}</Descriptions.Item>
                    <Descriptions.Item label="模型配置">{record.ModelProfileId || "未知"}</Descriptions.Item>
                    <Descriptions.Item label="结束时间">{formatRunDate(record.FinishedAtUtc)}</Descriptions.Item>
                    <Descriptions.Item label="运行耗时">{formatRunDuration(record.StartedAtUtc, record.FinishedAtUtc)}</Descriptions.Item>
                    <Descriptions.Item label="输入 Token">{formatRunCount(record.ModelUsage?.InputTokens)}</Descriptions.Item>
                    <Descriptions.Item label="输出 Token">{formatRunCount(record.ModelUsage?.OutputTokens)}</Descriptions.Item>
                    <Descriptions.Item label="总 Token">{formatRunCount(record.ModelUsage?.TotalTokens)}</Descriptions.Item>
                    <Descriptions.Item label="统计完整性">{usage.label}</Descriptions.Item>
                    <Descriptions.Item label="模型循环耗时">{formatRunMilliseconds(record.ModelUsage?.ModelDurationMilliseconds)}</Descriptions.Item>
                    <Descriptions.Item label="首段文本时间">{formatRunMilliseconds(record.ModelUsage?.TimeToFirstTextMilliseconds)}</Descriptions.Item>
                    <Descriptions.Item label="输出字符数">{formatRunCount(record.OutputCharacters)}</Descriptions.Item>
                    <Descriptions.Item label="工具调用次数">{formatRunCount(record.ToolCallCount)}</Descriptions.Item>
                  </Descriptions>
                  {record.ErrorCode && <Alert type="error" showIcon message={record.ErrorCode} />}
                  <List
                    size="small"
                    header="工具审计"
                    dataSource={Array.isArray(record.ToolCalls) ? record.ToolCalls : []}
                    locale={{ emptyText: "暂无工具审计记录" }}
                    renderItem={tool => (
                      <List.Item>
                        <Space direction="vertical" size={4}>
                          <Typography.Text>{tool.ToolName} · {tool.Risk} · {tool.Status}</Typography.Text>
                          <Typography.Text type="secondary">{formatRunDuration(tool.StartedAtUtc, tool.FinishedAtUtc)}</Typography.Text>
                          {tool.ErrorCode && <Typography.Text type="danger">{tool.ErrorCode}</Typography.Text>}
                        </Space>
                      </List.Item>
                    )}
                  />
                </>
              )
            };
          })} />
        ) : <Typography.Paragraph type="secondary">{loading ? "正在读取运行历史…" : current?.error ? "无法确认运行历史，请点击刷新重试。" : "尚无运行记录"}</Typography.Paragraph>}
      </Spin>
      <UserTokenQuota agentId={agentId} revision={revision} />
    </section>
  );
};

export default RunHistory;
