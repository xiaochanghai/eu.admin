import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { ModuleInfo } from "@/api/interface/index";

// const moduleState: ModuleInfo =  null;
const state: any = {
  moduleInfos: {},
  ids: {},
  tableParams: {},
  searchVisibles: {}
};
const globalSlice = createSlice({
  name: "hooks-module",
  initialState: state,
  reducers: {
    // setGlobalState<T extends keyof GlobalState>(state: GlobalState, { payload }: PayloadAction<ObjToKeyValUnion<GlobalState>>) {
    //   state[payload.key as T] = payload.value as GlobalState[T];
    // }
    setModuleInfo(state, { payload }: PayloadAction<ModuleInfo, string>) {
      state.moduleInfos[payload.moduleCode] = payload;
    },
    setId(state, { payload }: PayloadAction<any, string>) {
      state.ids[payload.moduleCode] = payload.id ?? null;
    },
    setTableParam(state, { payload }: PayloadAction<any, string>) {
      let moduleCode = payload.moduleCode;
      delete payload.moduleCode;
      state.tableParams[moduleCode] = payload;
    },
    setSearchVisible(state, { payload }: PayloadAction<any, string>) {
      let moduleCode = payload.moduleCode;
      delete payload.moduleCode;
      state.searchVisibles[moduleCode] = payload.value;
    }
  }
});

export const { setModuleInfo, setId, setTableParam, setSearchVisible } = globalSlice.actions;
export default globalSlice.reducer;
