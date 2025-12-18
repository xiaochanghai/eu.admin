// ? Global default configuration items
import type { ColProps } from "antd";

// Home address
export const HOME_URL: string = "/home/index";

// login page address
export const LOGIN_URL: string = "/login";

// default theme color
export const DEFAULT_PRIMARY: string = "#1677ff";

// Routing whitelist address (must be in a locally existing routing staticRouter.ts)
export const ROUTER_WHITE_LIST: string[] = ["/500"];

// AMAP_MAP_KEY
export const AMAP_MAP_KEY: string = "";

// BAIDU_MAP_KEY
export const BAIDU_MAP_KEY: string = "";
// 表单布局配置常量
export const STANDARD_FORM_LAYOUT: { labelCol: ColProps; wrapperCol: ColProps } = {
  labelCol: { xs: { span: 8 }, sm: { span: 8 }, md: { span: 8 } },
  wrapperCol: { xs: { span: 16 }, sm: { span: 16 }, md: { span: 16 } }
};

export const MODAL_FORM_LAYOUT: { labelCol: ColProps; wrapperCol: ColProps } = {
  labelCol: { span: 6, xl: 6, md: 8, sm: 8 },
  wrapperCol: { span: 16 }
};
