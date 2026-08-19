import React, { useMemo } from "react";
import { Tag } from "antd";
import { TableList } from "@/components";
import type { ColumnCustomizerMap } from "@/components/ProTable/columnCustomizers";
import FormPage from "./FormPage";

const MODULE_CODE = "AG_SKILL_MNG";

const Index: React.FC = () => {
  const columnCustomizers = useMemo<ColumnCustomizerMap>(
    () => ({
      Status: original => ({
        ...original,
        render: (_, record) => {
          const active = record.Status === "Active";
          return <Tag color={active ? "success" : "warning"}>{active ? "有效" : "已归档"}</Tag>;
        }
      }),
      CurrentPublishedLabel: original => ({
        ...original,
        renderText: value => (value ? `v${value}` : "未发布")
      })
    }),
    []
  );

  return <TableList moduleCode={MODULE_CODE} DynamicFormPage={FormPage} columnCustomizers={columnCustomizers} />;
};

export default Index;
