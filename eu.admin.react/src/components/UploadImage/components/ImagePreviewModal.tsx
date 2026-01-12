import React from "react";
import { Modal } from "antd";
import { ImagePreviewModalProps } from "../types";
import { PREVIEW_MODAL_CONFIG } from "../constants";

/**
 * 图片预览 Modal 组件
 * 用于展示大图预览
 */
export const ImagePreviewModal: React.FC<ImagePreviewModalProps> = React.memo(({ visible, title, imageUrl, onCancel }) => {
  return (
    <Modal width={PREVIEW_MODAL_CONFIG.width} open={visible} title={title} footer={PREVIEW_MODAL_CONFIG.footer} onCancel={onCancel}>
      <img alt="预览图片" style={{ width: "100%" }} src={imageUrl} />
    </Modal>
  );
});

ImagePreviewModal.displayName = "ImagePreviewModal";
