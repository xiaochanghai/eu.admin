import { ReqPage, ResPage, UserList } from "@/api/interface/index";
import { PORT1 } from "@/api/config/servicePort";
import http from "@/api";

/**
 * @name UserModule
 */
// Get user list
export const getUserList = (params: ReqPage) => {
  return http.post<ResPage<UserList>>(PORT1 + `/user/list`, params);
};
