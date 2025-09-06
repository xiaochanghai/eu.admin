import React from "react";
import { Space, Spin } from "antd";

import type { MessageInfo } from "@ant-design/x/es/use-x-chat";
import markdownit from "markdown-it";
const md = markdownit({ html: true, breaks: true });

export type BubbleDataType = {
  role: string;
  content: string;
};

interface BubbleViewProps {
  message: MessageInfo<BubbleDataType>;
}

export const ChatBubble: React.FC<BubbleViewProps> = React.memo(({ message }) => {
  return (
    <>
      {message.id === "loading" ? (
        <Space>
          <Spin size="small" />
        </Space>
      ) : message.message.role == "user" ? (
        message.message.content
      ) : (
        <div className="chat-bubble" dangerouslySetInnerHTML={{ __html: md.render(message.message.content) }} />
      )}
    </>
  );
});
