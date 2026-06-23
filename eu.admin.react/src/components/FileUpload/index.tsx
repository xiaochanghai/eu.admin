import React, { useState, useEffect, useCallback, useRef } from "react";
import { Upload, Typography } from "antd";
import { message } from "@/hooks/useMessage";
import { uploadFile } from "@/api/modules/module";
import { Icon } from "@/components";
import { RcFile, UploadChangeParam, UploadFile } from "antd/lib/upload/interface";
import { useTranslation } from "react-i18next";

const { Text } = Typography;
const CHUNK_SIZE = 5 * 1024 * 1024;

const createChunkUploadId = () => `${Date.now()}-${Math.random().toString(36).slice(2)}`;

const isAcceptedFile = (file: RcFile, accept?: string) => {
  if (!accept) return true;

  const acceptedTypes = accept
    .split(",")
    .map(item => item.trim().toLowerCase())
    .filter(Boolean);

  if (acceptedTypes.length === 0) return true;

  const fileName = file.name.toLowerCase();
  const fileType = file.type.toLowerCase();

  return acceptedTypes.some(type => {
    if (type.startsWith(".")) return fileName.endsWith(type);
    if (type.endsWith("/*")) return fileType.startsWith(type.slice(0, -1));
    return fileType === type;
  });
};

const getUploadErrorMessage = (error: unknown, fallback: string) => {
  if (error && typeof error === "object") {
    const resultMessage = (error as { Message?: string }).Message;
    const responseMessage = (error as { response?: { data?: { Message?: string } } }).response?.data?.Message;
    if (resultMessage) return resultMessage;
    if (responseMessage) return responseMessage;
  }

  if (error instanceof Error && error.message) return error.message;

  return fallback;
};

/**
 * 文件上传组件属性接口
 */
interface FileUploadProps {
  /** 文件ID (表单值) */
  value?: string;
  /** 文件名 (用于回显) */
  fileName?: string;
  /** 禁用状态 */
  disabled?: boolean;
  /** 接受的文件类型，如 ".pdf,.doc,.docx" */
  accept?: string;
  /** 文件存储路径 */
  filePath?: string;
  /** 最大文件大小 (MB) */
  maxFileSize?: number;
  /** 值变更回调 */
  onChange?: (fileId: string, fileName?: string) => void;
}

/**
 * 文件上传组件
 * 用于在表单中上传单个文件，采用 picture-card 样式
 */
const FileUpload: React.FC<FileUploadProps> = props => {
  const { value, fileName: propFileName, disabled = false, accept, filePath = "default", maxFileSize = 50, onChange } = props;
  const { t } = useTranslation();

  // 组件状态
  const [loading, setLoading] = useState<boolean>(false);
  const [fileId, setFileId] = useState<string>("");
  const [fileName, setFileName] = useState<string>("");
  const uploadFlagRef = useRef(true);

  /**
   * 加载文件信息
   */
  const loadFileInfo = useCallback(
    async (id: string) => {
      if (!id) {
        setFileId("");
        setFileName("");
        return;
      }

      try {
        setFileId(id);
        const name = propFileName || t("fileUpload.defaultFileName");
        setFileName(name);

      } catch (error) {
        console.error("加载文件失败:", error);
        message.error(t("fileUpload.loadFailed"));
      }
    },
    [propFileName, t]
  );

  /**
   * 上传前校验
   */
  const beforeUpload = useCallback(
    (file: RcFile): boolean => {
      const fileSizeMB = file.size / 1024 / 1024;
      if (fileSizeMB > maxFileSize) {
        message.error(t("fileUpload.fileSizeExceeded").replace("{size}", String(maxFileSize)));
        return false;
      }

      if (!isAcceptedFile(file, accept)) {
        message.error(t("fileUpload.fileTypeNotAllowed").replace("{accept}", accept || ""));
        return false;
      }

      return true;
    },
    [accept, maxFileSize, t]
  );

  /**
   * 删除已上传文件
   */
  const handleDelete = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      setFileId("");
      setFileName("");
      onChange?.("", "");
    },
    [onChange]
  );

  const uploadChunkFile = useCallback(
    async (file: RcFile) => {
      const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
      const uploadId = createChunkUploadId();
      let fileId: any = null;

      for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
        const start = chunkIndex * CHUNK_SIZE;
        const end = Math.min(file.size, start + CHUNK_SIZE);
        const chunk = file.slice(start, end);
        const formData = new FormData();

        formData.append("file", chunk, file.name);
        formData.append("fileName", file.name);
        formData.append("chunkIndex", String(chunkIndex));
        formData.append("totalChunks", String(totalChunks));
        formData.append("id", uploadId);
        formData.append("masterId", "");
        formData.append("filePath", filePath);

        const { Data, Success, Message } = await uploadFile("/api/File/UploadChunk", formData);
        if (!Success) throw new Error(Message || t("fileUpload.uploadFailed"));
        if (Data) fileId = Data.ID || Data;
      }

      return fileId;
    },
    [filePath, t]
  );

  /**
   * 上传文件
   */
  const handleUpload = useCallback(
    async (fileInfo: UploadChangeParam<UploadFile>) => {
      const { file } = fileInfo;

      // 如果是删除操作
      if (file.status === "removed") {
        setFileId("");
        setFileName("");
        onChange?.("", "");
        return;
      }

      // 防止重复上传
      if (!uploadFlagRef.current) return;
      uploadFlagRef.current = false;

      // 保存 loading 实例引用
      let hideLoading: (() => void) | undefined;

      try {
        if (!file.originFileObj) {
          uploadFlagRef.current = true;
          return;
        }

        // 上传前校验
        if (!beforeUpload(file.originFileObj)) {
          uploadFlagRef.current = true;
          return;
        }

        setLoading(true);
        hideLoading = message.loading(t("fileUpload.uploading"), 0) as unknown as () => void;

        const isChunkUpload = file.originFileObj.size > CHUNK_SIZE;
        let Data: any = null;
        let Success = true;
        let Message = "";

        if (isChunkUpload) {
          Data = await uploadChunkFile(file.originFileObj);
          Success = !!Data;
        } else {
          const formData = new FormData();
          formData.append("file", file.originFileObj);
          formData.append("masterId", "");
          formData.append("filePath", filePath);

          // 执行上传
          const result = await uploadFile("/api/File/Upload", formData);
          Data = result.Data;
          Success = result.Success;
          Message = result.Message;
        }

        if (Success && Data) {
          const newFileId = Data.ID || Data;
          const newFileName = file.originFileObj.name;
          hideLoading?.();
          message.success(Message || t("fileUpload.uploadSuccess"));
          setFileId(newFileId);
          setFileName(newFileName);

          onChange?.(newFileId, newFileName);
        } else {
          hideLoading?.();
          message.error(Message || t("fileUpload.uploadFailed"));
        }
      } catch (error) {
        hideLoading?.();
        console.error("上传文件失败:", error);
        message.error(getUploadErrorMessage(error, t("fileUpload.uploadFileFailed")));

      } finally {
        uploadFlagRef.current = true;
        setLoading(false);
      }
    },
    [filePath, beforeUpload, onChange, t, uploadChunkFile]
  );

  // 监听外部value变化
  useEffect(() => {
    if (value !== fileId) loadFileInfo(value || "");
  }, [value, fileId, loadFileInfo]);

  // 同步外部fileName变化
  useEffect(() => {
    if (propFileName && propFileName !== fileName) {
      setFileName(propFileName);
    }
  }, [propFileName, fileName]);

  /**
   * 截断过长的文件名
   */
  const truncateFileName = (name: string, maxLength = 10) => {
    if (name.length <= maxLength) return name;
    const ext = name.lastIndexOf(".") > -1 ? name.slice(name.lastIndexOf(".")) : "";
    const baseName = name.lastIndexOf(".") > -1 ? name.slice(0, name.lastIndexOf(".")) : name;
    const truncateLen = maxLength - ext.length - 1;
    return `${baseName.slice(0, truncateLen)}...${ext}`;
  };

  return (
    <Upload
      accept={accept}
      listType="picture-card"
      showUploadList={false}
      onChange={handleUpload}
      disabled={disabled || !!fileId}
      beforeUpload={beforeUpload}
    >
      {fileId && fileName ? (
        <div style={{ position: "relative", width: "100%", height: "100%", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", padding: 8 }}>
          {/* 右上角删除按钮 */}
          {!disabled && (
            <span
              onClick={handleDelete}
              style={{
                position: "absolute",
                top: 4,
                right: 4,
                cursor: "pointer",
                backgroundColor: "rgba(0, 0, 0, 0.5)",
                borderRadius: "50%",
                padding: 4,
                display: "flex",
                alignItems: "center",
                justifyContent: "center"
              }}
            >
              <Icon name="DeleteOutlined" style={{ color: "#fff", fontSize: 14 }} />
            </span>
          )}
          {/* 文件图标 */}
          <Icon name="FileOutlined" style={{ fontSize: 32, color: "#1890ff" }} />
          {/* 文件名 */}
          <Text
            style={{
              marginTop: 8,
              fontSize: 12,
              textAlign: "center",
              maxWidth: "100%",
              overflow: "hidden",
              textOverflow: "ellipsis",
              whiteSpace: "nowrap"
            }}
            title={fileName}
          >
            {truncateFileName(fileName)}
          </Text>
        </div>
      ) : (
        <div>
          <Icon name={loading ? "LoadingOutlined" : "PlusOutlined"} style={{ fontSize: 24 }} />
          <div className="ant-upload-text">{t("fileUpload.uploadButton")}</div>
        </div>
      )}
    </Upload>
  );
};

FileUpload.displayName = "FileUpload";

export default React.memo(FileUpload);
