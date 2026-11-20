import { useEffect, useMemo, useState } from "react";
import { RouterProvider as Router, RouteObject, createHashRouter, createBrowserRouter } from "react-router-dom";
import { convertToDynamicRouterFormat } from "./helper/ConvertRouter";
import { wrappedStaticRouter } from "./modules/staticRouter";
import { RootState, useSelector } from "@/redux";
import { RouteObjectType } from "./interface";
import useTheme from "@/hooks/useTheme";
import useInitSystemTheme from "@/hooks/useInitSystemTheme";
import useMessage from "@/hooks/useMessage";
import usePermissions from "@/hooks/usePermissions";
import NotFound from "@/components/Error/404";
import RouterGuard from "./helper/RouterGuard";
import { LOGIN_URL } from "@/config";

const mode = import.meta.env.VITE_ROUTER_MODE;
let nextRouterKey = 0;
const getLocationPath = () =>
  mode === "hash" ? (window.location.hash.replace(/^#/, "") || "/").split("?")[0] : window.location.pathname;

/**
 * @description Route file entry
 */
const RouterProvider: React.FC = () => {
  // useTheme && useMessage
  useTheme();
  useMessage();
  useInitSystemTheme();

  const { initPermissions } = usePermissions();

  const token = useSelector((state: RootState) => state.user.token);
  const authMenuList = useSelector((state: RootState) => state.auth.authMenuList);
  const [pathname, setPathname] = useState(getLocationPath);

  useEffect(() => {
    // When refreshing the page, there is no menu data
    if (token && !authMenuList.length) {
      // The hook clears the current token on failure; the guard handles navigation.
      void initPermissions(token).catch(() => undefined);
    }
  }, [authMenuList, token, initPermissions]);

  const useFullRouter = Boolean(token) && authMenuList.length > 0 && pathname !== LOGIN_URL;

  const routerList = useMemo<RouteObjectType[]>(() => {
    if (!useFullRouter) return wrappedStaticRouter;

    // Register dynamic routes in the same render that enables the 404 fallback.
    const dynamicRouter = convertToDynamicRouterFormat(authMenuList);
    return [
      ...wrappedStaticRouter.map(item =>
        item.path === "*"
          ? {
              ...item,
              element: (
                <RouterGuard>
                  <NotFound />
                </RouterGuard>
              )
            }
          : item
      ),
      ...dynamicRouter
    ];
  }, [authMenuList, useFullRouter]);

  const routerMode = {
    hash: () => createHashRouter(routerList as RouteObject[]),
    history: () => createBrowserRouter(routerList as RouteObject[])
  };

  // RouterProvider owns matching state. A new router must get a new Provider,
  // otherwise the old wildcard match can render the new manifest's 404.
  const { router, routerKey } = useMemo(() => ({ router: routerMode[mode](), routerKey: nextRouterKey++ }), [routerList]);

  useEffect(() => () => router.dispose(), [router]);

  useEffect(() => {
    setPathname(router.state.location.pathname);
    return router.subscribe(state => setPathname(state.location.pathname));
  }, [router]);

  return <Router key={routerKey} router={router} />;
};

export default RouterProvider;
