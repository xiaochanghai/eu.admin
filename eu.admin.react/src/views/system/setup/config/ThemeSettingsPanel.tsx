import type { ThemeSettings } from "./themeConfig";

import { useCallback, useMemo } from "react";
import { ConfigProvider, Divider, Switch, Popover, InputNumber, Tooltip } from "antd";
import ColorPicker from "@/layouts/components/ThemeDrawer/components/ColorPicker";
import { LayoutType } from "@/redux/interface";
import "@/layouts/components/ThemeDrawer/index.less";
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

export const ThemeSettingsPanel = ({
  value,
  onChange,
  disabled = false
}: {
  value: ThemeSettings;
  onChange: (patch: Partial<ThemeSettings>) => void;
  disabled?: boolean;
}) => {
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
    footer
  } = value;
  const handleLayoutChange = (layout: LayoutType) => onChange({ layout });
  const handleStateChange = useCallback(
    <K extends keyof ThemeSettings>(key: K, value: ThemeSettings[K]) => {
      onChange({ [key]: value });
    },
    [onChange]
  );

  const handleGreyModeChange = useCallback(
    (value: boolean) => {
      onChange({ isGrey: value, ...(value ? { isWeak: false } : {}) });
    },
    [onChange]
  );

  const handleWeakModeChange = useCallback(
    (value: boolean) => {
      onChange({ isWeak: value, ...(value ? { isGrey: false } : {}) });
    },
    [onChange]
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
    <ConfigProvider componentDisabled={disabled}>
      <div className="theme-drawer">
        {/* Layout Switching */}
        <ThemeSection icon="LayoutOutlined" title="布局样式">
          <div className="layout-box" role="radiogroup" aria-label="布局样式">
            {layoutConfigs.map(config => (
              <Tooltip key={config.key} placement="top" title={config.title} arrow={true} mouseEnterDelay={0.2}>
                <div
                  className={`layout-item ${config.className} ${
                    config.key === "transverse" || config.key === "columns" ? "" : "mb22"
                  } ${layout === config.key ? "layout-active" : ""}`}
                  role="radio"
                  aria-label={config.title}
                  aria-checked={layout === config.key}
                  aria-disabled={disabled}
                  tabIndex={disabled ? -1 : 0}
                  onKeyDown={event => {
                    if (!disabled && (event.key === "Enter" || event.key === " ")) {
                      event.preventDefault();
                      handleLayoutChange(config.key);
                    }
                  }}
                  onClick={() => !disabled && handleLayoutChange(config.key)}
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
            disabled={disabled || layout !== "classic"}
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
            <Popover
              placement="left"
              trigger={disabled ? [] : ["click"]}
              open={disabled ? false : undefined}
              content={<ColorPicker value={value.primary} onChange={primary => onChange({ primary })} disabled={disabled} />}
            >
              <button
                type="button"
                aria-label="主题颜色"
                className="primary"
                disabled={disabled}
                style={{
                  border: 0,
                  padding: 0,
                  boxShadow: `var(--hooks-colorBgContainer) 0 0 0 1px, ${value.primary} 0 0 0 5px`
                }}
              />
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
              precision={0}
              style={{ width: 80 }}
              value={borderRadius}
              formatter={value => `${value}px`}
              parser={value => Number(value?.replace("px", "") || 6)}
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
          <ThemeItem label="面包屑图标" checked={breadcrumbIcon} onChange={value => handleStateChange("breadcrumbIcon", value)} />
          <ThemeItem label="标签栏" checked={tabs} onChange={value => handleStateChange("tabs", value)} />
          <ThemeItem label="标签栏图标" checked={tabsIcon} onChange={value => handleStateChange("tabsIcon", value)} />
          <ThemeItem label="标签栏拖拽" checked={tabsDrag} onChange={value => handleStateChange("tabsDrag", value)} />
          <ThemeItem label="页脚" checked={footer} onChange={value => handleStateChange("footer", value)} />
        </ThemeSection>
      </div>
    </ConfigProvider>
  );
};

export default ThemeSettingsPanel;
