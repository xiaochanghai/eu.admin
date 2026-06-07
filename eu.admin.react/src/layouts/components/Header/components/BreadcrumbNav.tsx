import React, { useEffect, useMemo, useState } from "react";
import { Breadcrumb } from "antd";
import { ItemType } from "antd/es/breadcrumb/Breadcrumb";
import { MetaProps, RouteObjectType } from "@/routers/interface";
import { Link, useMatches } from "react-router-dom";
import { RootState, useSelector } from "@/redux";
import { getAllBreadcrumbList } from "@/utils";
import { Icon } from "@/components";
import { HOME_URL } from "@/config";

const BreadcrumbNav: React.FC = () => {
  const matches = useMatches();

  const authMenuList = useSelector((state: RootState) => state.auth.authMenuList);
  const breadcrumb = useSelector((state: RootState) => state.global.breadcrumb);
  const breadcrumbIcon = useSelector((state: RootState) => state.global.breadcrumbIcon);
  const language = useSelector((state: RootState) => state.global.language);
  const breadcrumbAllList = useMemo(() => getAllBreadcrumbList(authMenuList), [authMenuList]);

  const [curBreadcrumbList, setCurBreadcrumbList] = useState<ItemType[]>([]);

  // 根据当前语言获取菜单标题
  const getMenuTitle = (item: RouteObjectType) => {
    if (language === "en" && item.meta?.titleEn) return item.meta.titleEn;
    return item.meta?.title;
  };

  // Render Title
  const renderTitle = (item: RouteObjectType, isLink: boolean) => {
    const { icon } = item.meta || {};
    const content = (
      <React.Fragment>
        {breadcrumbIcon && icon && (
          <span className="mr5">
            <Icon name={icon!} />
          </span>
        )}
        <span>{getMenuTitle(item)}</span>
      </React.Fragment>
    );
    return isLink ? <Link to={item.path!}>{content}</Link> : content;
  };

  useEffect(() => {
    const meta = matches[matches.length - 1].data as MetaProps;
    if (!meta?.key) return;

    let breadcrumbList = breadcrumbAllList[meta.key] || [];

    // You don’t need breadcrumbs on the home page, you can delete the following judgments
    if (breadcrumbList[0]?.path !== HOME_URL) {
      breadcrumbList.unshift({
        path: HOME_URL,
        meta: { icon: "HomeOutlined", title: "首页", titleEn: "Home" }
      });
    }

    // Processed into the format required by antd breadcrumbs
    const antdBreadcrumbList = breadcrumbList.map(item => {
      const isLast = breadcrumbList.lastIndexOf(item) === breadcrumbList.length - 1;

      // The last breadcrumb is not clickable
      if (isLast) return { title: renderTitle(item, false) };

      // Render breadcrumb children
      if (item.children) {
        const items = item.children.filter(child => !child.meta?.isHide);
        return items.length
          ? {
              dropdownProps: { arrow: true },
              title: <a>{renderTitle(item, false)}</a>,
              menu: {
                items: items.map(child => {
                  return { title: renderTitle(child, true) };
                })
              }
            }
          : { title: renderTitle(item, true) };
      }

      // Other breadcrumb
      return { title: renderTitle(item, true) };
    });

    setCurBreadcrumbList(antdBreadcrumbList);
  }, [matches, breadcrumbIcon, language]);

  return <React.Fragment>{breadcrumb && <Breadcrumb items={curBreadcrumbList}></Breadcrumb>}</React.Fragment>;
};

export default BreadcrumbNav;
