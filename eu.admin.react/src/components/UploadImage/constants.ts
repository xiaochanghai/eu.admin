/**
 * UploadImage 常量配置
 */

/**
 * 默认接受的图片文件类型
 */
export const DEFAULT_ACCEPT = ".png,.jpeg,.jpg";

/**
 * 默认文件路径
 */
export const DEFAULT_FILE_PATH = "material";

/**
 * API 路径常量
 */
export const API_PATHS = {
  /** 获取文件列表 */
  GET_FILE_LIST: "/api/File/GetFileList",
  /** 上传图片 */
  UPLOAD_IMAGE: "/api/File/UploadImage",
  /** 获取图片 */
  GET_IMAGE: "/api/File/Img",
  /** 通过URL获取图片 */
  GET_BY_URL: "/api/File/GetByUrl",
  /** 删除文件 */
  DELETE_FILE: "/api/File"
} as const;

/**
 * 上传状态消息
 */
export const UPLOAD_MESSAGES = {
  UPLOADING: "上传中..",
  SUCCESS: "上传成功",
  FAILED: "上传失败",
  DELETE_SUCCESS: "删除成功",
  DELETE_FAILED: "删除图片失败",
  GET_DATA_FAILED: "获取图片数据失败"
} as const;

/**
 * 预览 Modal 配置
 */
export const PREVIEW_MODAL_CONFIG = {
  width: 800,
  footer: null
} as const;
