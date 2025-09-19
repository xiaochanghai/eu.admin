//
// import { ChatMain } from "@/components/Chat";
// import { useNavigate } from "react-router-dom";
// import { RootState, useSelector } from "@/redux";
// import { LOGIN_URL } from "@/config";

import React, { useEffect } from "react";
import { Layout } from "antd";
import ToolBarRight from "@/layouts/components/Header/ToolBarRight";
import { ChatMain } from "@/components/Chat";
import logo from "@/assets/images/logo.png";
import "./index.less";
const { Header } = Layout;
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;

const ToolBarLeft: React.FC = () => {
  return (
    <div className="logo">
      <img src={logo} alt="logo" className="logo-img" />
      <h2 className="logo-text">{APP_TITLE}</h2>
    </div>
  );
};
const LayoutChat: React.FC = () => {
  useEffect(() => {
    document.title = `AI助手 - ${APP_TITLE}`;
  }, []);
  // const navigate = useNavigate();
  // const token = useSelector((state: RootState) => state.user.token);
  // const isCollapse = useSelector((state: RootState) => state.global.isCollapse);

  // Redirect to login immediately when token becomes empty (after logout)
  // useEffect(() => {
  //   if (!token) {
  //     navigate(LOGIN_URL, { replace: true });
  //   }
  // }, [token]);

  return (
    <section className="layout-vertical layout-chat">
      {/* <Sider width={210} collapsed={isCollapse}>
        <div className="logo">
          <img src={logo} alt="logo" className="logo-img" />
          {!isCollapse && <h2 className="logo-text">{APP_TITLE}</h2>}
        </div> 
      </Sider> */}
      <Layout>
        <Header>
          <ToolBarLeft />
          <ToolBarRight />
        </Header>
        <ChatMain />
      </Layout>
    </section>
  );
};

export default LayoutChat;
