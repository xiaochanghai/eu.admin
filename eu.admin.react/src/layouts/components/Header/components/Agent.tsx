import { useCallback } from "react";
import { useDispatch } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";

import { Icon } from "@/components";

type LayoutType = "Chat" | "vertical" | string;

interface AgentProps {
  layout?: LayoutType;
}

const Agent: React.FC<AgentProps> = ({ layout }) => {
  const dispatch = useDispatch();

  const isChatLayout = layout === "Chat";
  const targetLayout = isChatLayout ? "vertical" : "chat";
  const iconName = isChatLayout ? "LayoutOutlined" : "OpenAIOutlined";
  const ariaLabel = isChatLayout ? "切换到垂直布局" : "切换到对话布局";

  const handleToggleLayout = useCallback(() => {
    dispatch(setGlobalState({ key: "layout", value: targetLayout }));
  }, [dispatch, targetLayout]);

  return (
    <button
      type="button"
      onClick={handleToggleLayout}
      aria-label={ariaLabel}
      style={{
        background: "none",
        border: "none",
        padding: 0,
        cursor: "pointer",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center"
      }}
    >
      <Icon name={iconName} style={{ fontSize: 16 }} />
    </button>
  );
};

export default Agent;
