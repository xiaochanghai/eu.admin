import { TypedUseSelectorHook, useDispatch as useReduxDispatch, useSelector as useReduxSelector } from "react-redux";
import { configureStore, combineReducers } from "@reduxjs/toolkit";
import { persistStore, persistReducer } from "redux-persist";
import storage from "redux-persist/lib/storage";
import global from "./modules/global";
import tabs from "./modules/tabs";
import auth from "./modules/auth";
import user from "./modules/user";
import module from "./modules/module";
import workflow from "./modules/workflow";
import formDesign from "./modules/formDesign";

// create reducer
const reducer = combineReducers({ global, tabs, auth, user, module, workflow, formDesign });

// redux persist
const persistConfig = {
  key: "redux-state",
  storage: storage,
  blacklist: ["auth", "module", "workflow", "formDesign"]
};
const persistReducerConfig = persistReducer(persistConfig, reducer);

// store
export const store = configureStore({
  reducer: persistReducerConfig,
  middleware: getDefaultMiddleware =>
    getDefaultMiddleware({
      serializableCheck: false
      // thunk 中间件已经默认包含在 getDefaultMiddleware 中，无需手动添加
    }),
  devTools: true
});

// create persist store
export const persistor = persistStore(store);

// redux hooks
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
export const useSelector: TypedUseSelectorHook<RootState> = useReduxSelector;
export const useDispatch = () => useReduxDispatch<AppDispatch>();
