import React from "react";
import { Space, Flex } from "antd";
import { Attachments } from "@ant-design/x";
import type { MessageInfo } from "@ant-design/x/es/use-x-chat";
import type { UploadFile } from "antd/es/upload/interface";
import Markdown from "./Markdown";

export type BubbleDataType = {
  role: string;
  content: string;

  parentMessageId?: string;
  contents?: BubbleDataTypeContent[];
  files?: UploadFile[];
  fileId?: string;
};

export type BubbleDataTypeContent = {
  key?: string;
  content?: string;
  parentMessageId?: string;
};

interface BubbleViewProps {
  message: MessageInfo<BubbleDataType>;
  content: string;
}

export const ChatBubble: React.FC<BubbleViewProps> = React.memo(({ message, content }) => {
  return (
    <>
      {message.message.role === "user" ? (
        <Space direction="vertical" size={12} style={{ width: "100%" }}>
          {/* 显示文本内容 */}
          {content && <div>{content}</div>}

          {/* 显示文件 */}
          <Flex vertical gap="middle">
            {message.message.files!.map((file, index) => (
              <Attachments.FileCard key={index} item={file} />
            ))}
          </Flex>
        </Space>
      ) : (
        <Markdown content={content} />
      )}
    </>
  );
});
