import { useEffect } from "react";
import { theme } from "antd";
import { shallowEqual } from "react-redux";
import { RootState, useSelector } from "@/redux";
import { setStyleProperty } from "@/utils";
import { getLightColor, getDarkColor } from "@/utils/color";
import siderTheme from "@/styles/theme/sider";
import headerTheme from "@/styles/theme/header";
import globalTheme from "@/styles/theme/global";

type ThemeType = "light" | "inverted" | "dark";

/**
 * @description  Use global theme settings hook
 */
const useTheme = () => {
  const { token } = theme.useToken();

  const { isDark, primary, isGrey, isWeak, borderRadius, compactAlgorithm, siderInverted, headerInverted } = useSelector(
    (state: RootState) => ({
      isDark: state.global.isDark,
      primary: state.global.primary,
      isGrey: state.global.isGrey,
      isWeak: state.global.isWeak,
      borderRadius: state.global.borderRadius,
      compactAlgorithm: state.global.compactAlgorithm,
      siderInverted: state.global.siderInverted,
      headerInverted: state.global.headerInverted
    }),
    shallowEqual
  );

  /**
   * @description Toggle dark mode
   */
  useEffect(() => switchDark(), [isDark]);
  const switchDark = () => {
    const html = document.documentElement;
    html.setAttribute("class", isDark ? "dark" : "");
    changePrimary();
  };

  /**
   * @description Toggle primary colors
   */
  useEffect(() => changePrimary(), [primary, borderRadius, compactAlgorithm]);
  const changePrimary = () => {
    const type: ThemeType = isDark ? "dark" : "light";
    // custom less variable
    Object.entries(globalTheme[type]).forEach(([key, val]) => setStyleProperty(key, val));
    // antd less variable
    Object.entries(token).forEach(([key, val]) => setStyleProperty(`--hooks-${key}`, val));
    // antd primaryColor less variable
    for (let i = 1; i <= 9; i++) {
      setStyleProperty(
        `--hooks-colorPrimary${i}`,
        isDark ? `${getDarkColor(primary, i / 10)}` : `${getLightColor(primary, i / 10)}`
      );
    }
  };

  /**
   * @description Switch between gray and weak colors
   */
  useEffect(() => changeGreyOrWeak(), [isGrey, isWeak]);
  const changeGreyOrWeak = () => {
    const html = document.documentElement;
    html.style.filter = isWeak ? "invert(80%)" : isGrey ? "grayscale(1)" : "";
  };

  /**
   * @description Toggle sider theme
   */
  useEffect(() => changeSiderTheme(), [isDark, siderInverted]);
  const changeSiderTheme = () => {
    const type: ThemeType = isDark ? "dark" : siderInverted ? "inverted" : "light";
    Object.entries(siderTheme[type]).forEach(([key, val]) => setStyleProperty(key, val));
  };

  /**
   * @description Toggle header theme
   */
  useEffect(() => changeHeaderTheme(), [isDark, headerInverted]);
  const changeHeaderTheme = () => {
    const type: ThemeType = isDark ? "dark" : headerInverted ? "inverted" : "light";
    Object.entries(headerTheme[type]).forEach(([key, val]) => setStyleProperty(key, val));
  };
};

export default useTheme;
