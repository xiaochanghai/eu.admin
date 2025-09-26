import React from "react";
import type { MessageInfo } from "@ant-design/x/es/use-x-chat";
import Markdown from "./Markdown";
import type { UploadFile } from "antd/es/upload/interface";

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
  return <>{message.message.role === "user" ? content : <Markdown content={content} />}</>;
});
