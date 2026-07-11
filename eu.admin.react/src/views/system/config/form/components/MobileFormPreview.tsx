import { Card, Empty, Form } from "antd";
import { Element } from "@/components";

interface MobileFormPreviewProps {
  fields: any[];
}

const MobileFormPreview = ({ fields }: MobileFormPreviewProps) => {
  const visibleFields = fields
    .filter(field => field.HideInForm === false && field.ColumnMode !== "list")
    .sort((left, right) => (left.FormTaxisNo || 0) - (right.FormTaxisNo || 0));

  if (visibleFields.length === 0) {
    return <Empty description="暂无可显示的申请表单字段" />;
  }

  return (
    <Card size="small" title="移动端申请表单预览" style={{ maxWidth: 520, margin: "0 auto" }}>
      <Form layout="vertical">
        {visibleFields.map(field => (
          <div key={field.ID || field.DataIndex} style={{ width: "100%", marginBottom: 12 }}>
            <Element field={{ ...field, GridSpan: 100 }} />
          </div>
        ))}
      </Form>
    </Card>
  );
};

export default MobileFormPreview;
