import { useState, useImperativeHandle, forwardRef } from "react";
import { Modal, message, Input, Form } from "antd";
import http from "@/api";
import { useTranslation } from "react-i18next";

export interface ShowPassModalProps {
  name: string;
}

export interface PasswordModalRef {
  showModal: (param: ShowPassModalProps) => void;
}

const PasswordModal = forwardRef<PasswordModalRef, {}>((_props, ref) => {
  const [form] = Form.useForm();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const { t } = useTranslation();

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
        message.success(t("password.changeSuccessMessage"));
      }
    });
  };

  const handleCancel = () => {
    setIsModalOpen(false);
  };

  return (
    <Modal title={t("password.title")} open={isModalOpen} onOk={handleOk} onCancel={handleCancel} destroyOnHidden>
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
          label={t("password.oldPassword")}
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
          label={t("password.newPassword")}
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
          label={t("password.confirmPassword")}
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
                return Promise.reject(new Error(t("password.notMatchMessage")));
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
