import { lazy } from "react";
import { getFlatMenuList } from "@/utils";
import { Navigate } from "react-router-dom";
import { RouteObjectType } from "../interface";
import RouterGuard from "./RouterGuard";
import LayoutIndex from "@/layouts";
import LazyComponent from "@/components/Lazy";

// Import all view files in the views directory
const modules = import.meta.glob("@/views/**/*.tsx") as Record<string, Parameters<typeof lazy>[number]>;

/**
 * @description Convert menuList to the format required by react-router
 * @param {Array} authMenuList Permissions menu list
 * @returns {Array} The routing format required by the react-router
 */
export const convertToDynamicRouterFormat = (authMenuList: RouteObjectType[]) => {
  // Flat Routing
  const flatMenuList = getFlatMenuList(authMenuList);

  // Convert Routing
  const handleMenuList = flatMenuList.map(item => {
    item.children && delete item.children;

    // Set redirection component
    if (item.redirect) item.element = <Navigate to={item.redirect} />;

    // Convert element to antd component
    if (item.element && typeof item.element == "string") {
      const Component = LazyComponent(lazy(modules["/src/views" + item.element + ".tsx"]));
      item.element = <RouterGuard>{Component}</RouterGuard>;
    }

    // Set loader
    item.loader = () => {
      return { ...item.meta, redirect: !!item.redirect };
    };
    return item;
  });

  const dynamicRouter: RouteObjectType[] = [{ element: <LayoutIndex />, children: [] }];

  // Add to Dynamic routing
  handleMenuList.forEach(item => {
    if (item.meta?.isFull) dynamicRouter.push(item);
    else dynamicRouter[0].children?.push(item);
  });

  return dynamicRouter;
};
