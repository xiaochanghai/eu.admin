import { useState, useImperativeHandle, forwardRef } from "react";
import { Modal, message, Input, Form } from "antd";
import http from "@/api";
export interface ShowPassModalProps {
  name: string;
}

export interface PasswordModalRef {
  showModal: (param: ShowPassModalProps) => void;
}

const PasswordModal = forwardRef<PasswordModalRef, {}>((_props, ref) => {
  const [form] = Form.useForm();
  const [isModalOpen, setIsModalOpen] = useState(false);

  useImperativeHandle(ref, () => ({ showModal }));

  const showModal = (params: ShowPassModalProps) => {
    console.log(params);
    setIsModalOpen(true);
  };

  const handleOk = async () => {
    form.validateFields().then(async (values: any) => {
      let { Success } = await http.put<any>("/api/Authorize/RestPassword", values);
      if (Success) {
        setIsModalOpen(false);
        form.resetFields();
        message.success("修改密码成功 🎉");
      }
    });
  };

  const handleCancel = () => {
    setIsModalOpen(false);
  };

  return (
    <Modal title="修改密码" open={isModalOpen} onOk={handleOk} onCancel={handleCancel} destroyOnClose={true}>
      <Form
        form={form}
        name="dependencies"
        autoComplete="off"
        style={{
          maxWidth: 400
        }}
        // onFinish={handleFinish}
        layout="vertical"
      >
        <Form.Item
          label="旧密码"
          name="oldPassword"
          rules={[
            {
              required: true
            }
          ]}
        >
          <Input.Password />
        </Form.Item>
        <Form.Item
          label="新密码"
          name="newPassword"
          rules={[
            {
              required: true
            }
          ]}
        >
          <Input.Password />
        </Form.Item>

        {/* Field */}
        <Form.Item
          label="确认密码"
          name="confirmPassword"
          dependencies={["newPassword"]}
          rules={[
            {
              required: true
            },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue("newPassword") === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error("确认密码与新密码不匹配!"));
              }
            })
          ]}
        >
          <Input.Password />
        </Form.Item>
      </Form>
    </Modal>
  );
});

PasswordModal.displayName = "PasswordModal";

export default PasswordModal;
