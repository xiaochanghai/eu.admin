import React, { useEffect, useState } from "react";
import { Layout } from "antd";
import { useLocation } from "react-router-dom";
import { getFirstLevelMenuList } from "@/utils";
import { RootState, useSelector } from "@/redux";
import { RouteObjectType } from "@/routers/interface";
import ToolBarLeft from "@/layouts/components/Header/ToolBarLeft";
import ToolBarRight from "@/layouts/components/Header/ToolBarRight";
import LayoutMenu from "@/layouts/components//Menu";
import LayoutMain from "@/layouts/components/Main";
import CollapseIcon from "../components/Header/components/CollapseIcon";
import logo from "@/assets/images/logo.png";
import "./index.less";

const { Header, Sider } = Layout;

const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;

const LayoutClassic: React.FC = () => {
  const { pathname } = useLocation();

  const isCollapse = useSelector((state: RootState) => state.global.isCollapse);
  const menuSplit = useSelector((state: RootState) => state.global.menuSplit);
  const showMenuList = useSelector((state: RootState) => state.auth.showMenuList);
  const firstLevelMenuList = getFirstLevelMenuList(showMenuList);

  const [subMenuList, setSubMenuList] = useState<RouteObjectType[]>([]);

  useEffect(() => {
    if (menuSplit) changeSubMenu();
  }, [pathname, menuSplit]);

  const changeSubMenu = () => {
    const menuItem = showMenuList.find(item => {
      return pathname === item.path || `/${pathname.split("/")[1]}` === item.path;
    });
    setSubMenuList(menuItem?.children || []);
  };

  return (
    <section className="layout-classic">
      <Header>
        <div className={`header-lf ${menuSplit ? "hide-logo" : "mask-image"}`}>
          <div className="logo">
            <img src={logo} alt="logo" className="logo-img" />
            <h2 className="logo-text">{APP_TITLE}</h2>
          </div>
          {menuSplit ? <LayoutMenu mode="horizontal" menuList={firstLevelMenuList} menuSplit={true} /> : <ToolBarLeft />}
        </div>
        <div className="header-ri">
          <ToolBarRight />
        </div>
      </Header>
      <div className="classic-content">
        <Sider width={210} collapsed={isCollapse} className={`${!subMenuList.length && menuSplit ? "not-sider" : ""}`}>
          {menuSplit ? (
            <React.Fragment>
              {subMenuList.length ? (
                <React.Fragment>
                  <LayoutMenu mode="inline" menuList={subMenuList} />
                  <div className="collapse-box">
                    <CollapseIcon />
                  </div>
                </React.Fragment>
              ) : null}
            </React.Fragment>
          ) : (
            <LayoutMenu mode="inline" />
          )}
        </Sider>
        <div className="classic-main">
          <LayoutMain />
        </div>
      </div>
    </section>
  );
};

export default LayoutClassic;
