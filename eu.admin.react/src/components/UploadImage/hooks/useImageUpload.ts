import { useState, useCallback, useRef } from "react";
import { UploadChangeParam, UploadFile } from "antd/lib/upload/interface";
import { message } from "@/hooks/useMessage";
import { uploadFile } from "@/api/modules/module";
import { getBase64 } from "../utils";
import { API_PATHS, UPLOAD_MESSAGES } from "../constants";

/**
 * 上传图片配置参数
 */
interface UseImageUploadProps {
  masterId: string;
  filePath: string;
  imageType?: string;
  masterTable?: string;
  masterColumn?: string;
  isUnique: boolean;
  onUploadSuccess?: () => void;
}

/**
 * 上传图片自定义 Hook
 * 处理图片上传的核心逻辑
 */
export const useImageUpload = (props: UseImageUploadProps) => {
  const { masterId, filePath, imageType, masterTable, masterColumn, isUnique, onUploadSuccess } = props;

  const [loading, setLoading] = useState<boolean>(false);
  const [imageUrl, setImageUrl] = useState<string>("");

  // 使用 ref 代替全局变量
  const uploadingRef = useRef<boolean>(false);

  /**
   * 上传文件附件
   */
  const uploadFileAttachment = useCallback(
    async (fileInfo: UploadChangeParam<UploadFile>) => {
      // 如果是删除操作，直接返回
      if (fileInfo.file.status === "removed") return;

      // 防止重复上传
      if (uploadingRef.current) return false;

      // 如果没有文件对象，直接返回
      if (!fileInfo.file.originFileObj) {
        return;
      }

      uploadingRef.current = true;

      try {
        // 转换为Base64并显示预览
        const base64 = await getBase64(fileInfo.file.originFileObj);
        setImageUrl(base64);
        setLoading(true);

        // 准备上传
        message.loading(UPLOAD_MESSAGES.UPLOADING, 0);
        const formData = new FormData();

        formData.append("file", fileInfo.file.originFileObj);
        formData.append("masterId", masterId);
        formData.append("filePath", filePath);
        formData.append("imageType", imageType ?? "");
        formData.append("masterTable", masterTable ?? "");
        formData.append("masterColumn", masterColumn ?? "");
        formData.append("isUnique", isUnique ? "true" : "false");

        // 执行上传
        const { Success, Message } = await uploadFile(API_PATHS.UPLOAD_IMAGE, formData);

        if (Success) {
          message.success(Message || UPLOAD_MESSAGES.SUCCESS);
          // 调用成功回调
          onUploadSuccess?.();
        } else {
          message.error(Message || UPLOAD_MESSAGES.FAILED);
        }
      } catch (error) {
        console.error("上传图片失败:", error);
        message.error(UPLOAD_MESSAGES.FAILED);
      } finally {
        uploadingRef.current = false;
        message.destroy();
        setLoading(false);
      }
    },
    [masterId, filePath, imageType, masterTable, masterColumn, isUnique, onUploadSuccess]
  );

  return {
    loading,
    imageUrl,
    setImageUrl,
    uploadFileAttachment
  };
};
