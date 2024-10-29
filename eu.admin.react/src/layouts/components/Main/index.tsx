import React from "react";
import { Layout } from "antd";
import { useDebounceFn } from "ahooks";
import { RefreshContext } from "@/context/Refresh";
import { useEffect, createRef, useContext } from "react";
import { RouteObjectType } from "@/routers/interface";
import { useLocation, useOutlet } from "react-router-dom";
import { setGlobalState } from "@/redux/modules/global";
import { RootState, useDispatch, useSelector } from "@/redux";
// import { CSSTransition, SwitchTransition } from "react-transition-group";
import Maximize from "./components/Maximize";
import LayoutTabs from "@/layouts/components/Tabs";
import LayoutFooter from "@/layouts/components/Footer";
import "./index.less";
import KeepAlive, { useKeepaliveRef } from "keepalive-for-react";

type RouteTypeWithNodeRef = {
  nodeRef?: React.Ref<HTMLElement> | undefined;
} & RouteObjectType;

const { Content } = Layout;

const LayoutMain: React.FC = () => {
  const outlet = useOutlet();
  const dispatch = useDispatch();
  const { pathname, search } = useLocation();
  // const { pathname } = useLocation();
  const { outletShow, reload } = useContext(RefreshContext);
  const aliveRef = useKeepaliveRef();

  const maximize = useSelector((state: RootState) => state.global.maximize);
  const isCollapse = useSelector((state: RootState) => state.global.isCollapse);
  const flatMenuList = useSelector((state: RootState) => state.auth.flatMenuList);

  // Monitor window changes, collapse menu
  const { run } = useDebounceFn(
    () => {
      const screenWidth = document.body.clientWidth;
      const shouldCollapse = screenWidth < 1200;
      if (isCollapse !== shouldCollapse) dispatch(setGlobalState({ key: "isCollapse", value: shouldCollapse }));
    },
    { wait: 100 }
  );
  useEffect(() => {
    window.addEventListener("resize", run, false);
    return () => window.removeEventListener("resize", run);
  }, []);

  // Monitor whether the current page is maximized, dynamically add class
  useEffect(() => {
    const root = document.getElementById("root") as HTMLElement;
    root.classList.toggle("main-maximize", maximize);
  }, [maximize]);

  useEffect(() => {
    if (reload) aliveRef.current?.refresh();
  }, [reload]);
  // Solve the transition animation that causes useEffect to execute multiple times
  // @see: http://reactcommunity.org/react-transition-group/with-react-router
  const menuList: RouteTypeWithNodeRef[] = flatMenuList.map(item => ({ ...item, nodeRef: createRef() }));
  const { nodeRef, element } = menuList.find(route => route.path === pathname) ?? {};
  const cacheKey = (pathname + search).slice(1).split("/").join("_");
  // let cacheDivClassName = !outletShow ? "fade-slide" : "";
  let cache = element == "/system/common/index" ? false : true;
  // if (reload) cache = false;
  return (
    <React.Fragment>
      <Maximize />
      <LayoutTabs />

      <KeepAlive
        aliveRef={aliveRef}
        activeName={cacheKey}
        // cacheDivClassName={"fade"}
        // cacheDivClassName={cacheDivClassName}
        max={20}
        cache={cache}
        strategy={"LRU"}
      >
        {/* <SwitchTransition>
          <CSSTransition classNames="fade" key={pathname} nodeRef={nodeRef} timeout={30000} exit={false} unmountOnExit>
            <Content ref={nodeRef}>{outletShow && outlet}</Content>
          </CSSTransition>
        </SwitchTransition> */}
        <Content ref={nodeRef}>{outletShow && outlet}</Content>
      </KeepAlive>
      <LayoutFooter />
    </React.Fragment>
  );
};

export default LayoutMain;
