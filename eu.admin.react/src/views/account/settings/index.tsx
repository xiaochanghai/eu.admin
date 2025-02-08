import { Button, Input, Upload, Form, Space, Card, Col, Row } from "antd";
import { message } from "@/hooks/useMessage";
import React, { useState } from "react";
import { RootState, useSelector, useDispatch } from "@/redux";
import { Icon } from "@/components";
import ImgCrop from "antd-img-crop";
import { uploadFile } from "@/api/modules/module";
import { setUserInfo } from "@/redux/modules/user";
import http from "@/api";
let baseURL = import.meta.env.VITE_API_URL as string;
let VITE_USER_NODE_ENV = import.meta.env.VITE_USER_NODE_ENV as string;
const Menu1: React.FC = () => {
  const dispatch = useDispatch();
  let userInfo = useSelector((state: RootState) => state.user.userInfo);

  const { AvatarFileId, UserId } = userInfo;

  const [imageUrl, setImageUrl] = useState(null);
  const [tempAvatarFileId, setTempAvatarFileId] = useState("");
  const handleFinish = async (values: any) => {
    values = { ...values, AvatarFileId: tempAvatarFileId };
    let { Success, Message } = await http.put<any>("/api/SmUser/" + UserId, values);

    if (Success) {
      userInfo = { ...userInfo, UserName: values.UserName };
      dispatch(setUserInfo({ ...userInfo, UserName: values.UserName, AvatarFileId: tempAvatarFileId }));
      message.success(Message);
    }
  };
  const getBase64 = (img: any, callback: any) => {
    const reader = new FileReader();
    reader.addEventListener("load", () => callback(reader.result));
    reader.readAsDataURL(img);
  };

  const beforeUpload = async (file: any) => {
    getBase64(file, (url: any) => {
      setImageUrl(url);
    });

    const formData = new FormData();
    formData.append("file", file);
    let { Data } = await uploadFile(`/api/SmUser/UploadAvatar`, formData);
    setTempAvatarFileId(Data);
    return false;
  };
  const uploadButton = (
    <button style={{ border: 0, background: "none" }} type="button">
      <Icon name="PlusOutlined" />
      <div style={{ marginTop: 8 }}>上传头像</div>
    </button>
  );

  return (
    <>
      <Row gutter={16}>
        <Col className="gutter-row" span={6}></Col>
        <Col className="gutter-row" span={12}>
          <>
            <Card className="basic-form">
              <Form layout="vertical" onFinish={handleFinish} initialValues={userInfo}>
                <Form.Item
                  name="UserName"
                  label={"昵称"}
                  wrapperCol={{ span: 12 }}
                  rules={[
                    {
                      required: true,
                      message: "请输入您的昵称!"
                    }
                  ]}
                >
                  <Input />
                </Form.Item>
                <Form.Item name="Avatar">
                  <>
                    <div>头像</div>
                    <div style={{ marginTop: 10 }}> </div>

                    <ImgCrop
                      {...{
                        modalTitle: `剪裁`,
                        rotationSlider: true,
                        showGrid: true
                      }}
                    >
                      <Upload
                        accept={".png,.jpeg,.jpg"}
                        method="POST"
                        name={"file"}
                        maxCount={1}
                        showUploadList={false}
                        beforeUpload={beforeUpload}
                        listType={"picture-card"}
                      >
                        {/* 小于可上数显示上传按钮 */}
                        {imageUrl || AvatarFileId ? (
                          <img
                            src={
                              imageUrl
                                ? imageUrl
                                : (VITE_USER_NODE_ENV == "development" ? baseURL : "") + `/api/File/Img/${AvatarFileId}`
                            }
                            alt="avatar"
                            style={{ width: "95%" }}
                          />
                        ) : (
                          uploadButton
                        )}
                      </Upload>
                    </ImgCrop>
                  </>
                </Form.Item>

                <Space style={{ display: "flex", justifyContent: "center" }}>
                  <Button type="primary" htmlType="submit">
                    保存
                  </Button>
                </Space>
              </Form>
            </Card>
          </>
        </Col>
        <Col className="gutter-row" span={6}></Col>
      </Row>
    </>
  );
};

export default Menu1;
