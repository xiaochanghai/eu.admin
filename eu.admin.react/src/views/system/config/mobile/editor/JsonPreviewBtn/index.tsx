import { useState } from "react";
import { Button, Modal, message } from "antd";
import { CodeOutlined, CopyOutlined } from "@ant-design/icons";
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

  const handleCopy = () => {
    navigator.clipboard.writeText(jsonStr).then(() => {
      message.success("已复制到剪贴板");
    });
  };

  return (
    <>
      <Button icon={<CodeOutlined />} onClick={() => setVisible(true)} style={{ borderRadius: 6 }}>
        JSON
      </Button>
      <Modal
        title={
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <CodeOutlined style={{ color: "#2563eb" }} />
            <span>配置 JSON 预览</span>
          </div>
        }
        open={visible}
        onCancel={() => setVisible(false)}
        footer={[
          <Button key="copy" icon={<CopyOutlined />} onClick={handleCopy}>
            复制
          </Button>,
          <Button key="close" type="primary" onClick={() => setVisible(false)}>
            关闭
          </Button>
        ]}
        width={720}
      >
        <pre
          style={{
            maxHeight: 500,
            overflow: "auto",
            background: "#0f172a",
            color: "#e2e8f0",
            padding: 20,
            borderRadius: 10,
            fontSize: 12,
            lineHeight: 1.6,
            fontFamily: "'Cascadia Code', 'Fira Code', Consolas, monospace",
            border: "1px solid #1e293b"
          }}
        >
          {jsonStr}
        </pre>
      </Modal>
    </>
  );
}
