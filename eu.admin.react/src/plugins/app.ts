import { Button } from "antd";
import { createElement } from "react";
//  import { useTranslation } from "react-i18next";

//   import { $t } from "../languages/index";

export function setupAppVersionNotification() {
  const canAutoUpdateApp = import.meta.env.VITE_AUTOMATICALLY_DETECT_UPDATE === "Y";
  const { lastBuildTime } = __APP_INFO__;
  if (!canAutoUpdateApp) return;
  let isShow = false;

  document.addEventListener("visibilitychange", async () => {
    const preConditions = [!isShow, document.visibilityState === "visible", !import.meta.env.DEV];
    if (!preConditions.every(Boolean)) return;

    const buildTime = await getHtmlBuildTime();
    if (buildTime === lastBuildTime) return;

    isShow = true;

    window.$notification?.open({
      btn: (() => {
        return createElement("div", { style: { display: "flex", gap: "12px", justifyContent: "end", width: "325px" } }, [
          createElement(
            Button,

            {
              key: "cancel",
              onClick() {
                window.$notification?.destroy();
              }
            },
            "稍后再说"
            // $t("system.updateCancel")
          ),
          createElement(
            Button,
            {
              key: "ok",
              onClick() {
                location.reload();
              },
              type: "primary"
            },
            "立即刷新"
            // $t("system.updateConfirm")
          )
        ]);
      })(),
      description: "检测到系统有新版本发布，是否立即刷新页面？",
      // $t("system.updateContent")
      message: "系统版本更新通知",
      // t("system.updateTitle")
      onClose() {
        isShow = false;
      }
    });
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
