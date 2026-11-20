import { notification } from "@/hooks/useMessage";
import { useCallback } from "react";
import { store, useDispatch } from "@/redux";
import { setToken } from "@/redux/modules/user";
import { setAuthButtonList, setAuthMenuList } from "@/redux/modules/auth";
import { getAuthMenuListApi, getAuthButtonListApi } from "@/api/modules/login";

let inFlight: { sessionVersion: number; promise: Promise<void> } | null = null;

/**
 * @description  Use permissions hook
 */
const usePermissions = () => {
  const dispatch = useDispatch();

  const initPermissions = useCallback(
    (token: string) => {
      if (!token) return Promise.resolve();
      if (store.getState().user.token !== token) {
        return Promise.reject(new DOMException("Permission initialization cancelled", "AbortError"));
      }
      const sessionVersion = store.getState().auth.sessionVersion;
      if (inFlight?.sessionVersion === sessionVersion) return inFlight.promise;
      const isCurrentSession = () =>
        store.getState().user.token === token && store.getState().auth.sessionVersion === sessionVersion;
      const assertCurrentSession = () => {
        if (!isCurrentSession()) throw new DOMException("Permission initialization cancelled", "AbortError");
      };
      const promise = (async () => {
        try {
          assertCurrentSession();
          // Get button list
          const { Data: buttonList } = await getAuthButtonListApi();
          assertCurrentSession();

          // Get menu list
          const { Data: menuList } = await getAuthMenuListApi();
          assertCurrentSession();

          // No menu permission
          if (!menuList.length) {
            notification.warning({
              title: "无权限访问",
              description: "当前账号无任何菜单权限，请联系系统管理员！"
            });
            dispatch(setToken(""));
            return Promise.reject("No permission");
          }
          dispatch(setAuthButtonList(buttonList));
          dispatch(setAuthMenuList(menuList));
        } catch (error) {
          // When the button | | menu request error occurs, clear the token
          if (isCurrentSession()) dispatch(setToken(""));
          return Promise.reject(error);
        } finally {
          if (inFlight?.sessionVersion === sessionVersion) inFlight = null;
        }
      })();
      inFlight = { sessionVersion, promise };
      return promise;
    },
    [dispatch]
  );

  return { initPermissions };
};

export default usePermissions;
