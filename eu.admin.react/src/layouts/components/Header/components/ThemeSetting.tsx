import { useDispatch, useSelector } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";

const ThemeSetting: React.FC = () => {
  const dispatch = useDispatch();
  const allowCustom = useSelector(state => state.global.allowCustomTheme === true);

  const setThemeDrawerVisible = () => {
    dispatch(setGlobalState({ key: "themeDrawerVisible", value: true }));
  };

  return allowCustom ? <i className="iconfont icon-zhuti" onClick={setThemeDrawerVisible}></i> : null;
};
export default ThemeSetting;
