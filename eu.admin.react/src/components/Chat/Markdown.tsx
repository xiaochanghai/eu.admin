import React from "react";

import markdownit from "markdown-it";
const md = markdownit({ html: true, breaks: true });

interface MarkdownProps {
  content: string;
}

const Markdown: React.FC<MarkdownProps> = React.memo(({ content }) => {
  return <div className="chat-bubble" dangerouslySetInnerHTML={{ __html: md.render(content) }} />;
});

export default Markdown;
