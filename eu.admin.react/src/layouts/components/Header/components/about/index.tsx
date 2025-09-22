import { useState, useImperativeHandle, forwardRef } from "react";
import { Modal } from "antd";
import Content from "@/views/about";

export interface ShowAboutModalProps {
  name: string;
}

export interface AboutModalRef {
  showModal: (param: ShowAboutModalProps) => void;
}

const AboutModal = forwardRef<AboutModalRef, {}>((_props, ref) => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  useImperativeHandle(ref, () => ({ showModal }));

  const showModal = (params: ShowAboutModalProps) => {
    console.log(params);
    setIsModalOpen(true);
  };

  const handleCancel = () => {
    setIsModalOpen(false);
  };

  return (
    <Modal open={isModalOpen} width={1000} footer={null} onCancel={handleCancel} destroyOnHidden>
      <Content />
    </Modal>
  );
});

AboutModal.displayName = "InfoModal";

export default AboutModal;
