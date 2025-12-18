import { useState, useEffect } from "react";
import { Modal, Button, Space, message, Tooltip } from "antd";
import { CodeOutlined, DownloadOutlined, CopyOutlined } from "@ant-design/icons";
import AceEditor from "react-ace";
import { RootState, useSelector } from "@/redux";
import { generateCode } from "./generateCode";

let myWorker = new Worker("./worker.js");

export default function CodeBox() {
  let state = useSelector((state: RootState) => state.formDesign);
  const [visible, setVisible] = useState(false);
  const [code, setCode] = useState("");

  useEffect(() => {
    myWorker.onmessage = function (e) {
      setCode(e.data);
    };
  }, []);

  useEffect(() => {
    const code = generateCode(state);
    myWorker.postMessage(code);
    console.log("Message posted to worker");
  }, [state]);

  const downLoad = () => {
    try {
      // 下载的文件名
      let filename = "App.jsx";
      let file = new File([code], filename, {
        type: "text/javascript"
      });
      // 创建隐藏的可下载链接
      let eleLink = document.createElement("a");
      eleLink.download = filename;
      eleLink.style.display = "none";
      // 下载内容转变成blob地址
      eleLink.href = URL.createObjectURL(file);
      // 触发点击
      document.body.appendChild(eleLink);
      eleLink.click();
      // 然后移除
      document.body.removeChild(eleLink);
      message.success("代码下载成功！");
    } catch (error) {
      message.error("下载失败，请重试");
    }
  };

  const copyCode = () => {
    try {
      navigator.clipboard.writeText(code);
      message.success("代码已复制到剪贴板！");
    } catch (error) {
      message.error("复制失败，请重试");
    }
  };

  return (
    <>
      <Tooltip title="代码预览">
        <Button icon={<CodeOutlined />} onClick={() => setVisible(!visible)}>
          代码预览
        </Button>
      </Tooltip>

      <Modal
        width={900}
        onCancel={() => setVisible(false)}
        title={
          <div className="flex items-center gap-2">
            <CodeOutlined className="text-blue-500" />
            <span>代码预览</span>
          </div>
        }
        footer={
          <div className="flex justify-between items-center">
            <div className="text-sm text-gray-500">
              <span className="font-medium">文件名：</span>
              <span className="text-blue-600">App.jsx</span>
            </div>
            <Space>
              <Button icon={<CopyOutlined />} onClick={copyCode}>
                复制代码
              </Button>
              <Button type="primary" icon={<DownloadOutlined />} onClick={downLoad}>
                下载文件
              </Button>
            </Space>
          </div>
        }
        open={visible}
        styles={{
          body: { padding: "16px 0" }
        }}
      >
        <div className="border border-gray-200 rounded-lg overflow-hidden">
          <AceEditor
            mode="javascript"
            theme="xcode"
            width="100%"
            height="560px"
            onChange={() => {}}
            value={code}
            name="code"
            showPrintMargin={false}
            fontSize={14}
            setOptions={{
              enableBasicAutocompletion: true,
              enableLiveAutocompletion: true,
              enableSnippets: true,
              showLineNumbers: true,
              tabSize: 2,
              useWorker: false
            }}
          />
        </div>
      </Modal>
    </>
  );
}
