import React from "react";
import { Modal } from "antd";
import { ModuleLog } from "@/components";
import { RecordLogModalProps } from "../types";

/**
 * 记录日志弹窗组件
 * 显示记录的变更历史
 */
export const RecordLogModal: React.FC<RecordLogModalProps> = React.memo(({ visible, data, onCancel }) => {
  return (
    <Modal title="日志" open={visible} width={1000} footer={null} onCancel={onCancel}>
      <ModuleLog log={data} />
    </Modal>
  );
});

RecordLogModal.displayName = "RecordLogModal";
