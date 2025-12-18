import React, { memo } from "react";
import { Upload, Form, Row, Col, Button, Space } from "antd";
import { Icon } from "@/components";
import { ImportTemplateInfo } from "../types";
import { FORM_LAYOUTS, UPLOAD_INSTRUCTIONS, NOTICE_ITEMS } from "../constants";

const FormItem = Form.Item;

interface UploadStepProps {
  templateInfo: ImportTemplateInfo;
  moduleName: string;
  onUpload: (info: any) => void;
  onDownload: (fileId: string, templateName: string) => void;
  uploading: boolean;
}

export const UploadStep: React.FC<UploadStepProps> = memo(({ templateInfo, moduleName, onUpload, onDownload, uploading }) => {
  const templateDownloadLink = templateInfo.FileId ? (
    <a
      onClick={() => onDownload(templateInfo.FileId!, templateInfo.TemplateName)}
      style={{ cursor: "pointer", color: "#1890ff" }}
    >
      {moduleName} 导入模板
    </a>
  ) : (
    <span style={{ color: "#999" }}>暂无模板</span>
  );

  return (
    <Form {...FORM_LAYOUTS.default} style={{ marginTop: 20 }}>
      <Row gutter={24} justify="center">
        <Col span={24}>
          <FormItem label="Excel文件：">
            <Space>
              <Upload
                accept=".xlsx,.xls"
                action=""
                showUploadList={false}
                beforeUpload={() => false}
                onChange={onUpload}
                disabled={uploading}
              >
                <Button type="primary" icon={<Icon name="UploadOutlined" />} loading={uploading}>
                  {uploading ? "上传中..." : "点击上传Excel文件"}
                </Button>
              </Upload>
            </Space>
          </FormItem>
        </Col>

        <Col span={24}>
          <FormItem {...FORM_LAYOUTS.responsive} label="导入步骤：">
            <div style={{ marginTop: 5 }}>1、下载导入模板：{templateDownloadLink}</div>
            {UPLOAD_INSTRUCTIONS.map((instruction, index) => (
              <div key={index} style={{ marginTop: 10 }}>
                {index + 2}、{instruction}
              </div>
            ))}
          </FormItem>
        </Col>

        <Col span={24}>
          <FormItem {...FORM_LAYOUTS.responsive} label="注意事项：">
            {NOTICE_ITEMS.map((notice, index) => (
              <div key={index} style={{ marginTop: index === 0 ? 5 : 10 }}>
                {index + 1}、{notice}
              </div>
            ))}
          </FormItem>
        </Col>
      </Row>
    </Form>
  );
});

UploadStep.displayName = "UploadStep";
