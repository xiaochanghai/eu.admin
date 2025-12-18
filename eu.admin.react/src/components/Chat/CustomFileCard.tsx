import React from "react";
import { Button, Progress } from "antd";

import { Icon } from "@/components";
import { downloadFile, formatFileSize } from "@/utils";
import type { UploadFile } from "antd/es/upload/interface";
import { Typography } from "antd";
const { Text } = Typography;
interface CustomFileCardProps {
  item: UploadFile;
  showDownload?: boolean;
  onDownload?: (file: UploadFile) => void;
}

// 文件类型图标映射
const getFileIcon = (fileName: string) => {
  const ext = fileName.split(".").pop()?.toLowerCase();

  switch (ext) {
    case "jpg":
    case "jpeg":
    case "png":
    case "gif":
    case "webp":
      return <Icon name="PictureOutlined" style={{ fontSize: 16, color: "#1677ff" }} />;
    case "mp4":
    case "avi":
    case "mov":
    case "wmv":
      return <Icon name="VideoCameraOutlined" style={{ fontSize: 16, color: "#1677ff" }} />;
    case "mp3":
    case "wav":
    case "flac":
      return <Icon name="AudioOutlined" style={{ fontSize: 16, color: "#1677ff" }} />;
    case "xlsx":
      return <Icon name="FileExcelFilled" style={{ fontSize: 32, color: "rgb(34, 179, 94)" }} />;
    default:
      return <Icon name="FileOutlined" style={{ fontSize: 16, color: "#1677ff" }} />;
  }
};

// // 格式化文件大小
// const formatFileSize = (size: number): string => {
//   if (size < 1024) return `${size} B`;
//   if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
//   if (size < 1024 * 1024 * 1024) return `${(size / (1024 * 1024)).toFixed(1)} MB`;
//   return `${(size / (1024 * 1024 * 1024)).toFixed(1)} GB`;
// };

const CustomFileCard: React.FC<CustomFileCardProps> = ({ item, showDownload = true, onDownload }) => {
  const handleDownload = () => {
    if (onDownload) {
      onDownload(item);
    } else if (item.uid && item.name) {
      downloadFile(item.uid, item.name);
    }
  };

  const isUploading = item.status === "uploading";
  const isError = item.status === "error";

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        padding: 12,
        backgroundColor: "rgba(0,0,0,0.06)",
        // border: "1px solid #d9d9d9",
        borderRadius: "6px",
        minHeight: "48px",
        width: "100%",
        maxWidth: "320px",
        gap: "8px",
        ...(isError && {
          backgroundColor: "#fff2f0",
          borderColor: "#ffccc7"
        })
      }}
    >
      {/* 文件图标或缩略图 */}
      <div style={{ flexShrink: 0, display: "flex", alignItems: "center" }}>
        {item.thumbUrl ? (
          <img
            src={item.thumbUrl}
            alt={item.name}
            style={{
              width: 32,
              height: 32,
              objectFit: "cover",
              borderRadius: 4
            }}
          />
        ) : (
          <div
            style={{
              width: 32,
              height: 32,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              backgroundColor: "#f0f0f0",
              borderRadius: 4
            }}
          >
            {getFileIcon(item.name || "")}
          </div>
        )}
      </div>

      {/* 文件信息 */}
      <div style={{ flex: 1, minWidth: 0, overflow: "hidden" }}>
        <div
          style={{
            fontSize: "14px",
            fontWeight: 500,
            color: isError ? "#ff4d4f" : "#262626",
            whiteSpace: "nowrap",
            overflow: "hidden",
            textOverflow: "ellipsis",
            lineHeight: "20px"
          }}
          title={item.name}
        >
          {item.name}
        </div>

        {item.size && (
          <div
            style={{
              fontSize: "12px",
              color: "#8c8c8c",
              lineHeight: "16px",
              marginTop: "2px"
            }}
          >
            {formatFileSize(item.size)}
          </div>
        )}

        {/* 上传进度 */}
        {isUploading && item.percent !== undefined && (
          <Progress
            percent={item.percent}
            size="small"
            status={isError ? "exception" : "active"}
            style={{
              marginTop: 4,
              fontSize: 12
            }}
            strokeWidth={2}
            showInfo={false}
          />
        )}

        {/* 错误信息 */}
        {isError && (
          <Text type="danger" style={{ fontSize: 12, display: "block", marginTop: 4 }}>
            上传失败
          </Text>
        )}
      </div>

      {/* 下载按钮 */}
      {showDownload && !isUploading && !isError && (
        <Button
          type="text"
          icon={<Icon name="DownloadOutlined" />}
          onClick={handleDownload}
          size="small"
          title="下载文件"
          style={{ flexShrink: 0 }}
        />
      )}
    </div>
  );
};

export default CustomFileCard;
