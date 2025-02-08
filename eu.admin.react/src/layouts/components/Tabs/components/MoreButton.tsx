import { useContext } from "react";
import { Dropdown, MenuProps } from "antd";
import { HOME_URL } from "@/config";
import { useTranslation } from "react-i18next";
import { useDispatch } from "@/redux";
import { useNavigate } from "react-router-dom";
import { RefreshContext } from "@/context/Refresh";
import { setGlobalState } from "@/redux/modules/global";
import { removeTab, closeMultipleTab, closeTabsOnSide } from "@/redux/modules/tabs";
import { Icon } from "@/components";

interface MoreButtonProps {
  path: string;
}

const MoreButton: React.FC<MoreButtonProps> = ({ path }) => {
  const navigate = useNavigate();
  const dispatch = useDispatch();

  const { t } = useTranslation();

  const { updateOutletShow } = useContext(RefreshContext);

  const refreshCurrentPage = () => {
    updateOutletShow(false);
    setTimeout(() => updateOutletShow(true));
  };
  const getIcon = (type: string) => {
    return <Icon name={type} className="font-size14" />;
  };
  const items: MenuProps["items"] = [
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
      onClick: () => dispatch(removeTab({ path, isCurrent: true }))
    },
    {
      key: "4",
      label: <span>{t("tabs.closeLeft")}</span>,
      icon: getIcon("VerticalRightOutlined"),
      onClick: () => dispatch(closeTabsOnSide({ path, type: "left" }))
    },
    {
      key: "5",
      label: <span>{t("tabs.closeRight")}</span>,
      icon: getIcon("VerticalLeftOutlined"),
      onClick: () => dispatch(closeTabsOnSide({ path, type: "right" }))
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

  return (
    <div className="more-button">
      <Dropdown menu={{ items }} placement="bottomRight" arrow={true} trigger={["click"]}>
        <div className="more-button-item">
          <i className={"iconfont icon-xiala"}></i>
        </div>
      </Dropdown>
    </div>
  );
};

export default MoreButton;
