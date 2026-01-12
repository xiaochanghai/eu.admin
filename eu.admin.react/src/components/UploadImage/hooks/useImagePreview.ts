import { useState, useCallback } from "react";
import { UploadFile } from "antd/lib/upload/interface";

/**
 * 图片预览自定义 Hook
 * 处理图片预览弹窗的显示/隐藏逻辑
 */
export const useImagePreview = () => {
  const [previewVisible, setPreviewVisible] = useState<boolean>(false);
  const [previewTitle, setPreviewTitle] = useState<string | null>(null);
  const [previewImage, setPreviewImage] = useState<string>("");

  /**
   * 处理图片预览
   */
  const handlePreview = useCallback((file: UploadFile) => {
    setPreviewVisible(true);
    setPreviewTitle(file.name || "图片预览");
    setPreviewImage(file.url || "");
  }, []);

  /**
   * 关闭预览
   */
  const handleClosePreview = useCallback(() => {
    setPreviewVisible(false);
  }, []);

  return {
    previewVisible,
    previewTitle,
    previewImage,
    handlePreview,
    handleClosePreview
  };
};
