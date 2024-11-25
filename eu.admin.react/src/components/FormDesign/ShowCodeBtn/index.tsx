import { useState, useEffect } from "react";
import { Modal } from "antd";
import AceEditor from "react-ace";
// import { CopyToClipboard } from "react-copy-to-clipboard";
import { RootState, useSelector } from "@/redux";
import { generateCode } from "./generateCode";
// import JSZip from "jszip";
// import "ace-builds/src-noconflict/mode-jsx";
// import "ace-builds/src-noconflict/mode-javascript";
// import "ace-builds/src-noconflict/theme-xcode";

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
  };
  return (
    <>
      <button onClick={() => setVisible(!visible)} className="btn ml-2">
        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
        </svg>
        <span className="ml-1">show code</span>
      </button>
      <Modal width="800px" onCancel={() => setVisible(false)} title="代码预览" footer={false} visible={visible}>
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
            tabSize: 2
          }}
        />
        <div className="flex justify-center border-t border-gray-200 pt-5">
          <button onClick={downLoad} className="btn btn-primary">
            <svg
              className="w-5 h-5 mr-1"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
              />
            </svg>
            下载
          </button>
        </div>
      </Modal>
    </>
  );
}
