import { UploadFile } from "antd/lib/upload/interface";

/**
 * 文件信息接口
 */
export interface FileInfo {
  /** 文件ID */
  ID: string;
  /** 原始文件名 */
  OriginalFileName: string;
  /** 文件名 */
  FileName: string;
  /** 文件扩展名 */
  FileExt: string;
}

/**
 * 上传图片组件属性接口
 */
export interface UploadImageProps {
  /** 主记录ID */
  Id?: string | null;
  /** 接受的文件类型 */
  accept?: string;
  /** 是否唯一图片 */
  isUnique?: boolean;
  /** 文件路径 */
  filePath?: string;
  /** 主表名 */
  masterTable?: string;
  /** 主表列名 */
  masterColumn?: string;
  /** 图片类型 */
  imageType?: string;
  /** 图片URL */
  ImageUrl?: string;
}

/**
 * 图片预览 Modal 属性
 */
export interface ImagePreviewModalProps {
  visible: boolean;
  title: string | null;
  imageUrl: string;
  onCancel: () => void;
}

/**
 * 单图上传组件属性
 */
export interface UniqueUploadProps {
  accept: string;
  imageUrl: string;
  loading: boolean;
  masterId: string;
  onUpload: (fileInfo: any) => void;
}

/**
 * 多图上传组件属性
 */
export interface MultipleUploadProps {
  accept: string;
  loading: boolean;
  masterId: string;
  fileList: UploadFile[];
  onUpload: (fileInfo: any) => void;
  onPreview: (file: UploadFile) => void;
  onRemove: (file: UploadFile) => Promise<boolean>;
}
