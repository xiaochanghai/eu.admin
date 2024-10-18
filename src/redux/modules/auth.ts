import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { AuthState } from "@/redux/interface";
import { getFlatMenuList, getShowMenuList } from "@/utils";

const authState: AuthState = {
  // List of menu permissions
  authMenuList: [],
  // Menu permission list ==> left menu bar rendering, need to remove isHide == true
  showMenuList: [],
  // Menu permission list ==> flattened one-dimensional array menu, mainly used to add dynamic routing
  flatMenuList: [],
  // List of button permissions
  authButtonList: {}
};

const authSlice = createSlice({
  name: "hooks-auth",
  initialState: authState,
  reducers: {
    setAuthButtonList(state, { payload }: PayloadAction<AuthState["authButtonList"]>) {
      state.authButtonList = payload;
    },
    setAuthMenuList(state, { payload }: PayloadAction<AuthState["authMenuList"]>) {
      state.authMenuList = payload;
      state.flatMenuList = getFlatMenuList(payload);
      state.showMenuList = getShowMenuList(payload);
    }
  }
});

export const { setAuthButtonList, setAuthMenuList } = authSlice.actions;
export default authSlice.reducer;
