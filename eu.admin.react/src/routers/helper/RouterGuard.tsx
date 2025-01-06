import React, { useEffect } from "react";
import { RootState, useSelector } from "@/redux";
import { MetaProps } from "@/routers/interface";
import { useLoaderData, useLocation, useNavigate } from "react-router-dom";
import { HOME_URL, LOGIN_URL, ROUTER_WHITE_LIST } from "@/config";
import { notification } from "@/hooks/useMessage";

/**
 * @description Route guard component
 */
interface RouterGuardProps {
  children: React.ReactNode;
}

const RouterGuard: React.FC<RouterGuardProps> = props => {
  const loader = useLoaderData();
  const navigate = useNavigate();
  const { pathname } = useLocation();

  // Mount navigate to provide non-React function components or calls in custom React Hook functions
  window.$navigate = navigate;
  window.$notification = notification;

  const token = useSelector((state: RootState) => state.user.token);
  const authMenuList = useSelector((state: RootState) => state.auth.authMenuList);

  useEffect(() => {
    const meta = loader as MetaProps;
    if (meta) {
      const title = import.meta.env.VITE_GLOB_APP_TITLE;
      document.title = meta?.title ? `${meta.title} - ${title}` : title;
    }

    // Routing whitelist
    if (ROUTER_WHITE_LIST.includes(pathname)) return;

    // Whether login page
    const isLoginPage = pathname === LOGIN_URL;

    // If there is menu data, token, or login on the accessed page, redirect to the home page
    if (authMenuList.length && token && isLoginPage) {
      return navigate(HOME_URL);
    }

    // If there is not menu data, no token && the accessed page is not login, redirect to the login page
    if ((!token && !isLoginPage) || (!authMenuList.length && !token && !isLoginPage)) {
      return navigate(LOGIN_URL, { replace: true });
    }
  }, [loader]);

  return props.children;
};

export default RouterGuard;
