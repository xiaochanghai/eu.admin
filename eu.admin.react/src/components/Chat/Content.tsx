import React from "react";

import Markdown from "./Markdown";
import TableList from "@/views/system/common/components/TableList";

interface MarkdownProps {
  content: string;
}

const ChatContent: React.FC<MarkdownProps> = React.memo(({ content }) => {
  // debugger;
  try {
    if (content !== "<tool_call>" && content !== "\n") {
      let content1 = JSON.parse(content);
      console.log(content1);
      console.log(content1.content.text);
      if (content1 && content1.content && content1.content[0].text === "BD_SUPPLIER_MNG")
        return (
          <div style={{ maxWidth: 1000 }}>
            <TableList moduleCode="BD_SUPPLIER_MNG" />
          </div>
        );
    } else return <Markdown content={content} />;
  } catch (error) {
    return <Markdown content={content} />;
  }
});

export default ChatContent;
