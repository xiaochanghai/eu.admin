import React from "react";

import { Think } from "@ant-design/x";
import type { ComponentProps } from "@ant-design/x-markdown";
import { useTranslation } from "react-i18next";

export const ThinkComponent = React.memo((props: ComponentProps) => {
  const { t } = useTranslation();

  const [title, setTitle] = React.useState(`${t("chat.deepThinking")}...`);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    if (props.streamStatus === "done") {
      setTitle(t("chat.completeThinking"));
      setLoading(false);
    }
  }, [props.streamStatus]);

  return (
    <Think title={title} loading={loading}>
      {props.children}
    </Think>
  );
});
