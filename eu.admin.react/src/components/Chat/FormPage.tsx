import React from "react";
import FormPage1 from "@/views/system/common/components/FormPage";
import { Card } from "antd";

interface MarkdownProps {
  moduleCode: string;
}

const FormPage: React.FC<MarkdownProps> = React.memo(({ moduleCode }) => {
  return (
    <Card style={{ border: 0 }}>
      <FormPage1
        moduleCode={moduleCode}
        displayToolBar
        // Id={id}
        // masterId={masterId}
        // IsView={isView}
        // onReload={() => tableRef.current?.reload()}
        // onClose={onClose}
        // formPageRef={formPageRef}
        // onDisabled={(value: boolean) => setDisabled(value)}
      />
    </Card>
  );
});

export default FormPage;
