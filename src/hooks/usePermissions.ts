import { notification } from "@/hooks/useMessage";
import { useDispatch } from "@/redux";
import { setToken } from "@/redux/modules/user";
import { setAuthButtonList, setAuthMenuList } from "@/redux/modules/auth";
import { getAuthMenuListApi, getAuthButtonListApi } from "@/api/modules/login";

/**
 * @description  Use permissions hook
 */
const usePermissions = () => {
  const dispatch = useDispatch();

  const initPermissions = async (token: string) => {
    if (token) {
      try {
        // Get button list
        const { Data: buttonList } = await getAuthButtonListApi();
        dispatch(setAuthButtonList(buttonList));

        // Get menu list
        const { Data: menuList } = await getAuthMenuListApi();
        dispatch(setAuthMenuList(menuList));

        // No menu permission
        if (!menuList.length) {
          notification.warning({
            message: "无权限访问",
            description: "当前账号无任何菜单权限，请联系系统管理员！"
          });
          dispatch(setToken(""));
          return Promise.reject("No permission");
        }
      } catch (error) {
        // When the button | | menu request error occurs, clear the token
        dispatch(setToken(""));
        return Promise.reject(error);
      }
    }
  };

  return { initPermissions };
};

export default usePermissions;
