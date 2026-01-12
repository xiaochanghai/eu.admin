import React from "react";
import { Upload } from "antd";
import { Icon } from "@/components";
import { MultipleUploadProps } from "../types";
import { ImagePreviewModal } from "./ImagePreviewModal";
import { useImagePreview } from "../hooks";

/**
 * 多图上传组件
 * 用于上传多张图片的场景
 */
export const MultipleUpload: React.FC<MultipleUploadProps> = React.memo(
  ({ accept, loading, masterId, fileList, onUpload, onPreview, onRemove }) => {
    const { previewVisible, previewTitle, previewImage, handlePreview, handleClosePreview } = useImagePreview();

    const handleFilePreview = (file: any) => {
      handlePreview(file);
      onPreview(file);
    };

    return (
      <>
        <div>
          <Upload
            accept={accept}
            listType="picture-card"
            fileList={fileList}
            onChange={onUpload}
            onPreview={handleFilePreview}
            onRemove={onRemove}
            disabled={!masterId}
          >
            <div>
              <Icon name={loading ? "LoadingOutlined" : "PlusOutlined"} className="font-size24" />
              <div className="ant-upload-text">图片上传</div>
            </div>
          </Upload>
        </div>
        <ImagePreviewModal visible={previewVisible} title={previewTitle} imageUrl={previewImage} onCancel={handleClosePreview} />
      </>
    );
  }
);

MultipleUpload.displayName = "MultipleUpload";
