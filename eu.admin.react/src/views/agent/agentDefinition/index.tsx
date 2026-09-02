import React, { useMemo, useRef, useState } from "react";
import { Button, Tag, Upload } from "antd";
import { UploadOutlined } from "@ant-design/icons";
import { TableList } from "@/components";
import type { ActionType } from "@/typings";
import type { ColumnCustomizerMap } from "@/components/ProTable/columnCustomizers";
import { importAgent } from "@/api/modules/agent";
import { message } from "@/hooks/useMessage";
import FormPage from "./FormPage";

const MODULE_CODE = "AG_AGENT_DEFINITION_MNG";

const statusText = {
  Enabled: { color: "success", text: "已启用" },
  Disabled: { color: "default", text: "已停用" },
  Archived: { color: "warning", text: "已归档" }
} as const;

const Index: React.FC = () => {
  const tableActionRef = useRef<ActionType>();
  const [importing, setImporting] = useState(false);
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

  const importPackage = async (file: File) => {
    if (importing) return Upload.LIST_IGNORE;
    if (!file.name.toLowerCase().endsWith(".json")) {
      message.error("请选择 Agent JSON 配置包");
      return Upload.LIST_IGNORE;
    }
    setImporting(true);
    try {
      await importAgent(await file.text());
      message.success("Agent 配置包已导入");
      tableActionRef.current?.reload();
    } catch (error) {
      message.error(error instanceof Error ? error.message : "Agent 配置包导入失败");
    } finally {
      setImporting(false);
    }
    return Upload.LIST_IGNORE;
  };

  return (
    <TableList
      moduleCode={MODULE_CODE}
      DynamicFormPage={FormPage}
      columnCustomizers={columnCustomizers}
      tableActionRef={tableActionRef}
      expendAction={() => (
        <Upload accept="application/json,.json" showUploadList={false} beforeUpload={importPackage}>
          <Button icon={<UploadOutlined />} loading={importing}>导入 Agent</Button>
        </Upload>
      )}
    />
  );
};

export default Index;
