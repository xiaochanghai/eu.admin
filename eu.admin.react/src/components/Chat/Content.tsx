import React from "react";

import Markdown from "./Markdown";
import TableList from "@/views/system/common/components/TableList";
import FormPage from "./FormPage";

interface MarkdownProps {
  content: string;
}

const ChatContent: React.FC<MarkdownProps> = React.memo(({ content }) => {
  // debugger;
  try {
    if (content !== "<tool_call>" && content !== "\n") {
      let content1 = JSON.parse(content);
      // console.log(content1);
      // console.log(content1.content.text);
      let moduleInfo = JSON.parse(content1.content[0].text);
      if (content1 && content1.content && moduleInfo) {
        return (
          <div style={{ maxWidth: 1000 }}>
            {moduleInfo.type === "module_list" && <TableList moduleCode={moduleInfo.moudleCode} />}
            {moduleInfo.type === "module_edit" && <FormPage moduleCode={moduleInfo.moudleCode} id={moduleInfo.id ?? ""} />}
          </div>
        );
      }
    } else return <Markdown content={content} />;
  } catch (error) {
    return <Markdown content={content} />;
  }
});

export default ChatContent;
