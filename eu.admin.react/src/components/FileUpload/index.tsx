import React, { useState, useEffect, useCallback } from "react";
import { Upload, Typography } from "antd";
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

const { Text } = Typography;

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

  /**
   * 获取文件下载URL
   */
  const getFileUrl = useCallback((id: string): string => {
    return `${VITE_USER_NODE_ENV === "development" ? baseURL : ""}/api/File/Download/${id}`;
  }, []);

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
    [propFileName, getFileUrl, t]
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
      return true;
    },
    [maxFileSize, t]
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
      if (!uploadFlag) return;
      uploadFlag = false;

      // 保存 loading 实例引用
      let hideLoading: (() => void) | undefined;

      try {
        if (!file.originFileObj) {
          uploadFlag = true;
          return;
        }

        // 上传前校验
        if (!beforeUpload(file.originFileObj)) {
          uploadFlag = true;
          return;
        }

        setLoading(true);
        hideLoading = message.loading(t("fileUpload.uploading"), 0) as unknown as () => void;

        const formData = new FormData();
        formData.append("file", file.originFileObj);
        formData.append("masterId", "");
        formData.append("filePath", filePath);

        // 执行上传
        const { Data, Success, Message } = await uploadFile("/api/File/Upload", formData);

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
        message.error(t("fileUpload.uploadFileFailed"));

      } finally {
        uploadFlag = true;
        setLoading(false);
      }
    },
    [filePath, beforeUpload, onChange, getFileUrl, t]
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
