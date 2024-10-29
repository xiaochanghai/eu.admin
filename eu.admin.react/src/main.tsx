import App from "./App.tsx";
import ReactDOM from "react-dom/client";
import { Provider } from "react-redux";
import { store, persistor } from "@/redux";
import { PersistGate } from "redux-persist/integration/react";
import "antd/dist/reset.css";
import "@/styles/index.less";
import "@/assets/fonts/font.less";
import "@/assets/iconfont/iconfont.less";
import "virtual:svg-icons-register";

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <Provider store={store}>
    <PersistGate persistor={persistor}>
      {/* <React.StrictMode> */}
      <App />
      {/* </React.StrictMode> */}
    </PersistGate>
  </Provider>
);
