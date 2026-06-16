import React, { useState, useEffect, useCallback } from "react";
import { Upload } from "antd";
import { message } from "@/hooks/useMessage";
import { uploadFile } from "@/api/modules/module";
import { Icon } from "@/components";
import { RcFile, UploadChangeParam, UploadFile } from "antd/lib/upload/interface";
import { useTranslation } from "react-i18next";

// 环境变量
const baseURL = import.meta.env.VITE_API_URL as string;
const VITE_USER_NODE_ENV = import.meta.env.VITE_USER_NODE_ENV as string;

// 上传状态标志，防止重复上传
let uploadFlag = true;

/**
 * 封面图组件属性接口
 */
interface ImageCoverProps {
  /** 主记录ID */
  value?: string;
  /** 禁用状态 */
  disabled?: boolean;
  /** 接受的文件类型 */
  accept?: string;
  /** 文件路径 */
  filePath?: string;
  /** 图片类型 */
  imageType?: string;
  /** 值变更回调 */
  onChange?: (fileId: string) => void;
}

/**
 * 封面图上传组件
 * 用于在表单中上传单张封面图片
 * @param props 组件属性
 */
const ImageCover: React.FC<ImageCoverProps> = props => {
  const { value, disabled = false, accept = ".png,.jpeg,.jpg", filePath = "equipment", imageType, onChange } = props;
  const { t } = useTranslation();

  // 组件状态
  const [loading, setLoading] = useState<boolean>(false);
  const [imageUrl, setImageUrl] = useState<string>("");
  const [fileId, setFileId] = useState<string>("");

  /**
   * 将文件转换为Base64格式
   * @param file 文件对象
   * @param callback 回调函数
   */
  const getBase64 = useCallback((file: RcFile, callback: (url: string) => void) => {
    const reader = new FileReader();
    reader.addEventListener("load", () => callback(reader.result as string));
    reader.readAsDataURL(file);
  }, []);

  /**
   * 获取图片URL
   */
  const loadImageUrl = useCallback(
    async (id: string) => {
      if (!id) {
        setImageUrl("");
        return;
      }

      try {
        // 根据文件ID构建图片URL
        const url = `${VITE_USER_NODE_ENV === "development" ? baseURL : ""}/api/File/Img/${id}`;
        setImageUrl(url);
        setFileId(id);
      } catch (error) {
        console.error("加载图片失败:", error);
        message.error(t("imageCover.loadFailed"));
      }
    },
    [t]
  );

  /**
   * 上传文件
   * @param fileInfo 上传的文件信息
   */
  const uploadFileAttachment = useCallback(
    async (fileInfo: UploadChangeParam<UploadFile>) => {
      // 如果是删除操作，直接返回
      if (fileInfo.file.status === "removed") return;

      // 防止重复上传
      if (!uploadFlag) return false;
      uploadFlag = false;

      try {
        // 如果没有文件对象，直接返回
        if (!fileInfo.file.originFileObj) {
          uploadFlag = true;
          return;
        }

        // 转换为Base64并显示预览
        getBase64(fileInfo.file.originFileObj, (url: string) => {
          setImageUrl(url);
          setLoading(true);
        });

        // 准备上传
        message.loading(t("imageCover.uploading"), 0);
        const formData = new FormData();

        formData.append("file", fileInfo.file.originFileObj);
        formData.append("masterId", "");
        formData.append("filePath", filePath);
        formData.append("imageType", imageType ?? filePath);

        // 执行上传
        const { Data, Success, Message } = await uploadFile("/api/File/UploadImage", formData);

        if (Success && Data) {
          message.success(Message || t("imageCover.uploadSuccess"));
          setFileId(Data.ID || Data);

          // 通知父组件文件ID已变更
          onChange?.(Data.ID || Data);
        } else {
          message.error(Message || t("imageCover.uploadFailed"));
          setImageUrl("");
        }
      } catch (error) {
        console.error("上传图片失败:", error);
        message.error(t("imageCover.uploadImageFailed"));
        setImageUrl("");
      } finally {
        uploadFlag = true;
        message.destroy();
        setLoading(false);
      }
    },
    [filePath, imageType, getBase64, onChange, t]
  );

  // 监听外部value变化
  useEffect(() => {
    if (value !== fileId) loadImageUrl(value || "");
  }, [value, fileId, loadImageUrl]);

  return (
    <Upload accept={accept} listType="picture-card" showUploadList={false} onChange={uploadFileAttachment} disabled={disabled}>
      {imageUrl ? (
        <img src={imageUrl} alt={t("imageCover.coverImageAlt")} style={{ width: "100%", height: "100%", objectFit: "cover" }} />
      ) : (
        <div>
          <Icon name={loading ? "LoadingOutlined" : "PlusOutlined"} className="font-size24" />
          <div className="ant-upload-text">{t("imageCover.uploadCover")}</div>
        </div>
      )}
    </Upload>
  );
};

// 添加组件显示名称，方便调试
ImageCover.displayName = "ImageCover";

// 使用React.memo优化组件性能，避免不必要的重渲染
export default React.memo(ImageCover);
