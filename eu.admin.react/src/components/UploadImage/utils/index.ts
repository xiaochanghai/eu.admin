import { RcFile } from "antd/lib/upload/interface";

/**
 * 将文件转换为Base64格式
 * @param file 文件对象
 * @returns Promise<string> Base64字符串
 */
export const getBase64 = (file: RcFile): Promise<string> => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = error => reject(error);
  });
};

/**
 * 构建图片URL
 * @param fileId 文件ID
 * @param isDevelopment 是否开发环境
 * @param baseURL 基础URL
 * @returns 完整的图片URL
 */
export const buildImageUrl = (fileId: string, isDevelopment: boolean, baseURL: string): string => {
  const prefix = isDevelopment ? baseURL : "";
  return `${prefix}/api/File/Img/${fileId}`;
};

/**
 * 构建获取文件列表的URL
 * @param masterId 主记录ID
 * @param imageType 图片类型
 * @returns 查询URL
 */
export const buildFileListUrl = (masterId: string, imageType: string): string => {
  return `/api/File/GetFileList?masterId=${masterId}&imageType=${imageType}`;
};
