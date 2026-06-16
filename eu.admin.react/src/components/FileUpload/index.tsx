import React, { useState, useEffect, useCallback } from "react";
import { Upload, Button, Space, Typography } from "antd";
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
  /** 自定义按钮文字 */
  buttonText?: string;
  /** 是否显示文件链接 */
  showFileLink?: boolean;
}

/**
 * 文件上传组件
 * 用于在表单中上传单个文件
 */
const FileUpload: React.FC<FileUploadProps> = props => {
  const {
    value,
    fileName: propFileName,
    disabled = false,
    accept,
    filePath = "default",
    maxFileSize = 50,
    onChange,
    buttonText,
    showFileLink = true
  } = props;
  const { t } = useTranslation();

  // 组件状态
  const [loading, setLoading] = useState<boolean>(false);
  const [fileId, setFileId] = useState<string>("");
  const [fileName, setFileName] = useState<string>("");
  const [fileList, setFileList] = useState<UploadFile[]>([]);

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
        setFileList([]);
        return;
      }

      try {
        setFileId(id);
        const name = propFileName || t("fileUpload.defaultFileName");
        setFileName(name);
        setFileList([
          {
            uid: id,
            name,
            status: "done",
            url: getFileUrl(id)
          }
        ]);
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
      // 检查文件大小
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
   * 上传文件
   */
  const handleUpload = useCallback(
    async (fileInfo: UploadChangeParam<UploadFile>) => {
      const { file } = fileInfo;

      // 如果是删除操作
      if (file.status === "removed") {
        setFileId("");
        setFileName("");
        setFileList([]);
        onChange?.("", "");
        return;
      }

      // 防止重复上传
      if (!uploadFlag) return;
      uploadFlag = false;

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
        message.loading(t("fileUpload.uploading"), 0);

        const formData = new FormData();
        formData.append("file", file.originFileObj);
        formData.append("masterId", "");
        formData.append("filePath", filePath);

        // 执行上传
        const { Data, Success, Message } = await uploadFile("/api/File/Upload", formData);

        if (Success && Data) {
          const newFileId = Data.ID || Data;
          const newFileName = file.originFileObj.name;
          message.success(Message || t("fileUpload.uploadSuccess"));
          setFileId(newFileId);
          setFileName(newFileName);
          setFileList([
            {
              uid: newFileId,
              name: newFileName,
              status: "done",
              url: getFileUrl(newFileId)
            }
          ]);
          onChange?.(newFileId, newFileName);
        } else {
          message.error(Message || t("fileUpload.uploadFailed"));
          setFileList([]);
        }
      } catch (error) {
        console.error("上传文件失败:", error);
        message.error(t("fileUpload.uploadFileFailed"));
        setFileList([]);
      } finally {
        uploadFlag = true;
        message.destroy();
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
   * 渲染已上传文件信息
   */
  const renderFileInfo = () => {
    if (!fileId || !fileName) return null;

    return (
      <Space style={{ marginTop: 8 }}>
        <Icon name="FileOutlined" style={{ color: "#1890ff" }} />
        {showFileLink ? (
          <a href={getFileUrl(fileId)} target="_blank" rel="noopener noreferrer">
            {fileName}
          </a>
        ) : (
          <Text>{fileName}</Text>
        )}
      </Space>
    );
  };

  return (
    <div>
      <Upload
        accept={accept}
        maxCount={1}
        fileList={fileList}
        onChange={handleUpload}
        disabled={disabled || !!fileId}
        beforeUpload={beforeUpload}
      >
        <Button icon={<Icon name="UploadOutlined" />} loading={loading} disabled={disabled || !!fileId}>
          {buttonText || t("fileUpload.uploadButton")}
        </Button>
      </Upload>
      {renderFileInfo()}
    </div>
  );
};

FileUpload.displayName = "FileUpload";

export default React.memo(FileUpload);
