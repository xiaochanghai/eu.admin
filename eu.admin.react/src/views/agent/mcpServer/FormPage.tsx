import React, { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from "react";
import { Alert, Button, Descriptions, Empty, Flex, Form, Input, Select, Space, Spin, Switch, Tag, Typography } from "antd";
import { SyncOutlined } from "@ant-design/icons";
import { message } from "@/hooks/useMessage";
import {
  classifyMcpTool,
  createMcpServer,
  getMcpServer,
  McpServerDefinition,
  McpServerInput,
  McpServerStatus,
  McpToolRisk,
  McpTransportKind,
  setMcpServerArchived,
  syncMcpServer,
  updateMcpServer
} from "@/api/modules/agentMcp";
import type { UpdateMcpServerInput } from "@/api/modules/agentMcp";
import { SaveTypeEnum } from "@/typings";
import "./index.less";

interface McpServerFormValues {
  code: string;
  name: string;
  description: string;
  transport: McpTransportKind;
  endpoint: string;
  command: string;
  argumentsText: string;
  credentialAlias: string;
  enabled: boolean;
}

interface FormPageProps {
  Id?: string | null;
  IsView?: boolean | null;
  formPageRef: React.RefObject<{ onSave: () => void; onSaveAdd: () => void } | null>;
  onReload?: () => void;
  onDisabled?: (disabled: boolean) => void;
}

const statusMeta: Record<McpServerStatus, { color: string; text: string }> = {
  NotSynced: { color: "default", text: "未同步" },
  Healthy: { color: "success", text: "健康" },
  Unhealthy: { color: "error", text: "异常" },
  Disabled: { color: "warning", text: "已停用" },
  Archived: { color: "default", text: "已归档" }
};

const riskOptions: Array<{ label: string; value: McpToolRisk; disabled?: boolean }> = [
  { label: "Unknown（待分类）", value: "Unknown", disabled: true },
  { label: "ReadOnly", value: "ReadOnly" },
  { label: "Mutating", value: "Mutating" },
  { label: "HighRisk", value: "HighRisk" }
];

const errorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error && error.message) return error.message;
  if (typeof error === "object" && error !== null && "Message" in error && typeof error.Message === "string") {
    return error.Message;
  }
  return fallback;
};

const errorCode = (error: unknown) => {
  if (typeof error !== "object" || error === null || !("Data" in error)) return "";
  const data = error.Data;
  return typeof data === "object" && data !== null && "ErrorCode" in data && typeof data.ErrorCode === "string"
    ? data.ErrorCode
    : "";
};

const archiveErrorMessage = (error: unknown) => {
  const messageText = errorMessage(error, "MCP Server 归档状态更新失败");
  if (errorCode(error) !== "MCP_ARCHIVE_BLOCKED") return messageText;
  const marker = "Agent(s): ";
  const markerIndex = messageText.indexOf(marker);
  const references =
    markerIndex >= 0
      ? `Agent“${messageText
          .slice(markerIndex + marker.length)
          .replace(/\.$/, "")
          .replace(/, /g, "”、Agent“")}”`
      : "已启用 Agent";
  return `暂时无法归档：${references}仍在使用该 MCP Server。请先解除工具绑定或停用引用方，再重新归档。`;
};

const FormPage: React.FC<FormPageProps> = ({ Id, IsView, formPageRef, onReload, onDisabled }) => {
  const [form] = Form.useForm<McpServerFormValues>();
  const onDisabledRef = useRef(onDisabled);
  const transport = Form.useWatch("transport", form) || "StreamableHttp";
  const [current, setCurrent] = useState<McpServerDefinition | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [editorError, setEditorError] = useState("");
  const [classifyingId, setClassifyingId] = useState<string | null>(null);

  const archived = current?.Status === "Archived";
  const readOnly = Boolean(IsView || archived);
  const actionBusy = busy || classifyingId !== null;

  useEffect(() => {
    onDisabledRef.current = onDisabled;
  }, [onDisabled]);

  const fillEditor = useCallback(
    (server: McpServerDefinition) => {
      setCurrent(server);
      setEditorError("");
      form.setFieldsValue({
        code: server.Code || "",
        name: server.Name || "",
        description: server.Description || "",
        transport: server.Transport || "StreamableHttp",
        endpoint: server.Endpoint || "",
        command: server.Command || "",
        argumentsText: (server.Arguments || []).join("\n"),
        credentialAlias: server.CredentialAlias || "",
        enabled: server.Enabled
      });
      onDisabledRef.current?.(true);
    },
    [form]
  );

  const resetEditor = useCallback(() => {
    setCurrent(null);
    setEditorError("");
    form.resetFields();
    form.setFieldsValue({ transport: "StreamableHttp", enabled: true });
    onDisabledRef.current?.(Boolean(IsView));
  }, [IsView, form]);

  const load = useCallback(async () => {
    setLoading(true);
    setEditorError("");
    try {
      if (Id) fillEditor(await getMcpServer(Id));
      else resetEditor();
    } catch (error) {
      setEditorError(errorMessage(error, "MCP Server 配置加载失败"));
      onDisabledRef.current?.(true);
    } finally {
      setLoading(false);
    }
  }, [Id, fillEditor, resetEditor]);

  useEffect(() => {
    void load();
  }, [load]);

  const buildInput = useCallback((values: McpServerFormValues): McpServerInput => {
    const stdio = values.transport === "Stdio";
    return {
      code: values.code.trim(),
      name: values.name.trim(),
      description: values.description || "",
      transport: values.transport,
      endpoint: stdio ? "" : values.endpoint.trim(),
      command: stdio ? values.command.trim() : "",
      arguments: stdio
        ? (values.argumentsText || "")
            .split(/\r?\n/)
            .map(value => value.trim())
            .filter(Boolean)
        : [],
      credentialAlias: values.credentialAlias.trim(),
      enabled: values.enabled
    };
  }, []);

  const buildUpdateInput = (
    input: McpServerInput,
    expectedLogicalRevision: number,
    enabled = input.enabled
  ): UpdateMcpServerInput => ({
    name: input.name,
    description: input.description,
    transport: input.transport,
    endpoint: input.endpoint,
    command: input.command,
    arguments: input.arguments,
    credentialAlias: input.credentialAlias,
    enabled,
    expectedLogicalRevision
  });

  const save = useCallback(
    async (saveType = SaveTypeEnum.Save) => {
      if (actionBusy || readOnly) return;
      const values = await form.validateFields();
      const input = buildInput(values);
      setBusy(true);
      setEditorError("");
      try {
        const saved = current
          ? await updateMcpServer(current.Id, buildUpdateInput(input, current.LogicalRevision))
          : await createMcpServer(input);
        fillEditor(saved);
        onReload?.();
        message.success(current ? "MCP Server 配置已保存" : "MCP Server 已创建");
        if (saveType === SaveTypeEnum.SaveAdd) resetEditor();
      } catch (error) {
        setEditorError(errorMessage(error, "MCP Server 保存失败"));
      } finally {
        setBusy(false);
      }
    },
    [actionBusy, buildInput, current, fillEditor, form, onReload, readOnly, resetEditor]
  );

  useImperativeHandle(
    formPageRef,
    () => ({
      onSave: () => void save(),
      onSaveAdd: () => void save(SaveTypeEnum.SaveAdd)
    }),
    [save]
  );

  const sync = async () => {
    if (!current || actionBusy || archived) return;
    setBusy(true);
    setEditorError("");
    try {
      const synced = await syncMcpServer(current.Id, current.LogicalRevision);
      fillEditor(synced);
      onReload?.();
      message.success(`同步完成，共发现 ${synced.CurrentToolVersionIds.length} 个工具`);
    } catch (error) {
      const syncError = errorMessage(error, "MCP 工具同步失败");
      try {
        fillEditor(await getMcpServer(current.Id));
      } catch {
        // 保留原始同步错误和当前编辑状态。
      }
      setEditorError(syncError);
      onReload?.();
    } finally {
      setBusy(false);
    }
  };

  const toggleEnabled = async () => {
    if (!current || actionBusy || archived) return;
    const values = await form.validateFields();
    setBusy(true);
    setEditorError("");
    try {
      const updated = await updateMcpServer(
        current.Id,
        buildUpdateInput(buildInput(values), current.LogicalRevision, !current.Enabled)
      );
      fillEditor(updated);
      onReload?.();
      message.success(updated.Enabled ? "MCP Server 已启用，请同步工具后使用" : "MCP Server 已停用");
    } catch (error) {
      setEditorError(errorMessage(error, "MCP Server 状态更新失败"));
    } finally {
      setBusy(false);
    }
  };

  const toggleArchived = async () => {
    if (!current || actionBusy || IsView) return;
    const restoring = current.Status === "Archived";
    if (!restoring && current.Enabled) {
      setEditorError("请先停用 MCP Server，再执行归档。");
      return;
    }
    setBusy(true);
    setEditorError("");
    try {
      const updated = await setMcpServerArchived(current.Id, current.LogicalRevision, !restoring);
      fillEditor(updated);
      onReload?.();
      message.success(restoring ? "MCP Server 已恢复为停用状态" : "MCP Server 已归档");
    } catch (error) {
      setEditorError(archiveErrorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  const classify = async (toolVersionId: string, risk: McpToolRisk) => {
    if (!current || actionBusy || readOnly || risk === "Unknown") return;
    setClassifyingId(toolVersionId);
    setEditorError("");
    try {
      const updated = await classifyMcpTool(current.Id, toolVersionId, current.LogicalRevision, risk);
      fillEditor(updated);
      onReload?.();
      message.success("工具风险分类已保存，新工具版本已生成");
    } catch (error) {
      setEditorError(errorMessage(error, "工具风险分类保存失败"));
    } finally {
      setClassifyingId(null);
    }
  };

  const currentTools = useMemo(() => {
    if (!current) return [];
    const versions = new Map(current.ToolVersions.map(tool => [tool.Id, tool]));
    return current.CurrentToolVersionIds.map(id => versions.get(id)).filter(tool => tool !== undefined);
  }, [current]);

  return (
    <Spin spinning={loading || busy}>
      <div className="mcp-server-page">
        {editorError && (
          <Alert
            type="error"
            showIcon
            message={editorError}
            closable
            onClose={() => setEditorError("")}
            className="mcp-server-page__alert"
          />
        )}

        {current && (
          <>
            <Flex justify="space-between" align="center" gap={12} wrap className="mcp-server-page__toolbar">
              <Descriptions size="small" column={{ xs: 1, sm: 2 }}>
                <Descriptions.Item label="状态">
                  <Tag color={statusMeta[current.Status].color}>{statusMeta[current.Status].text}</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="修订号">REV {current.LogicalRevision}</Descriptions.Item>
                <Descriptions.Item label="最近同步">
                  {current.LastSyncedAtUtc ? new Date(current.LastSyncedAtUtc).toLocaleString() : "尚未同步"}
                </Descriptions.Item>
                <Descriptions.Item label="当前工具">{current.CurrentToolVersionIds.length} 个</Descriptions.Item>
              </Descriptions>
              <Space wrap>
                {archived ? (
                  <Button onClick={() => void toggleArchived()} disabled={actionBusy || Boolean(IsView)}>
                    恢复
                  </Button>
                ) : (
                  <>
                    <Button onClick={() => void toggleEnabled()} disabled={actionBusy || Boolean(IsView)}>
                      {current.Enabled ? "停用" : "启用"}
                    </Button>
                    <Button icon={<SyncOutlined />} onClick={() => void sync()} disabled={actionBusy || Boolean(IsView)}>
                      同步工具
                    </Button>
                    <Button danger onClick={() => void toggleArchived()} disabled={actionBusy || Boolean(IsView)}>
                      归档
                    </Button>
                  </>
                )}
              </Space>
            </Flex>
            {current.LastError && (
              <Alert
                type="warning"
                showIcon
                message="最近一次连接错误"
                description={current.LastError}
                className="mcp-server-page__alert"
              />
            )}
            {archived && <Alert type="warning" showIcon message="该 MCP Server 已归档，当前仅允许查看或恢复。" />}
          </>
        )}

        <Form<McpServerFormValues>
          form={form}
          layout="vertical"
          requiredMark="optional"
          initialValues={{ transport: "StreamableHttp", enabled: true }}
          disabled={readOnly || actionBusy}
          onValuesChange={() => onDisabledRef.current?.(false)}
        >
          <Flex gap={16} wrap>
            <Form.Item
              name="code"
              label="Server Code"
              rules={[{ required: true }, { pattern: /^[a-z0-9]+(?:-[a-z0-9]+)*$/, message: "请输入小写 kebab-case" }]}
              className="mcp-server-page__half"
            >
              <Input
                maxLength={64}
                disabled={Boolean(current) || readOnly}
                placeholder="例如 business-query"
                onBlur={event => {
                  if (event.target.value.trim() === "business-query" && !form.getFieldValue("credentialAlias")) {
                    form.setFieldValue("credentialAlias", "alias:business-query-local");
                  }
                }}
              />
            </Form.Item>
            <Form.Item name="name" label="名称" rules={[{ required: true }]} className="mcp-server-page__half">
              <Input maxLength={128} />
            </Form.Item>
          </Flex>
          <Form.Item name="description" label="说明">
            <Input.TextArea autoSize={{ minRows: 2, maxRows: 5 }} maxLength={1000} showCount />
          </Form.Item>
          <Flex gap={16} wrap>
            <Form.Item name="transport" label="传输方式" rules={[{ required: true }]} className="mcp-server-page__half">
              <Select
                options={[
                  { label: "Streamable HTTP", value: "StreamableHttp" },
                  { label: "SSE", value: "Sse" },
                  { label: "Stdio", value: "Stdio" }
                ]}
              />
            </Form.Item>
            <Form.Item name="credentialAlias" label="凭据别名" className="mcp-server-page__half">
              <Input maxLength={256} placeholder="alias:production-mcp" />
            </Form.Item>
          </Flex>
          {transport === "Stdio" ? (
            <>
              <Form.Item name="command" label="命令" rules={[{ required: true }]}>
                <Input placeholder="可执行文件或命令" />
              </Form.Item>
              <Form.Item name="argumentsText" label="参数（每行一个）">
                <Input.TextArea className="mcp-server-page__code" autoSize={{ minRows: 4, maxRows: 8 }} />
              </Form.Item>
            </>
          ) : (
            <Form.Item name="endpoint" label="Endpoint" rules={[{ required: true }, { type: "url", message: "请输入有效 URL" }]}>
              <Input placeholder={transport === "Sse" ? "https://example.com/sse" : "https://example.com/mcp"} />
            </Form.Item>
          )}
          <Form.Item name="enabled" label="启用" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>

        {current && (
          <section className="mcp-server-page__tools">
            <Flex justify="space-between" align="center" gap={12} wrap>
              <div>
                <Typography.Title level={5}>已发现工具</Typography.Title>
                <Typography.Text type="secondary">风险等级变更会生成新的工具版本。</Typography.Text>
              </div>
              <Tag>{currentTools.length} 个</Tag>
            </Flex>
            {currentTools.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="尚未发现工具，请保存配置后执行同步" />
            ) : (
              currentTools.map(tool => (
                <Flex key={tool.Id} justify="space-between" align="center" gap={16} className="mcp-server-page__tool-row">
                  <div className="mcp-server-page__tool-info">
                    <Typography.Text strong code>
                      {tool.Name}
                    </Typography.Text>
                    <Typography.Text type="secondary" ellipsis>
                      {tool.Description || "无说明"}
                    </Typography.Text>
                  </div>
                  <Select<McpToolRisk>
                    aria-label={`${tool.Name} 风险等级`}
                    options={riskOptions}
                    value={tool.Risk}
                    loading={classifyingId === tool.Id}
                    disabled={Boolean(classifyingId) || busy || readOnly}
                    onChange={risk => void classify(tool.Id, risk)}
                    className="mcp-server-page__risk"
                  />
                </Flex>
              ))
            )}
          </section>
        )}
      </div>
    </Spin>
  );
};

export default FormPage;
