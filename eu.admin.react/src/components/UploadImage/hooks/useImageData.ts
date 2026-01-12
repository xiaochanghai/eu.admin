import { useState, useCallback, useEffect } from "react";
import { UploadFile } from "antd/lib/upload/interface";
import http from "@/api";
import { message } from "@/hooks/useMessage";
import { FileInfo } from "../types";
import { API_PATHS, UPLOAD_MESSAGES } from "../constants";
import { buildImageUrl, buildFileListUrl } from "../utils";

/**
 * 图片数据管理配置参数
 */
interface UseImageDataProps {
  Id?: string | null;
  ImageUrl?: string;
  imageType?: string;
  filePath: string;
  isUnique: boolean;
}

/**
 * 图片数据管理自定义 Hook
 * 处理图片列表的获取、删除等操作
 */
export const useImageData = (props: UseImageDataProps) => {
  const { Id, ImageUrl, imageType, filePath, isUnique } = props;

  const [files, setFiles] = useState<FileInfo[]>([]);
  const [imageUrl, setImageUrl] = useState<string>("");
  const [masterId, setMasterId] = useState<string>("");

  // 环境变量
  const baseURL = import.meta.env.VITE_API_URL as string;
  const isDevelopment = import.meta.env.VITE_USER_NODE_ENV === "development";

  /**
   * 获取图片数据
   */
  const getImageData = useCallback(async () => {
    if (!Id) return;

    try {
      const url = buildFileListUrl(Id, imageType ?? filePath);
      const { Data, Success } = await http.get<any>(url);

      if (Success) {
        if (!isUnique) {
          setFiles(Data);
        } else if (Data.length > 0) {
          const url = buildImageUrl(Data[0].ID, isDevelopment, baseURL);
          setImageUrl(url);
        }
      }
    } catch (error) {
      console.error("获取图片数据失败:", error);
      message.error(UPLOAD_MESSAGES.GET_DATA_FAILED);
    }
  }, [Id, imageType, filePath, isUnique, isDevelopment, baseURL]);

  /**
   * 处理图片删除
   */
  const onRemove = useCallback(
    async (file: UploadFile) => {
      try {
        await http.delete<any>(`${API_PATHS.DELETE_FILE}/${file.uid}`);

        // 更新本地文件列表
        const tempList = [...files];
        const index = tempList.findIndex((item: FileInfo) => item.ID === file.uid);

        if (index > -1) {
          tempList.splice(index, 1);
          setFiles(tempList);
        }

        message.success(UPLOAD_MESSAGES.DELETE_SUCCESS);
      } catch (error) {
        console.error("删除图片失败:", error);
        message.error(UPLOAD_MESSAGES.DELETE_FAILED);
      }

      return false;
    },
    [files]
  );

  /**
   * 转换为 Upload 组件所需的文件列表格式
   */
  const fileList = files.map((item: FileInfo) => ({
    uid: item.ID,
    name: item.OriginalFileName,
    status: "done" as const,
    url: buildImageUrl(item.ID, isDevelopment, baseURL)
  }));

  // 初始化和依赖变更时更新数据
  useEffect(() => {
    setMasterId(Id || "");

    if (ImageUrl) {
      const url = `${API_PATHS.GET_BY_URL}?url=${ImageUrl}`;
      setImageUrl(url);
    }

    getImageData();
  }, [ImageUrl, Id, getImageData]);

  return {
    files,
    fileList,
    imageUrl,
    masterId,
    setImageUrl,
    getImageData,
    onRemove
  };
};
