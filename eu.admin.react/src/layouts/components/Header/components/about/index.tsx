import { useState, useImperativeHandle, forwardRef, lazy } from "react";
import { Modal } from "antd";
import LazyComponent from "@/components/Lazy";

const AboutContent = LazyComponent(lazy(() => import("@/views/about")));

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
      {AboutContent}
    </Modal>
  );
});

AboutModal.displayName = "InfoModal";

export default AboutModal;
