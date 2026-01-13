import { useDispatch } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";

import { Icon } from "@/components";
interface ToolBarRightProps {
  layout?: string;
}

const Agent: React.FC<ToolBarRightProps> = ({ layout }) => {
  const dispatch = useDispatch();
  return (
    <i onClick={() => dispatch(setGlobalState({ key: "layout", value: layout !== "Chat" ? "chat" : "vertical" }))}>
      <Icon name={layout === "Chat" ? "LayoutOutlined" : "OpenAIOutlined"} style={{ fontSize: 16 }} />
    </i>
  );
};
export default Agent;
