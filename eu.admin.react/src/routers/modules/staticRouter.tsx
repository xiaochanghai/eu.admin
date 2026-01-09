import { lazy } from "react";
import { Navigate } from "react-router-dom";
import { Loading } from "@/components/Loading";
import LazyComponent from "@/components/Lazy";
import { HOME_URL, LOGIN_URL } from "@/config";
import { RouteObjectType } from "@/routers/interface";
import NotAuth from "@/components/Error/403";
import NotFound from "@/components/Error/404";
import NotNetwork from "@/components/Error/500";
import ErrorTest from "@/components/Error/ErrorTest";
import RouterGuard from "../helper/RouterGuard";

const Login = LazyComponent(lazy(() => import("@/views/login/index")));

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
    element: Login,
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
  // ErrorBoundary test (development only)
  {
    path: "/error-test",
    element: <ErrorTest />,
    meta: {
      title: "ErrorBoundary测试"
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
    },
    HydrateFallback: () => <Loading />
  };
});
