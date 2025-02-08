import React, { useContext, useEffect } from "react";
import { Dropdown, Tabs, MenuProps } from "antd";
import { CSS } from "@dnd-kit/utilities";
import { useUpdateEffect } from "ahooks";
import { type DragEndEvent, DndContext, PointerSensor, useSensor } from "@dnd-kit/core";
import { arrayMove, horizontalListSortingStrategy, SortableContext, useSortable } from "@dnd-kit/sortable";
import { useLocation, useMatches, useNavigate } from "react-router-dom";
import { RootState, useDispatch, useSelector } from "@/redux";
import { addTab, removeTab, setTabsList, closeMultipleTab, closeTabsOnSide } from "@/redux/modules/tabs";
import { TabsListProp } from "@/redux/interface";
import { MetaProps } from "@/routers/interface";
import { Icon } from "@/components";
// import MoreButton from "./components/MoreButton";
import { useTranslation } from "react-i18next";
import { setGlobalState } from "@/redux/modules/global";
import { RefreshContext } from "@/context/Refresh";
import { HOME_URL } from "@/config";
import "./index.less";

type TargetKey = string | React.MouseEvent<Element, MouseEvent> | React.KeyboardEvent<Element>;

interface DraggableTabPaneProps extends React.HTMLAttributes<HTMLDivElement> {
  "data-node-key": string;
}

const DraggableTabNode = ({ ...props }: DraggableTabPaneProps) => {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({
    id: props["data-node-key"]
  });

  const style: React.CSSProperties = {
    ...props.style,
    transform: CSS.Transform.toString(transform && { ...transform, scaleX: 1 }),
    transition
  };

  return React.cloneElement(props.children as React.ReactElement, {
    ref: setNodeRef,
    style,
    ...attributes,
    ...listeners
  });
};

const LayoutTabs: React.FC = () => {
  const matches = useMatches();
  const dispatch = useDispatch();
  const location = useLocation();
  const navigate = useNavigate();

  const path = location.pathname + location.search;

  const tabs = useSelector((state: RootState) => state.global.tabs);
  const tabsIcon = useSelector((state: RootState) => state.global.tabsIcon);
  const tabsDrag = useSelector((state: RootState) => state.global.tabsDrag);
  const tabsList = useSelector((state: RootState) => state.tabs.tabsList);
  const flatMenuList = useSelector((state: RootState) => state.auth.flatMenuList);

  const sensor = useSensor(PointerSensor, { activationConstraint: { distance: 10 } });

  useEffect(() => initTabs(), []);

  const initTabs = () => {
    flatMenuList.forEach(item => {
      if (item.meta?.isAffix && !item.meta.isHide && !item.meta.isFull) {
        const tabValue = {
          icon: item.meta.icon!,
          title: item.meta.title!,
          path: item.path!,
          closable: !item.meta.isAffix
        };
        dispatch(addTab(tabValue));
      }
    });
  };

  // Listen for route changes
  useUpdateEffect(() => {
    const meta = matches[matches.length - 1].data as MetaProps & { redirect: boolean };
    if (!meta?.redirect) {
      const tabValue = {
        icon: meta.icon!,
        title: meta.title!,
        path: path,
        closable: !meta.isAffix
      };
      dispatch(addTab(tabValue));
    }
  }, [matches]);

  const { t } = useTranslation();

  const { updateOutletShow, updateReload } = useContext(RefreshContext);

  const refreshCurrentPage = () => {
    updateOutletShow(false);
    updateReload(true);
    setTimeout(() => {
      updateOutletShow(true);
      updateReload(false);
    });
    // setTimeout(() => {
    // }, 6000);
  };
  const getIcon = (type: string) => {
    return <Icon name={type} className="font-size14" />;
  };
  const getMenuItems = (item: any, index: any) => {
    const opts: MenuProps["items"] = [
      {
        key: "1",
        label: <span>{t("tabs.refresh")}</span>,
        icon: getIcon("ReloadOutlined"),
        onClick: refreshCurrentPage
      },
      {
        key: "2",
        label: <span>{t("tabs.maximize")}</span>,
        icon: getIcon("ExpandOutlined"),
        onClick: () => dispatch(setGlobalState({ key: "maximize", value: true }))
      },
      {
        type: "divider"
      },
      {
        key: "3",
        label: <span>{t("tabs.closeCurrent")}</span>,
        icon: getIcon("CloseCircleOutlined"),
        onClick: () => dispatch(removeTab({ path, isCurrent: true })),
        disabled: !item.closable ? true : false
      },
      {
        key: "4",
        label: <span>{t("tabs.closeLeft")}</span>,
        icon: getIcon("VerticalRightOutlined"),
        onClick: () => dispatch(closeTabsOnSide({ path, type: "left" })),
        disabled: index == 0 ? true : false
      },
      {
        key: "5",
        label: <span>{t("tabs.closeRight")}</span>,
        icon: getIcon("VerticalLeftOutlined"),
        onClick: () => dispatch(closeTabsOnSide({ path, type: "right" })),
        disabled: index + 1 == tabsList.length ? true : false
      },
      {
        type: "divider"
      },
      {
        key: "6",
        label: <span>{t("tabs.closeOther")}</span>,
        icon: getIcon("ColumnWidthOutlined"),
        onClick: () => dispatch(closeMultipleTab({ path }))
      },
      {
        key: "7",
        label: <span>{t("tabs.closeAll")}</span>,
        icon: getIcon("SwitcherOutlined"),
        onClick: () => {
          dispatch(closeMultipleTab({}));
          navigate(HOME_URL);
        }
      }
    ];
    return opts;
  };
  const renderTabTitle = (item: any, index: number) => {
    return (
      <Dropdown menu={{ items: getMenuItems(item, index) }} trigger={["contextMenu"]}>
        <div style={{ margin: "-12px 0", padding: "12px 0" }}>
          <React.Fragment>
            {tabsIcon && <Icon name={item.icon} />}
            {item.title}
          </React.Fragment>
        </div>
      </Dropdown>
    );
  };
  const items = tabsList.map((item, index) => {
    return {
      key: item.path,
      label: renderTabTitle(item, index),
      closable: item.closable
    };
  });

  const onDragEnd = ({ active, over }: DragEndEvent) => {
    if (active.id !== over?.id) {
      const activeIndex = tabsList.findIndex(i => i.path === active.id);
      const overIndex = tabsList.findIndex(i => i.path === over?.id);
      dispatch(setTabsList(arrayMove<TabsListProp>(tabsList, activeIndex, overIndex)));
    }
  };

  const onChange = (path: string) => {
    navigate(path);
  };

  const onEdit = (targetKey: TargetKey, action: "add" | "remove") => {
    if (action === "remove" && typeof targetKey == "string") {
      dispatch(removeTab({ path: targetKey, isCurrent: targetKey == path }));
    }
  };
  return (
    <React.Fragment>
      {tabs && (
        <Tabs
          hideAdd
          type="editable-card"
          className="tabs-box"
          size="middle"
          items={items}
          activeKey={path}
          onEdit={onEdit}
          onChange={onChange}
          // tabBarExtraContent={<MoreButton path={path} />}
          {...(tabsDrag && {
            renderTabBar: (tabBarProps, DefaultTabBar) => (
              <DndContext sensors={[sensor]} onDragEnd={onDragEnd}>
                <SortableContext items={items.map(i => i.key)} strategy={horizontalListSortingStrategy}>
                  <DefaultTabBar {...tabBarProps}>
                    {node => (
                      <DraggableTabNode {...node.props} key={node.key}>
                        {node}
                      </DraggableTabNode>
                    )}
                  </DefaultTabBar>
                </SortableContext>
              </DndContext>
            )
          })}
        />
      )}
    </React.Fragment>
  );
};

export default LayoutTabs;
