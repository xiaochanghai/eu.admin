import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { UserState } from "@/redux/interface";

const userState: UserState = {
  token: "",
  userInfo: { UserName: "", UserId: "", AvatarFileId: "", UserType: "" }
};

const globalSlice = createSlice({
  name: "hooks-user",
  initialState: userState,
  reducers: {
    setToken(state, { payload }: PayloadAction<string>) {
      state.token = payload;
    },
    setUserInfo(state, { payload }: PayloadAction<UserState["userInfo"]>) {
      state.userInfo = payload;
    },
    clearUserInfo(state) {
      state.userInfo = { UserName: "", UserId: "", AvatarFileId: "", UserType: "" };
    }
  }
});

export const { setToken, setUserInfo, clearUserInfo } = globalSlice.actions;
export default globalSlice.reducer;
