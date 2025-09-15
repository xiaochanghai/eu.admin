import React from "react";

import markdownit from "markdown-it";
const md = markdownit({ html: true, breaks: true });

interface MarkdownProps {
  content: string;
}

const Markdown: React.FC<MarkdownProps> = React.memo(({ content }) => {
  return (
    <>
      {content != "\n" && (
        <div
          className="chat-bubble"
          style={{ backgroundColor: "rgba(0, 0, 0, 0.06)", padding: "12px 16px", borderRadius: 8 }}
          dangerouslySetInnerHTML={{ __html: md.render(content) }}
        />
      )}
    </>
  );
});

export default Markdown;
