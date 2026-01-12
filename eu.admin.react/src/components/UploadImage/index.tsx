import React, { useCallback } from "react";

// Hooks
import { useImageUpload, useImageData, useImagePreview } from "./hooks";

// Components
import { UniqueUpload, MultipleUpload } from "./components";

// Types and Constants
import { UploadImageProps } from "./types";
import { DEFAULT_ACCEPT, DEFAULT_FILE_PATH } from "./constants";

/**
 * 图片上传组件
 * 支持单图片和多图片上传模式
 *
 * @example
 * // 单图上传
 * <UploadImage Id={id} isUnique />
 *
 * @example
 * // 多图上传
 * <UploadImage Id={id} filePath="products" imageType="gallery" />
 */
const UploadImage: React.FC<UploadImageProps> = React.memo(props => {
  const {
    Id,
    accept = DEFAULT_ACCEPT,
    isUnique = false,
    filePath = DEFAULT_FILE_PATH,
    masterTable,
    masterColumn,
    imageType,
    ImageUrl
  } = props;

  // ==================== 自定义 Hooks ====================

  // 图片数据管理
  const { fileList, imageUrl: dataImageUrl, masterId, getImageData, onRemove } = useImageData({
    Id,
    ImageUrl,
    imageType,
    filePath,
    isUnique
  });

  // 图片上传
  const { loading, imageUrl: uploadImageUrl, uploadFileAttachment } = useImageUpload({
    masterId,
    filePath,
    imageType,
    masterTable,
    masterColumn,
    isUnique,
    onUploadSuccess: getImageData
  });

  // 图片预览
  const { handlePreview } = useImagePreview();

  // ==================== 图片URL管理 ====================

  // 合并两个来源的图片URL
  const finalImageUrl = uploadImageUrl || dataImageUrl;

  // 上传成功后更新数据图片URL
  const handleUploadChange = useCallback(
    async (fileInfo: any) => {
      await uploadFileAttachment(fileInfo);
      // 上传时的预览URL已在 uploadFileAttachment 中设置
    },
    [uploadFileAttachment]
  );

  // ==================== 渲染 ====================

  return (
    <>
      {isUnique ? (
        <UniqueUpload accept={accept} imageUrl={finalImageUrl} loading={loading} masterId={masterId} onUpload={handleUploadChange} />
      ) : (
        <MultipleUpload
          accept={accept}
          loading={loading}
          masterId={masterId}
          fileList={fileList}
          onUpload={handleUploadChange}
          onPreview={handlePreview}
          onRemove={onRemove}
        />
      )}
    </>
  );
});

// 添加组件显示名称，方便调试
UploadImage.displayName = "UploadImage";

export default UploadImage;
