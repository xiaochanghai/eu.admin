import { DEFAULT_PRIMARY } from "@/config";
import type { GlobalState } from "@/redux/interface";
import type { SmConfigGroupView, SmConfigItem } from "@/api/modules/smConfig";

export const defaultTheme = {
  layout: "vertical",
  primary: DEFAULT_PRIMARY,
  borderRadius: 6,
  compactAlgorithm: false,
  isDark: false,
  isGrey: false,
  isWeak: false,
  isHappy: false,
  menuSplit: true,
  siderInverted: false,
  headerInverted: false,
  isCollapse: false,
  accordion: true,
  watermark: false,
  breadcrumb: true,
  breadcrumbIcon: true,
  tabs: true,
  tabsIcon: true,
  tabsDrag: true,
  footer: true
} satisfies Partial<GlobalState>;

export type ThemeSettings = Pick<GlobalState, keyof typeof defaultTheme>;
export type SystemThemeSettings = ThemeSettings & { allowCustom: boolean };
export const defaultSystemTheme: SystemThemeSettings = { ...defaultTheme, allowCustom: true };

const names: Record<keyof SystemThemeSettings, string> = {
  layout: "布局样式",
  primary: "主题颜色",
  borderRadius: "圆角大小",
  compactAlgorithm: "紧凑主题",
  isDark: "暗黑模式",
  isGrey: "灰色模式",
  isWeak: "色弱模式",
  isHappy: "快乐模式",
  menuSplit: "菜单分割",
  siderInverted: "侧边栏反转色",
  headerInverted: "头部反转色",
  isCollapse: "菜单折叠",
  accordion: "菜单手风琴",
  watermark: "水印",
  breadcrumb: "面包屑",
  breadcrumbIcon: "面包屑图标",
  tabs: "标签栏",
  tabsIcon: "标签栏图标",
  tabsDrag: "标签栏拖拽",
  footer: "页脚",
  allowCustom: "允许用户自定义主题"
};
const keys = Object.keys(names) as (keyof SystemThemeSettings)[];
const layouts = ["vertical", "classic", "transverse", "columns"] as const;

/** Only known, valid settings are consumed from persisted configuration. */
export const readThemeConfig = (items: Pick<SmConfigItem, "ConfigCode" | "ConfigValue">[] | null = []): SystemThemeSettings => {
  const result = { ...defaultSystemTheme };
  for (const key of keys) {
    const raw = items?.find(item => item.ConfigCode === `theme.${key}`)?.ConfigValue;
    if (typeof raw !== "string") continue;
    if (key === "layout") {
      const layout = layouts.find(layout => layout === raw);
      if (layout) result.layout = layout;
    } else if (key === "primary") {
      if (/^#[0-9a-f]{6}$/i.test(raw)) result.primary = raw;
    } else if (key === "borderRadius") {
      const value = Number(raw);
      if (Number.isInteger(value) && value >= 1 && value <= 20) result.borderRadius = value;
    } else if (["Y", "true", "1"].includes(raw)) {
      result[key] = true;
    } else if (["N", "false", "0"].includes(raw)) {
      result[key] = false;
    }
  }
  // Match the panel's mutually exclusive accessibility modes.
  if (result.isGrey) result.isWeak = false;
  return result;
};

export type ThemeConfigChange = Omit<SmConfigItem, "ID"> & { ID?: string };

export const getThemeConfigChanges = (group: SmConfigGroupView, value: SystemThemeSettings): ThemeConfigChange[] =>
  keys.flatMap((key, index) => {
    const code = `theme.${key}`;
    const existing = group.detail?.find(item => item.ConfigCode === code);
    const configValue = typeof value[key] === "boolean" ? (value[key] ? "Y" : "N") : String(value[key]);
    if (existing?.ConfigValue === configValue) return [];
    return [
      {
        ...(existing?.ID ? { ID: existing.ID } : {}),
        ConfigGroupId: group.ID,
        ConfigName: names[key],
        ConfigCode: code,
        ConfigValue: configValue,
        InputType:
          typeof value[key] === "boolean" ? "SWITCH" : key === "borderRadius" ? "NUMBER" : key === "layout" ? "SELECT" : "INPUT",
        ...(key === "layout" ? { AvailableValue: "纵向:vertical;经典:classic;横向:transverse;分栏:columns" } : {}),
        Sequence: index
      }
    ];
  });
