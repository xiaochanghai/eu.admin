import { Navigate } from "react-router-dom";
import { Loading } from "@/components/Loading";
import { HOME_URL, LOGIN_URL } from "@/config";
import { RouteObjectType } from "@/routers/interface";
import Login from "@/views/login/index";
import NotAuth from "@/components/Error/403";
import NotFound from "@/components/Error/404";
import NotNetwork from "@/components/Error/500";
import RouterGuard from "../helper/RouterGuard";

/**
 * staticRouter
 */
export const staticRouter: RouteObjectType[] = [
  {
    path: "/",
    element: <Navigate to={HOME_URL} />
  },
  {
    path: LOGIN_URL,
    element: <Login />,
    meta: {
      title: "登录"
    }
  },
  // error pages
  {
    path: "/403",
    element: <NotAuth />,
    meta: {
      title: "403页面"
    }
  },
  {
    path: "/404",
    element: <NotFound />,
    meta: {
      title: "404页面"
    }
  },
  {
    path: "/500",
    element: <NotNetwork />,
    meta: {
      title: "500页面"
    }
  },
  // Set <Loading /> here first to prevent page refresh 404
  {
    path: "*",
    element: <Loading />
  }
];

// Wrap each element with a higher-order component
export const wrappedStaticRouter = staticRouter.map(route => {
  return {
    ...route,
    element: <RouterGuard>{route.element}</RouterGuard>,
    loader: () => {
      return { ...route.meta };
    }
  };
});
