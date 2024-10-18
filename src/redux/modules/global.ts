import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { GlobalState } from "@/redux/interface";
import { DEFAULT_PRIMARY } from "@/config";

const globalState: GlobalState = {
  // layout mode (vertical | classic | transverse | columns)
  layout: "vertical",
  // antd component size ("small" | "middle" | "large")
  componentSize: "middle",
  // antd compact theme
  compactAlgorithm: false,
  // antd border radius
  borderRadius: 6,
  // current system language
  language: null,
  // Whether the current page is full screen
  maximize: false,
  // theme color
  primary: DEFAULT_PRIMARY,
  // dark mode
  isDark: false,
  // gray mode
  isGrey: false,
  // weakness mode
  isWeak: false,
  // happy mode
  isHappy: false,
  // menu splitting
  menuSplit: true,
  // sidebar Invert Color
  siderInverted: false,
  // head Inverted Color
  headerInverted: false,
  // menu collapse
  isCollapse: false,
  // menu accordion
  accordion: true,
  // water mark
  watermark: false,
  // breadcrumb
  breadcrumb: true,
  // breadcrumb icon
  breadcrumbIcon: true,
  // tabs
  tabs: true,
  // tabs icon
  tabsIcon: true,
  // tabs drag
  tabsDrag: true,
  // footer
  footer: true,
  // theme box display status
  themeDrawerVisible: false
};

const globalSlice = createSlice({
  name: "hooks-global",
  initialState: globalState,
  reducers: {
    setGlobalState<T extends keyof GlobalState>(state: GlobalState, { payload }: PayloadAction<ObjToKeyValUnion<GlobalState>>) {
      state[payload.key as T] = payload.value as GlobalState[T];
    }
  }
});

export const { setGlobalState } = globalSlice.actions;
export default globalSlice.reducer;
