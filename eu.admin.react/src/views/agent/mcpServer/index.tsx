import React, { useMemo } from "react";
import { Tag, Typography } from "antd";
import { TableList } from "@/components";
import type { ColumnCustomizerMap } from "@/components/ProTable/columnCustomizers";
import type { McpServerStatus } from "@/api/modules/agentMcp";
import FormPage from "./FormPage";

const MODULE_CODE = "AG_MCP_SERVER_MNG";

const statusMeta: Record<McpServerStatus, { color: string; text: string }> = {
  NotSynced: { color: "default", text: "未同步" },
  Healthy: { color: "success", text: "健康" },
  Unhealthy: { color: "error", text: "异常" },
  Disabled: { color: "warning", text: "已停用" },
  Archived: { color: "default", text: "已归档" }
};

const transportText = {
  StreamableHttp: "Streamable HTTP",
  Sse: "SSE",
  Stdio: "Stdio"
} as const;

const Index: React.FC = () => {
  const columnCustomizers = useMemo<ColumnCustomizerMap>(
    () => ({
      Transport: original => ({
        ...original,
        renderText: value => transportText[value as keyof typeof transportText] || value
      }),
      ConnectionTarget: original => ({
        ...original,
        render: (_, record) => (
          <Typography.Text type="secondary" ellipsis>
            {record.ConnectionTarget || "—"}
          </Typography.Text>
        )
      }),
      Status: original => ({
        ...original,
        render: (_, record) => {
          const status = statusMeta[record.Status as McpServerStatus];
          return <Tag color={status?.color}>{status?.text || record.Status}</Tag>;
        }
      })
    }),
    []
  );

  return <TableList moduleCode={MODULE_CODE} DynamicFormPage={FormPage} columnCustomizers={columnCustomizers} />;
};

export default Index;
