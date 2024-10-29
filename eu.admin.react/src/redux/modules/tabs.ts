import { TabsState, TabsListProp } from "@/redux/interface";
import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { getUrlWithParams } from "@/utils";

const tabsState: TabsState = {
  tabsList: []
};

const tabsSlice = createSlice({
  name: "hooks-tabs",
  initialState: tabsState,
  reducers: {
    setTabsList(state, { payload }: PayloadAction<TabsState["tabsList"]>) {
      state.tabsList = payload;
    },
    addTab(state, { payload }: PayloadAction<TabsListProp>) {
      if (state.tabsList.every(item => item.path !== payload.path)) {
        state.tabsList.push(payload);
      }
    },
    removeTab(state, { payload }: PayloadAction<{ path: string; isCurrent: boolean }>) {
      if (!state.tabsList.find(item => item.path === payload.path)?.closable) return;
      if (payload.isCurrent) {
        state.tabsList.forEach((item, index) => {
          if (item.path !== payload.path) return;
          const nextTab = state.tabsList[index + 1] || state.tabsList[index - 1];
          if (!nextTab) return;
          window.$navigate(nextTab.path);
        });
      }
      state.tabsList = state.tabsList.filter(item => item.path !== payload.path);
    },
    closeTabsOnSide(state, { payload }: PayloadAction<{ path: string; type: "left" | "right" }>) {
      const currentIndex = state.tabsList.findIndex(item => item.path === payload.path);
      if (currentIndex !== -1) {
        const range = payload.type === "left" ? [0, currentIndex] : [currentIndex + 1, state.tabsList.length];
        state.tabsList = state.tabsList.filter((item, index) => {
          return index < range[0] || index >= range[1] || !item.closable;
        });
      }
    },
    closeMultipleTab(state, { payload }: PayloadAction<{ path?: string }>) {
      state.tabsList = state.tabsList.filter(item => {
        return item.path === payload.path || !item.closable;
      });
    },
    setTabTitle(state, { payload }: PayloadAction<string>) {
      state.tabsList = state.tabsList.map(item => {
        if (item.path == getUrlWithParams()) {
          return { ...item, title: payload };
        }
        return item;
      });
    }
  }
});

export const { setTabsList, addTab, removeTab, closeMultipleTab, closeTabsOnSide, setTabTitle } = tabsSlice.actions;
export default tabsSlice.reducer;
