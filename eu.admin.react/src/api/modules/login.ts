import http from "@/api";
import { AuthState } from "@/redux/interface";
import { ReqLogin, ResLogin } from "@/api/interface/index";
// import authMenuList from "@/assets/json/authMenuList.json";
// import authMenuList1 from "@/assets/json/authMenuList1.json";
import authButtonList from "@/assets/json/authButtonList.json";

/**
 * @name AuthModule
 */
// User login
export const loginApi = (params: ReqLogin) => {
  return http.post<ResLogin>(`api/Authorize/Login`, params);
};

// Get menu list
export const getAuthMenuListApi = () => {
  return http.get<AuthState["authMenuList"]>(`api/SmModule/GetAuthMenu`);
  // return authMenuList1;
};

// Get button permissions
export const getAuthButtonListApi = () => {
  // return http.get<AuthState["authButtonList"]>(PORT1 + `/auth/buttons`);
  return authButtonList;
};

// User logout
export const logoutApi = () => {
  return http.get(`api/Authorize/LogOut`, {}, { loading: true });
};
