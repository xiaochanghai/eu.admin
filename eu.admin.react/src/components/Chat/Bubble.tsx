import React from "react";
import { Space, Spin } from "antd";

import type { MessageInfo } from "@ant-design/x/es/use-x-chat";
import Markdown from "./Markdown";

export type BubbleDataType = {
  role: string;
  content: string;
  parentMessageId?: string;
  contents?: BubbleDataTypeContent[];
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
      {message.id === "loading" ? (
        <Space>
          <Spin size="small" />
        </Space>
      ) : message.message.role === "user" ? (
        content
      ) : (
        <Markdown content={content} />
      )}
    </>
  );
});
