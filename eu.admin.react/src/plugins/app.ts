import { Button } from "antd";
import { createElement } from "react";
import i18n from "@/languages/index";

// 防止重复注册事件监听器和重复显示通知
let isListenerRegistered = false;
let isShow = false;
const NOTIFICATION_KEY = "app-version-update";

export function setupAppVersionNotification() {
  const canAutoUpdateApp = import.meta.env.VITE_AUTOMATICALLY_DETECT_UPDATE === "Y";
  const { lastBuildTime } = __APP_INFO__;
  if (!canAutoUpdateApp || isListenerRegistered) return;

  // 标记已注册，防止重复
  isListenerRegistered = true;

  document.addEventListener("visibilitychange", async () => {
    const preConditions = [!isShow, document.visibilityState === "visible", !import.meta.env.DEV];
    if (!preConditions.every(Boolean)) return;

    // 立即设置标志，防止并发调用
    isShow = true;

    try {
      const buildTime = await getHtmlBuildTime();
      if (buildTime === lastBuildTime) {
        // 没有更新，重置标志以便下次检查
        isShow = false;
        return;
      }

      // 检测到新版本，显示通知
      window.$notification?.open({
        key: NOTIFICATION_KEY,
        btn: (() => {
          return createElement("div", { style: { display: "flex", gap: "12px", justifyContent: "end", width: "325px" } }, [
            createElement(
              Button,

              {
                key: "cancel",
                onClick() {
                  isShow = false;
                  window.$notification?.destroy(NOTIFICATION_KEY);
                }
              },
              i18n.t("system.updateCancel")
            ),
            createElement(
              Button,
              {
                key: "ok",
                async onClick() {
                  window.$notification?.destroy(NOTIFICATION_KEY);

                  // 清除所有缓存并强制刷新
                  if ("caches" in window) {
                    const names = await caches.keys();
                    await Promise.all(names.map(name => caches.delete(name)));
                  }

                  // 清除 sessionStorage 和 localStorage 中可能的缓存标记
                  sessionStorage.clear();

                  // 使用 location.reload() 强制从服务器重新加载，绕过缓存
                  window.location.reload();
                },
                type: "primary"
              },
              i18n.t("system.updateConfirm")
            )
          ]);
        })(),
        description: i18n.t("system.updateContent"),
        title: i18n.t("system.updateTitle"),
        duration: 0, // 不自动关闭
        onClose() {
          isShow = false;
        }
      });
    } catch (error) {
      // 检查失败，重置标志以便下次检查
      console.error("Failed to check for updates:", error);
      isShow = false;
    }
  });
}

async function getHtmlBuildTime() {
  const res = await fetch(`/index.html?time=${Date.now()}`, {
    headers: {
      "Cache-Control": "no-cache"
    }
  });

  const html = await res.text();
  // 定义正则表达式，添加全局标志 g
  const regex = /<meta\s+name="buildTime"\s+content="([^"]+)"/i;
  const match = html.match(regex);

  const buildTime = match?.[1] || "";

  return buildTime;
}
