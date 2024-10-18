import React, { useState, useEffect } from "react";
import { Modal, Upload, Space, message } from "antd";
import { uploadFile } from "@/api/modules/module";
import http from "@/api";
// import styles from "../index.less";
import { PlusOutlined, LoadingOutlined } from "@ant-design/icons";
// import { useNavigate } from "react-router-dom";
let flag = true;
let baseURL = import.meta.env.VITE_API_URL as string;
let VITE_USER_NODE_ENV = import.meta.env.VITE_USER_NODE_ENV as string;

const UploadImage: React.FC<any> = props => {
  // const navigate = useNavigate();
  let { Id, accept, isUnique, filePath, masterTable, masterColumn, imageType, ImageUrl } = props;
  const [loading, setLoading] = useState(false);
  const [imageUrl, setImageUrl] = useState("");
  const [MasterId, setMasterId] = useState("");
  const [files, setFiles] = useState([]);
  const [previewVisible, setPreviewVisible] = useState(false);
  const [previewTitle, setPreviewTitle] = useState(null);
  const [previewImage, setPreviewImage] = useState("");

  useEffect(() => {
    setMasterId(Id);
    if (ImageUrl) {
      let ImageUrl1 = "/api/File/GetByUrl?url=" + ImageUrl;
      setImageUrl(ImageUrl1);
    }
    getImageData();
  }, [ImageUrl, Id]);

  const getBase64 = (img: any, callback: any) => {
    const reader = new FileReader();
    reader.addEventListener("load", () => callback(reader.result));
    reader.readAsDataURL(img);
  };
  const getImageData = async () => {
    if (Id) {
      let { Data, Success } = await http.get<any>(
        "/api/File/GetFileList?masterId=" + Id + "&imageType=" + (imageType ?? filePath)
      );
      if (Success) {
        if (!isUnique) setFiles(Data);
        else if (Data.length > 0)
          setImageUrl((VITE_USER_NODE_ENV == "development" ? baseURL : "") + `/api/File/Img/${Data[0].ID}`);
      }
    }
  };
  const uploadFileAttachment = async (file: any) => {
    if (file.file.status == "removed") return;
    if (!filePath) filePath = "material";
    if (!isUnique) isUnique = false;
    if (flag) flag = false;
    else return false;

    getBase64(file.file.originFileObj, (url: any) => {
      setImageUrl(url);
      setLoading(true);
    });
    //附件上传
    message.loading("上传中..", 0);
    const formData = new FormData();

    formData.append("file", file.file.originFileObj);
    formData.append("masterId", MasterId);
    formData.append("filePath", filePath);
    formData.append("imageType", imageType ?? "");
    formData.append("masterTable", masterTable);
    formData.append("masterColumn", masterColumn);
    formData.append("isUnique", "true");

    let { Success, Message } = await uploadFile("/api/File/UploadImage", formData);
    flag = true;
    message.destroy();
    if (Success) {
      message.success(Message);
      getImageData();
    }

    setLoading(false);
  };
  const fileList: any[] | undefined = [];
  files.map((item: any) => {
    fileList.push({
      uid: item.ID,
      name: item.OriginalFileName,
      status: "done",
      url: "/api/File/GetByUrl?url=" + item.FileName + "." + item.FileExt
    });
  });

  const handlePreview = (file: any) => {
    setPreviewVisible(true);
    setPreviewTitle(file.name);
    setPreviewImage(file.url);
  };
  const onRemove = async (file: any) => {
    // console.log(file);
    await http.get<any>("/api/File/Delete?Id=" + file.uid);
    let tempList = [...files];
    let index = tempList.findIndex((item: any) => item.ID == file.uid);
    if (index > -1) {
      tempList.splice(index, 1);
      setFiles(tempList);
    }
    return false;
  };
  return (
    <>
      {isUnique ? (
        <>
          <Space style={{ justifyContent: "flex-start", float: "left" }}>
            <Upload
              accept={accept}
              listType="picture-card"
              showUploadList={false}
              // className={styles.uploader}
              onChange={file => {
                uploadFileAttachment(file);
              }}
              disabled={MasterId ? false : true}
            >
              {imageUrl ? (
                <img src={imageUrl} alt={imageUrl} style={{ width: "100%" }} />
              ) : (
                <div>
                  {loading ? <LoadingOutlined style={{ fontSize: 24 }} /> : <PlusOutlined style={{ fontSize: 24 }} />}
                  <div className="ant-upload-text">图片上传</div>
                </div>
              )}
            </Upload>
          </Space>
        </>
      ) : (
        <>
          {MasterId ? (
            <>
              <div>
                {MasterId ? (
                  <div>
                    <Upload
                      accept={accept}
                      listType="picture-card"
                      // showUploadList={false}
                      fileList={fileList}
                      onChange={file => {
                        uploadFileAttachment(file);
                      }}
                      //   onPreview={handlePreview}
                      onPreview={file => {
                        handlePreview(file);
                      }}
                      onRemove={file => {
                        onRemove(file);
                      }}
                    >
                      <div>
                        {loading ? <LoadingOutlined style={{ fontSize: 24 }} /> : <PlusOutlined style={{ fontSize: 24 }} />}
                        <div className="ant-upload-text">图片上传</div>
                      </div>
                    </Upload>
                  </div>
                ) : null}
              </div>
              <Modal open={previewVisible} title={previewTitle} footer={null} onCancel={() => setPreviewVisible(false)}>
                <img
                  alt="example"
                  style={{
                    width: "100%"
                  }}
                  src={previewImage}
                />
              </Modal>
            </>
          ) : null}
        </>
      )}
    </>
  );
};

export default UploadImage;
