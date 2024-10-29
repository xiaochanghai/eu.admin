import React from "react";
import { Layout } from "antd";
import { RootState, useSelector } from "@/redux";
import "./index.less";

const { Footer } = Layout;

const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;
const APP_COMPANY = import.meta.env.VITE_GLOB_APP_COMPANY;

const LayoutFooter: React.FC = () => {
  const footer = useSelector((state: RootState) => state.global.footer);

  return (
    <React.Fragment>
      {footer && (
        <Footer className="ant-footer">
          <a>
            2020-2024 © {APP_TITLE} {APP_COMPANY ? APP_COMPANY + " 技术支持" : null}
          </a>
        </Footer>
      )}
    </React.Fragment>
  );
};

export default LayoutFooter;
