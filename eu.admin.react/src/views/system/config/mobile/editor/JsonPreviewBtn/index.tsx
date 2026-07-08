import { useState } from "react";
import { Button, Modal } from "antd";
import { CodeOutlined } from "@ant-design/icons";
import { useSelector } from "@/redux";
import { RootState } from "@/redux";

export default function JsonPreviewBtn() {
  const state = useSelector((s: RootState) => s.mobileEditor);
  const [visible, setVisible] = useState(false);

  const jsonStr = JSON.stringify(
    {
      type: state.type,
      props: state.props,
      children: state.children
    },
    null,
    2
  );

  return (
    <>
      <Button icon={<CodeOutlined />} onClick={() => setVisible(true)}>
        JSON预览
      </Button>
      <Modal
        title="配置 JSON 预览"
        open={visible}
        onCancel={() => setVisible(false)}
        footer={null}
        width={700}
      >
        <pre
          style={{
            maxHeight: 500,
            overflow: "auto",
            background: "#0f172a",
            color: "#dbeafe",
            padding: 16,
            borderRadius: 8,
            fontSize: 12,
            lineHeight: 1.5
          }}
        >
          {jsonStr}
        </pre>
      </Modal>
    </>
  );
}
