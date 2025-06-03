import React, { useState, useEffect, useCallback } from "react";
import { Modal, Upload, Space } from "antd";
import { message } from "@/hooks/useMessage";
import { uploadFile } from "@/api/modules/module";
import http from "@/api";
import { Icon } from "@/components";
import { RcFile, UploadChangeParam, UploadFile } from "antd/lib/upload/interface";

// 环境变量
const baseURL = import.meta.env.VITE_API_URL as string;
const VITE_USER_NODE_ENV = import.meta.env.VITE_USER_NODE_ENV as string;

// 上传状态标志，防止重复上传
let uploadFlag = true;

/**
 * 文件信息接口
 */
interface FileInfo {
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
interface UploadImageProps {
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
 * 图片上传组件
 * 支持单图片和多图片上传模式
 * @param props 组件属性
 */
const UploadImage: React.FC<UploadImageProps> = props => {
  const { Id, accept, isUnique = false, filePath = "material", masterTable, masterColumn, imageType, ImageUrl } = props;

  // 组件状态
  const [loading, setLoading] = useState<boolean>(false);
  const [imageUrl, setImageUrl] = useState<string>("");
  const [masterId, setMasterId] = useState<string>("");
  const [files, setFiles] = useState<FileInfo[]>([]);
  const [previewVisible, setPreviewVisible] = useState<boolean>(false);
  const [previewTitle, setPreviewTitle] = useState<string | null>(null);
  const [previewImage, setPreviewImage] = useState<string>("");

  /**
   * 将文件转换为Base64格式
   * @param file 文件对象
   * @param callback 回调函数
   */
  const getBase64 = useCallback((file: RcFile, callback: (url: string) => void) => {
    const reader = new FileReader();
    reader.addEventListener("load", () => callback(reader.result as string));
    reader.readAsDataURL(file);
  }, []);

  /**
   * 获取图片数据
   * 根据主记录ID和图片类型获取已上传的图片列表
   */
  const getImageData = useCallback(async () => {
    if (!Id) return;

    try {
      const { Data, Success } = await http.get<any>(`/api/File/GetFileList?masterId=${Id}&imageType=${imageType ?? filePath}`);

      if (Success) {
        if (!isUnique) {
          setFiles(Data);
        } else if (Data.length > 0) {
          setImageUrl((VITE_USER_NODE_ENV === "development" ? baseURL : "") + `/api/File/Img/${Data[0].ID}`);
        }
      }
    } catch (error) {
      console.error("获取图片数据失败:", error);
      message.error("获取图片数据失败");
    }
  }, [Id, imageType, filePath, isUnique]);

  /**
   * 上传文件附件
   * @param file 上传的文件信息
   */
  const uploadFileAttachment = useCallback(
    async (fileInfo: UploadChangeParam<UploadFile>) => {
      // 如果是删除操作，直接返回
      if (fileInfo.file.status === "removed") return;

      // 防止重复上传
      if (!uploadFlag) return false;
      uploadFlag = false;

      try {
        // 如果没有文件对象，直接返回
        if (!fileInfo.file.originFileObj) {
          uploadFlag = true;
          return;
        }

        // 转换为Base64并显示预览
        getBase64(fileInfo.file.originFileObj, (url: string) => {
          setImageUrl(url);
          setLoading(true);
        });

        // 准备上传
        message.loading("上传中..", 0);
        const formData = new FormData();

        formData.append("file", fileInfo.file.originFileObj);
        formData.append("masterId", masterId);
        formData.append("filePath", filePath);
        formData.append("imageType", imageType ?? "");
        formData.append("masterTable", masterTable ?? "");
        formData.append("masterColumn", masterColumn ?? "");
        formData.append("isUnique", isUnique ? "true" : "false");

        // 执行上传
        const { Success, Message } = await uploadFile("/api/File/UploadImage", formData);

        if (Success) {
          message.success(Message || "上传成功");
          await getImageData();
        } else {
          message.error(Message || "上传失败");
        }
      } catch (error) {
        console.error("上传图片失败:", error);
        message.error("上传图片失败");
      } finally {
        uploadFlag = true;
        message.destroy();
        setLoading(false);
      }
    },
    [masterId, filePath, imageType, masterTable, masterColumn, isUnique, getBase64, getImageData]
  );

  /**
   * 处理图片预览
   * @param file 预览的文件信息
   */
  const handlePreview = useCallback((file: UploadFile) => {
    setPreviewVisible(true);
    setPreviewTitle(file.name || "图片预览");
    setPreviewImage(file.url || "");
  }, []);

  /**
   * 处理图片删除
   * @param file 要删除的文件信息
   */
  const onRemove = useCallback(
    async (file: UploadFile) => {
      try {
        await http.get<any>(`/api/File/Delete?Id=${file.uid}`);

        // 更新本地文件列表
        const tempList = [...files];
        const index = tempList.findIndex((item: FileInfo) => item.ID === file.uid);

        if (index > -1) {
          tempList.splice(index, 1);
          setFiles(tempList);
        }

        message.success("删除成功");
      } catch (error) {
        console.error("删除图片失败:", error);
        message.error("删除图片失败");
      }

      return false;
    },
    [files]
  );

  // 初始化和依赖变更时更新数据
  useEffect(() => {
    setMasterId(Id || "");

    if (ImageUrl) {
      const imageUrl = `/api/File/GetByUrl?url=${ImageUrl}`;
      setImageUrl(imageUrl);
    }

    getImageData();
  }, [ImageUrl, Id, getImageData]);

  // 准备文件列表用于上传组件
  const fileList = files.map((item: FileInfo) => ({
    uid: item.ID,
    name: item.OriginalFileName,
    status: "done" as const,
    url: `/api/File/GetByUrl?url=${item.FileName}.${item.FileExt}`
  }));

  // 渲染唯一图片模式
  const renderUniqueMode = () => (
    <Space style={{ justifyContent: "flex-start", float: "left" }}>
      <Upload accept={accept} listType="picture-card" showUploadList={false} onChange={uploadFileAttachment} disabled={!masterId}>
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

  // 渲染多图片模式
  const renderMultipleMode = () => (
    <>
      <div>
        <Upload
          accept={accept}
          listType="picture-card"
          fileList={fileList}
          onChange={uploadFileAttachment}
          onPreview={handlePreview}
          onRemove={onRemove}
        >
          <div>
            <Icon name={loading ? "LoadingOutlined" : "PlusOutlined"} className="font-size24" />
            <div className="ant-upload-text">图片上传</div>
          </div>
        </Upload>
      </div>
      <Modal open={previewVisible} title={previewTitle} footer={null} onCancel={() => setPreviewVisible(false)}>
        <img alt="预览图片" style={{ width: "100%" }} src={previewImage} />
      </Modal>
    </>
  );

  // 根据模式渲染不同的上传组件
  return <>{isUnique ? renderUniqueMode() : masterId ? renderMultipleMode() : null}</>;
};

// 添加组件显示名称，方便调试
UploadImage.displayName = "UploadImage";

// 使用React.memo优化组件性能，避免不必要的重渲染
export default React.memo(UploadImage);
