import React, { useEffect } from "react";
import { theme, ConfigProvider, App as AppProvider } from "antd";
import { HappyProvider } from "@ant-design/happy-work-theme";
import { RootState, useSelector, useDispatch } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";
import { LanguageType } from "@/redux/interface";
import { shallowEqual } from "react-redux";
import { getBrowserLang } from "@/utils";
import { I18nextProvider } from "react-i18next";
import { RefreshProvider } from "@/context/Refresh";
import RouterProvider from "@/routers";
import i18n from "@/languages/index";
import enUS from "antd/locale/en_US";
import zhCN from "antd/locale/zh_CN";
import dayjs from "dayjs";
import "dayjs/locale/zh-cn";

const App: React.FC = () => {
  const dispatch = useDispatch();

  const { isDark, primary, isHappy, componentSize, compactAlgorithm, borderRadius, language } = useSelector(
    (state: RootState) => ({
      isDark: state.global.isDark,
      primary: state.global.primary,
      isHappy: state.global.isHappy,
      componentSize: state.global.componentSize,
      compactAlgorithm: state.global.compactAlgorithm,
      borderRadius: state.global.borderRadius,
      language: state.global.language
    }),
    shallowEqual
  );

  // init theme algorithm
  const algorithm = () => {
    const algorithmArr = isDark ? [theme.darkAlgorithm] : [theme.defaultAlgorithm];
    if (compactAlgorithm) algorithmArr.push(theme.compactAlgorithm);
    return algorithmArr;
  };

  // init language
  const initLanguage = () => {
    const result = language ?? getBrowserLang();
    dispatch(setGlobalState({ key: "language", value: result as LanguageType }));
    i18n.changeLanguage(language as string);
    dayjs.locale(language === "zh" ? "zh-cn" : "en");
  };

  useEffect(() => {
    initLanguage();
  }, [language]);

  return (
    <ConfigProvider
      locale={language === "zh" ? zhCN : enUS}
      componentSize={componentSize}
      // autoInsertSpaceInButton={true}
      theme={{
        token: { colorPrimary: primary, borderRadius },
        algorithm: algorithm()
      }}
    >
      <HappyProvider disabled={!isHappy}>
        <AppProvider>
          <I18nextProvider i18n={i18n}>
            <RefreshProvider>
              <RouterProvider />
            </RefreshProvider>
          </I18nextProvider>
        </AppProvider>
      </HappyProvider>
    </ConfigProvider>
  );
};

export default App;
