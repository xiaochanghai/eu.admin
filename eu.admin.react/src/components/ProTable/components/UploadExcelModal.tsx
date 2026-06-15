import React from "react";
import { Modal } from "antd";
import { UploadExcel } from "@/components";
import { UploadExcelModalProps } from "../types";
import { useTranslation } from "react-i18next";

/**
 * Excel导入弹窗组件
 * 包装 UploadExcel 组件
 */
export const UploadExcelModal: React.FC<UploadExcelModalProps> = React.memo(
  ({ visible, moduleInfo, onCancel, onReload }) => {
    const { t } = useTranslation();
    if (!visible) return null;

    return (
      <Modal
        destroyOnHidden
        title={`${moduleInfo.moduleName}-${t("proTable.importTitle")}`}
        open={visible}
        mask={{ closable: false }}
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
