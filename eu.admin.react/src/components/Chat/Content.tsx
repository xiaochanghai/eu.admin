import React from "react";

import Markdown from "./Markdown";
import { TableList } from "@/components";
import FormPage from "./FormPage";
import { Flex } from "antd";
import { Attachment } from "@ant-design/x/es/attachments";
import CustomFileCard from "./CustomFileCard"; // 导入自定义组件
import { downloadFile } from "@/utils";
import { ChatUploadExcel } from "@/components/UploadExcel";
// import { Attachments } from "@ant-design/x";

interface MarkdownProps {
  content: string;
}
// const filesList = [
//   {
//     uid: "1",
//     name: "excel-file.xlsx",
//     size: 111111
//   },
//   {
//     uid: "2",
//     name: "word-file.docx",
//     size: 222222
//   },
//   {
//     uid: "3",
//     name: "image-file.png",
//     size: 333333
//   },
//   {
//     uid: "4",
//     name: "pdf-file.pdf",
//     size: 444444
//   },
//   {
//     uid: "5",
//     name: "ppt-file.pptx",
//     size: 555555
//   },
//   {
//     uid: "6",
//     name: "video-file.mp4",
//     size: 666666
//   },
//   {
//     uid: "7",
//     name: "audio-file.mp3",
//     size: 777777
//   },
//   {
//     uid: "8",
//     name: "zip-file.zip",
//     size: 888888
//   },
//   {
//     uid: "9",
//     name: "markdown-file.md",
//     size: 999999,
//     description: "Custom description here"
//   },
//   {
//     uid: "10",
//     name: "image-file.png",
//     thumbUrl: "https://zos.alipayobjects.com/rmsportal/jkjgkEfvpUPVyRjUImniVslZfWPnJuuZ.png",
//     url: "https://zos.alipayobjects.com/rmsportal/jkjgkEfvpUPVyRjUImniVslZfWPnJuuZ.png",
//     size: 123456
//   }
// ];
const ChatContent: React.FC<MarkdownProps> = React.memo(({ content }) => {
  try {
    if (content !== "<tool_call>" && content !== "\n") {
      let moduleInfo = JSON.parse(content);
      if (moduleInfo.type) {
        if (moduleInfo.type === "module_list" || moduleInfo.type === "module_edit")
          return (
            <div style={{ minWidth: 1000, maxWidth: 1200 }}>
              {moduleInfo.type === "module_list" && <TableList moduleCode={moduleInfo.moduleCode} />}
              {moduleInfo.type === "module_edit" && <FormPage moduleCode={moduleInfo.moduleCode} id={moduleInfo.id ?? ""} />}
            </div>
          );
        else if (moduleInfo.type === "ExcelTemplate") {
          var files = moduleInfo.files.map((file: any) => {
            return { ...file, uid: file.ID, name: file.FileName, size: file.Length, status: "done", url: file.Url };
          });
          return (
            <Flex vertical gap="middle">
              {/* <Markdown content={content} /> */}
              {files.map((file: Attachment, index: React.Key | null | undefined) => (
                <CustomFileCard
                  key={index}
                  item={file}
                  onDownload={file => {
                    // 自定义下载逻辑，使用文件的 ID 和 FileName
                    downloadFile(file.uid, file.name);
                  }}
                />
              ))}
              {/* {files.map((file: Attachment, index: React.Key | null | undefined) => (
                <Attachments.FileCard key={index} item={file} />
              ))} */}
            </Flex>
          );
        } else if (moduleInfo.type === "ExcelResult") {
          return <ChatUploadExcel ImportDataId={moduleInfo.ImportDataId} TemplateId={moduleInfo.TemplateId} />;
        }
      } else return <Markdown content={content} />;
    } else return <Markdown content={content} />;
  } catch (error) {
    return <Markdown content={content} />;
  }
});

export default ChatContent;
