import { Button } from "antd";
import { setGlobalState } from "@/redux/modules/global";
import { RootState, useDispatch, useSelector } from "@/redux";

const SwitchDark: React.FC = () => {
  const dispatch = useDispatch();
  const isDark = useSelector((state: RootState) => state.global.isDark);
  {
    /* <IconFont style={{ fontSize: 22 }} type={isDark ? "icon-sun" : "icon-moon"} /> */
  }
  return (
    <Button
      type="text"
      size="large"
      className="switch-dark"
      icon={<i style={{ fontSize: 22 }} className={isDark ? "iconfont icon-sun" : "iconfont icon-moon"}></i>}
      onClick={() => dispatch(setGlobalState({ key: "isDark", value: !isDark }))}
    ></Button>
  );
};

export default SwitchDark;
