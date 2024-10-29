import i18n from "i18next";
import enUsTrans from "./modules/en";
import zhCnTrans from "./modules/zh";
import { getBrowserLang } from "@/utils";
import { initReactI18next } from "react-i18next";

i18n.use(initReactI18next).init({
  resources: {
    en: {
      translation: enUsTrans
    },
    zh: {
      translation: zhCnTrans
    }
  },
  lng: getBrowserLang(),
  debug: false,
  interpolation: {
    escapeValue: false
  }
});

export default i18n;
