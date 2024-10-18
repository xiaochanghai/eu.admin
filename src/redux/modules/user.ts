import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { UserState } from "@/redux/interface";

const userState: UserState = {
  token: "",
  userInfo: { UserName: "", UserId: "", AvatarFileId: "" }
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
    }
  }
});

export const { setToken, setUserInfo } = globalSlice.actions;
export default globalSlice.reducer;
