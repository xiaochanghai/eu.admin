import { type MenuProps, Dropdown } from "antd";
import { useDispatch, RootState, useSelector } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";
import { SizeType } from "antd/es/config-provider/SizeContext";

const ComponentSize: React.FC = () => {
  const dispatch = useDispatch();
  const componentSize = useSelector((state: RootState) => state.global.componentSize);

  const setComponentSize: MenuProps["onClick"] = val => {
    dispatch(setGlobalState({ key: "componentSize", value: val.key as SizeType }));
  };

  const items: MenuProps["items"] = [
    { key: "middle", label: "默认", disabled: componentSize === "middle" },
    { key: "large", label: "大型", disabled: componentSize === "large" },
    { key: "small", label: "小型", disabled: componentSize === "small" }
  ];

  const menuProps = {
    items,
    onClick: setComponentSize
  };

  return (
    <Dropdown menu={menuProps} placement="bottom" arrow trigger={["click"]}>
      <i className="iconfont icon-contentright"></i>
    </Dropdown>
  );
};

export default ComponentSize;
