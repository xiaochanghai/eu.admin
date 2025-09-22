import { useState, useImperativeHandle, forwardRef } from "react";
import { Modal, message } from "antd";

export interface ShowInfoModalProps {
  name: string;
}

export interface InfoModalRef {
  showModal: (param: ShowInfoModalProps) => void;
}

const InfoModal = forwardRef<InfoModalRef, {}>((_props, ref) => {
  const [isModalOpen, setIsModalOpen] = useState(false);

  useImperativeHandle(ref, () => ({ showModal }));

  const showModal = (params: ShowInfoModalProps) => {
    console.log(params);
    setIsModalOpen(true);
  };

  const handleOk = () => {
    setIsModalOpen(false);
    message.success("修改用户信息成功 🎉");
  };

  const handleCancel = () => {
    setIsModalOpen(false);
  };

  return (
    <Modal title="个人信息" open={isModalOpen} onOk={handleOk} onCancel={handleCancel} destroyOnHidden>
      <p>This is UserInfo...</p>
      <p>This is UserInfo...</p>
      <p>This is UserInfo...</p>
    </Modal>
  );
});

InfoModal.displayName = "InfoModal";

export default InfoModal;
