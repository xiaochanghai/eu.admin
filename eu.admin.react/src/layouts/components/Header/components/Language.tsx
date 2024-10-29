import type { MenuProps } from "antd";
import { Dropdown } from "antd";
import { useDispatch, RootState, useSelector } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";
import { LanguageType } from "@/redux/interface";

const Language: React.FC = () => {
  const dispatch = useDispatch();
  const language = useSelector((state: RootState) => state.global.language);

  const setLanguage: MenuProps["onClick"] = val => {
    dispatch(setGlobalState({ key: "language", value: val.key as LanguageType }));
  };

  const items: MenuProps["items"] = [
    { key: "zh", label: "简体中文", disabled: language === "zh" },
    { key: "en", label: "English", disabled: language === "en" }
  ];

  const menuProps = {
    items,
    onClick: setLanguage
  };

  return (
    <Dropdown menu={menuProps} placement="bottom" arrow trigger={["click"]}>
      <i className="iconfont icon-zhongyingwen"></i>
    </Dropdown>
  );
};
export default Language;
