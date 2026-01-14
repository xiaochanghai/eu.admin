import { useCallback, useMemo } from "react";
import { Drawer, Divider, Switch, Popover, InputNumber, Tooltip } from "antd";
import { setGlobalState } from "@/redux/modules/global";
import { RootState, useDispatch, useSelector } from "@/redux";
import { LayoutType, GlobalState } from "@/redux/interface";
import ColorPicker from "./components/ColorPicker";
import "./index.less";
import { Icon } from "@/components";

interface LayoutConfig {
  key: LayoutType;
  title: string;
  className: string;
  renderContent: () => JSX.Element;
}

interface ThemeItemProps {
  label: string;
  tooltip?: string;
  checked?: boolean;
  disabled?: boolean;
  onChange: (value: boolean) => void;
  checkedChildren?: React.ReactNode;
  unCheckedChildren?: React.ReactNode;
  className?: string;
}

interface ThemeSectionProps {
  icon: string;
  title: string;
  children: React.ReactNode;
}

const ThemeItem: React.FC<ThemeItemProps> = ({
  label,
  tooltip,
  checked,
  disabled,
  onChange,
  checkedChildren,
  unCheckedChildren,
  className
}) => (
  <div className={`theme-item ${className || ""}`}>
    <span>
      {label}
      {tooltip && (
        <Tooltip title={tooltip}>
          <span>
            <Icon name="QuestionCircleOutlined" />
          </span>
        </Tooltip>
      )}
    </span>
    <Switch
      checked={checked}
      disabled={disabled}
      onChange={onChange}
      checkedChildren={checkedChildren}
      unCheckedChildren={unCheckedChildren}
    />
  </div>
);

const ThemeSection: React.FC<ThemeSectionProps> = ({ icon, title, children }) => (
  <>
    <Divider className="divider">
      <Icon name={icon} />
      {title}
    </Divider>
    {children}
  </>
);

const ThemeDrawer: React.FC = () => {
  const dispatch = useDispatch();
  const {
    layout,
    compactAlgorithm,
    borderRadius,
    isDark,
    isGrey,
    isWeak,
    isHappy,
    menuSplit,
    siderInverted,
    headerInverted,
    isCollapse,
    accordion,
    watermark,
    breadcrumb,
    breadcrumbIcon,
    tabs,
    tabsIcon,
    tabsDrag,
    footer,
    themeDrawerVisible
  } = useSelector((state: RootState) => state.global);

  const handleCloseDrawer = useCallback(() => {
    dispatch(setGlobalState({ key: "themeDrawerVisible", value: false }));
  }, [dispatch]);

  const handleLayoutChange = useCallback(
    (value: LayoutType) => {
      dispatch(setGlobalState({ key: "layout", value }));
    },
    [dispatch]
  );

  const handleStateChange = useCallback(
    <K extends keyof GlobalState>(key: K, value: GlobalState[K]) => {
      dispatch(setGlobalState({ key, value } as any));
    },
    [dispatch]
  );

  const handleGreyModeChange = useCallback(
    (value: boolean) => {
      if (isWeak) handleStateChange("isWeak", false);
      handleStateChange("isGrey", value);
    },
    [isWeak, handleStateChange]
  );

  const handleWeakModeChange = useCallback(
    (value: boolean) => {
      if (isGrey) handleStateChange("isGrey", false);
      handleStateChange("isWeak", value);
    },
    [isGrey, handleStateChange]
  );

  const layoutConfigs: LayoutConfig[] = useMemo(
    () => [
      {
        key: "vertical",
        title: "纵向",
        className: "layout-vertical",
        renderContent: () => (
          <>
            <div className="layout-dark"></div>
            <div className="layout-container">
              <div className="layout-light"></div>
              <div className="layout-content"></div>
            </div>
          </>
        )
      },
      {
        key: "classic",
        title: "经典",
        className: "layout-classic",
        renderContent: () => (
          <>
            <div className="layout-dark"></div>
            <div className="layout-container">
              <div className="layout-light"></div>
              <div className="layout-content"></div>
            </div>
          </>
        )
      },
      {
        key: "transverse",
        title: "横向",
        className: "layout-transverse",
        renderContent: () => (
          <>
            <div className="layout-dark"></div>
            <div className="layout-content"></div>
          </>
        )
      },
      {
        key: "columns",
        title: "分栏",
        className: "layout-columns",
        renderContent: () => (
          <>
            <div className="layout-dark"></div>
            <div className="layout-light"></div>
            <div className="layout-content"></div>
          </>
        )
      }
    ],
    []
  );

  return (
    <Drawer
      title="主题配置"
      size={290}
      zIndex={999}
      closable={false}
      maskClosable={true}
      open={themeDrawerVisible}
      className="theme-drawer"
      onClose={handleCloseDrawer}
    >
      {/* Layout Switching */}
      <ThemeSection icon="LayoutOutlined" title="布局样式">
        <div className="layout-box">
          {layoutConfigs.map(config => (
            <Tooltip key={config.key} placement="top" title={config.title} arrow={true} mouseEnterDelay={0.2}>
              <div
                className={`layout-item ${config.className} ${
                  config.key === "transverse" || config.key === "columns" ? "" : "mb22"
                } ${layout === config.key ? "layout-active" : ""}`}
                onClick={() => handleLayoutChange(config.key)}
              >
                {config.renderContent()}
                {layout === config.key && <Icon name="CheckCircleFilled" />}
              </div>
            </Tooltip>
          ))}
        </div>

        <ThemeItem
          label="菜单分割"
          tooltip="经典模式下生效"
          checked={menuSplit}
          disabled={layout !== "classic"}
          onChange={value => handleStateChange("menuSplit", value)}
          className="mt30"
        />
        <ThemeItem
          label="侧边栏反转色"
          tooltip="侧边栏颜色变为深色模式"
          checked={siderInverted}
          onChange={value => handleStateChange("siderInverted", value)}
        />
        <ThemeItem
          label="头部反转色"
          tooltip="头部颜色变为深色模式"
          checked={headerInverted}
          onChange={value => handleStateChange("headerInverted", value)}
          className="mb35"
        />
      </ThemeSection>

      {/* Theme Settings */}
      <ThemeSection icon="FireOutlined" title="全局主题">
        <div className="theme-item">
          <span>主题颜色</span>
          <Popover placement="left" trigger="click" content={ColorPicker}>
            <label className="primary"></label>
          </Popover>
        </div>
        <ThemeItem
          label="暗黑模式"
          checked={isDark}
          onChange={value => handleStateChange("isDark", value)}
          checkedChildren={<span className="dark-icon dark-icon-sun">🌞</span>}
          unCheckedChildren={<span className="dark-icon dark-icon-moon">🌛</span>}
        />
        <ThemeItem label="灰色模式" checked={isGrey} onChange={handleGreyModeChange} />
        <ThemeItem label="色弱模式" checked={isWeak} onChange={handleWeakModeChange} />
        <ThemeItem label="快乐模式" checked={isHappy} onChange={value => handleStateChange("isHappy", value)} />
        <ThemeItem
          label="紧凑主题"
          checked={compactAlgorithm}
          onChange={value => handleStateChange("compactAlgorithm", value)}
        />
        <div className="theme-item mb35">
          <span>圆角大小</span>
          <InputNumber
            min={1}
            max={20}
            style={{ width: 80 }}
            defaultValue={borderRadius}
            formatter={value => `${value}px`}
            parser={value => (value ? value!.replace("px", "") : 6) as number}
            onChange={value => handleStateChange("borderRadius", value || 6)}
          />
        </div>
      </ThemeSection>

      {/* Interface Settings */}
      <ThemeSection icon="SettingOutlined" title="界面设置">
        <ThemeItem label="菜单折叠" checked={isCollapse} onChange={value => handleStateChange("isCollapse", value)} />
        <ThemeItem label="菜单手风琴" checked={accordion} onChange={value => handleStateChange("accordion", value)} />
        <ThemeItem label="水印" checked={watermark} onChange={value => handleStateChange("watermark", value)} />
        <ThemeItem label="面包屑" checked={breadcrumb} onChange={value => handleStateChange("breadcrumb", value)} />
        <ThemeItem
          label="面包屑图标"
          checked={breadcrumbIcon}
          onChange={value => handleStateChange("breadcrumbIcon", value)}
        />
        <ThemeItem label="标签栏" checked={tabs} onChange={value => handleStateChange("tabs", value)} />
        <ThemeItem label="标签栏图标" checked={tabsIcon} onChange={value => handleStateChange("tabsIcon", value)} />
        <ThemeItem label="标签栏拖拽" checked={tabsDrag} onChange={value => handleStateChange("tabsDrag", value)} />
        <ThemeItem label="页脚" checked={footer} onChange={value => handleStateChange("footer", value)} />
      </ThemeSection>
    </Drawer>
  );
};

export default ThemeDrawer;
