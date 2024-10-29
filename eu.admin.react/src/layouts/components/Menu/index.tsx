import { useEffect, useMemo, useState } from "react";
import { Menu, MenuProps } from "antd";
import { useLocation, useNavigate, useMatches } from "react-router-dom";
import { RouteObjectType, MetaProps } from "@/routers/interface";
import { RootState, useSelector } from "@/redux";
import { shallowEqual } from "react-redux";
import { getOpenKeys } from "@/utils";
import { Icon } from "@/components/Icon";
import "./index.less";

interface LayoutMenuProps {
  mode: MenuProps["mode"];
  menuList?: RouteObjectType[];
  menuSplit?: boolean;
}

const LayoutMenu: React.FC<LayoutMenuProps> = ({ mode, menuList, menuSplit }) => {
  const matches = useMatches();
  const navigate = useNavigate();
  const { pathname } = useLocation();

  const { layout, isDark, accordion, isCollapse, siderInverted, headerInverted, showMenuList, flatMenuList } = useSelector(
    (state: RootState) => ({
      layout: state.global.layout,
      isDark: state.global.isDark,
      accordion: state.global.accordion,
      isCollapse: state.global.isCollapse,
      siderInverted: state.global.siderInverted,
      headerInverted: state.global.headerInverted,
      showMenuList: state.auth.showMenuList,
      flatMenuList: state.auth.flatMenuList
    }),
    shallowEqual
  );

  const [openKeys, setOpenKeys] = useState<string[]>([]);
  const [selectedKeys, setSelectedKeys] = useState<string[]>([]);
  const [splitSelectedKeys, setSplitSelectedKeys] = useState<string[]>([]);

  type MenuItem = Required<MenuProps>["items"][number];

  function getItem(
    label: React.ReactNode,
    key?: React.Key | null,
    icon?: React.ReactNode,
    children?: MenuItem[],
    type?: "group"
  ): MenuItem {
    return {
      key,
      icon,
      children,
      label,
      type
    } as MenuItem;
  }

  const handleMenuAsAntdFormat = (list: RouteObjectType[]): MenuItem[] => {
    return list.map(item => {
      return !item?.children?.length
        ? getItem(item.meta?.title, item.path, <Icon name={item.meta!.icon!} />)
        : getItem(item.meta?.title, item.path, <Icon name={item.meta!.icon!} />, handleMenuAsAntdFormat(item.children!));
    });
  };

  const antdMenuList = useMemo(() => handleMenuAsAntdFormat(menuList ?? showMenuList), [menuList, showMenuList]);

  useEffect(() => {
    const meta = matches[matches.length - 1].data as MetaProps;
    // Set Selected Keys
    const path = meta?.activeMenu ?? pathname;
    setSelectedKeys([path]);

    // Set Split Selected Keys (find can be found to represent children)
    const splitPath = `/${path.split("/")[1]}`;
    const splitKeys = showMenuList.find(item => item.path === splitPath) ? splitPath : path;
    setSplitSelectedKeys([splitKeys]);

    // Use setTimeout to prevent style exceptions from menu expansion
    if (accordion) setTimeout(() => isCollapse || setOpenKeys(getOpenKeys(pathname)));
  }, [matches, isCollapse]);

  const onOpenChange: MenuProps["onOpenChange"] = openKeys => {
    if (openKeys.length === 0 || openKeys.length === 1) return setOpenKeys(openKeys);
    const latestOpenKey = openKeys[openKeys.length - 1];
    if (latestOpenKey.includes(openKeys[0])) return setOpenKeys(openKeys);
    setOpenKeys([latestOpenKey]);
  };

  const handleMenuNavigation = (path: string) => {
    const menuItem = flatMenuList.find(item => item.path === path);
    if (menuItem?.meta?.isLink) window.open(menuItem.meta.isLink, "_blank");
    navigate(path);
  };

  const clickMenu: MenuProps["onClick"] = ({ key }) => {
    // If not split menu
    if (!menuSplit) return handleMenuNavigation(key);

    // If split menu
    const children = showMenuList.find(item => item.path === key)?.children;
    if (children?.length) return handleMenuNavigation(children[0].path!);
    handleMenuNavigation(key);
  };

  const isClassicLayout = useMemo(() => layout === "classic", [layout]);
  const isTransverseLayout = useMemo(() => layout === "transverse", [layout]);

  const isDarkTheme = useMemo(() => {
    if (isDark) return true;
    if (headerInverted && isTransverseLayout) return true;
    if (headerInverted && isClassicLayout && menuSplit) return true;
    if (siderInverted && !isTransverseLayout && !menuSplit) return true;
    return false;
  }, [layout, isDark, headerInverted, siderInverted, menuSplit]);

  return (
    <Menu
      theme={isDarkTheme ? "dark" : "light"}
      mode={mode}
      selectedKeys={menuSplit ? splitSelectedKeys : selectedKeys}
      onClick={clickMenu}
      items={antdMenuList}
      {...(!isTransverseLayout && accordion && { openKeys, onOpenChange })}
    />
  );
};

export default LayoutMenu;
