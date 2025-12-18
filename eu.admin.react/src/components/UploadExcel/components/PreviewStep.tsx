import React, { memo, useMemo } from "react";
import { Button, Table, Descriptions, Space } from "antd";
import { Icon, Result } from "@/components";
import { ErrorItem, ImportColumn, ImportTemplateInfo, ImportType } from "../types";
import { ERROR_TABLE_COLUMNS, TABLE_CONFIG } from "../constants";

import type { DescriptionsProps } from "antd";
interface PreviewStepProps {
  errorList: ErrorItem[];
  importList: Record<string, any>[];
  importMasterList: Record<string, any>[];
  importColumns: ImportColumn[];
  templateInfo: ImportTemplateInfo;
  onTransferData: (type: ImportType) => void;
  onReset: () => void;
  showResetButton?: boolean;
}

export const PreviewStep: React.FC<PreviewStepProps> = memo(
  ({ errorList, importList, importMasterList, importColumns, templateInfo, onTransferData, onReset, showResetButton = true }) => {
    // 错误预览
    const errorPreview = useMemo(
      () => (
        <Space>
          <Result
            style={{ padding: 15 }}
            status="error"
            title="读取失败"
            subTitle={`${errorList.length}条错误信息`}
            extra={
              showResetButton
                ? [
                    <Button type="primary" key="reset" icon={<Icon name="ArrowUpOutlined" />} onClick={onReset}>
                      返回上一页
                    </Button>
                  ]
                : undefined
            }
          />
          <Table columns={ERROR_TABLE_COLUMNS} dataSource={errorList} {...TABLE_CONFIG} rowKey="Key" />
        </Space>
      ),
      [errorList, onReset, showResetButton]
    );

    // 数据预览
    const dataPreview = useMemo(
      () => (
        <Space orientation="vertical" size="small" style={{ display: "flex", paddingLeft: 10, paddingRight: 10 }}>
          <Result
            status="success"
            title="读取成功"
            // subTitle={`本次从Excel共读取数据如下`}
            extra={[
              <Button type="primary" key="append" icon={<Icon name="PlusOutlined" />} onClick={() => onTransferData("append")}>
                追加导入
              </Button>,
              templateInfo?.IsAllowOverride && (
                <Button key="override" danger icon={<Icon name="ImportOutlined" />} onClick={() => onTransferData("override")}>
                  覆盖导入
                </Button>
              ),
              showResetButton && (
                <Button type="primary" key="reset" icon={<Icon name="ArrowUpOutlined" />} onClick={onReset}>
                  返回上一页
                </Button>
              )
            ].filter(Boolean)}
          />
          <Descriptions items={importMasterList as DescriptionsProps["items"]} />
          <Table columns={importColumns} dataSource={importList} {...TABLE_CONFIG} rowKey="Key" />
        </Space>
      ),
      [importList, importColumns, templateInfo, onTransferData, onReset, showResetButton]
    );

    // 根据状态渲染对应内容
    if (errorList.length > 0) {
      return errorPreview;
    }

    if (importList.length > 0) {
      return dataPreview;
    }

    return null;
  }
);

PreviewStep.displayName = "PreviewStep";
