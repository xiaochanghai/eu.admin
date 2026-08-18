import React, { useMemo } from "react";
import { Tag } from "antd";
import { TableList } from "@/components";
import type { ColumnCustomizerMap } from "@/components/ProTable/columnCustomizers";
import FormPage from "./FormPage";

const MODULE_CODE = "AG_AGENT_DEFINITION_MNG";

const statusText = {
  Enabled: { color: "success", text: "已启用" },
  Disabled: { color: "default", text: "已停用" },
  Archived: { color: "warning", text: "已归档" }
} as const;

const Index: React.FC = () => {
  const columnCustomizers = useMemo<ColumnCustomizerMap>(() => ({
    RuntimeStatus: original => ({
      ...original,
      render: (_, record) => {
        const status = statusText[record.RuntimeStatus as keyof typeof statusText];
        return <Tag color={status?.color}>{status?.text || record.RuntimeStatus}</Tag>;
      }
    }),
    CurrentPublishedLabel: original => ({
      ...original,
      renderText: value => value ? `v${value}` : "仅 Draft"
    })
  }), []);

  return (
    <TableList
      moduleCode={MODULE_CODE}
      DynamicFormPage={FormPage}
      columnCustomizers={columnCustomizers}
    />
  );
};

export default Index;
