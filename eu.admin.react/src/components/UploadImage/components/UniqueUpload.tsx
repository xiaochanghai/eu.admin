import React from "react";
import { Upload, Space } from "antd";
import { Icon } from "@/components";
import { UniqueUploadProps } from "../types";

/**
 * 单图上传组件
 * 用于上传单张图片的场景
 */
export const UniqueUpload: React.FC<UniqueUploadProps> = React.memo(({ accept, imageUrl, loading, masterId, onUpload }) => {
  return (
    <Space style={{ justifyContent: "flex-start", float: "left" }}>
      <Upload accept={accept} listType="picture-card" showUploadList={false} onChange={onUpload} disabled={!masterId}>
        {imageUrl ? (
          <img src={imageUrl} alt="上传图片" style={{ width: "100%" }} />
        ) : (
          <div>
            <Icon name={loading ? "LoadingOutlined" : "PlusOutlined"} className="font-size24" />
            <div className="ant-upload-text">图片上传</div>
          </div>
        )}
      </Upload>
    </Space>
  );
});

UniqueUpload.displayName = "UniqueUpload";
