import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { AuthState } from "@/redux/interface";
import { getFlatMenuList, getShowMenuList } from "@/utils";
import { setToken } from "./user";

const authState: AuthState = {
  sessionVersion: 0,
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
  },
  extraReducers: builder => {
    // Token updates start a new permission session, including expiry and re-login.
    builder.addCase(setToken, state => ({ ...authState, sessionVersion: state.sessionVersion + 1 }));
  }
});

export const { setAuthButtonList, setAuthMenuList } = authSlice.actions;
export default authSlice.reducer;
