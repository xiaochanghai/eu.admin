import React from "react";
import { Modal } from "antd";
import { UploadExcel } from "@/components";
import { UploadExcelModalProps } from "../types";

/**
 * Excel导入弹窗组件
 * 包装 UploadExcel 组件
 */
export const UploadExcelModal: React.FC<UploadExcelModalProps> = React.memo(
  ({ visible, moduleInfo, onCancel, onReload }) => {
    if (!visible) return null;

    return (
      <Modal
        destroyOnClose
        title={`${moduleInfo.moduleName}-导入`}
        open={visible}
        maskClosable={false}
        width={1000}
        footer={null}
        onCancel={onCancel}
      >
        <UploadExcel moduleInfo={moduleInfo} onCancel={onCancel} onReload={onReload} />
      </Modal>
    );
  }
);

UploadExcelModal.displayName = "UploadExcelModal";
