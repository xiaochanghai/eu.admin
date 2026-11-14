import { useEffect } from "react";
import { getClientConfig } from "@/api/modules/smConfig";
import { readThemeConfig } from "@/config/theme";
import { store, useDispatch, useSelector } from "@/redux";
import { initializeTheme } from "@/redux/modules/global";
import { message } from "@/hooks/useMessage";

/** 登录会话初始化时读取系统主题配置并写入 Redux，页面样式由 useTheme 应用。 */
export default function useInitSystemTheme() {
  const dispatch = useDispatch();
  const token = useSelector(state => state.user.token);

  useEffect(() => {
    dispatch(initializeTheme(null));
    if (!token) return;
    const controller = new AbortController();
    const isCurrent = () => !controller.signal.aborted && store.getState().user.token === token;
    const load = async () => {
      // 请求错误已由统一拦截器处理，此处仅终止本次加载。
      const result = await getClientConfig(controller.signal).catch(() => null);
      if (!isCurrent() || !result) return;
      const theme = result.Data?.Theme;
      if (!result.Success || !theme || typeof theme !== "object" || Array.isArray(theme)) {
        message.warning("系统主题配置格式无效，请检查配置。");
        return;
      }
      const items = Object.entries(theme)
        .filter(([, value]) => typeof value === "string")
        .map(([key, ConfigValue]) => ({ ConfigCode: `theme.${key}`, ConfigValue }));
      dispatch(initializeTheme(readThemeConfig(items)));
    };
    void load();
    return () => controller.abort();
  }, [token, dispatch]);
}
