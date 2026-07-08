import http from "@/api";

/** 移动端页面配置 */
export interface SmMobilePageConfig {
  ID: string;
  PageCode: string;
  PageName: string;
  AppScope?: string;
  PageType?: string;
  Title?: string;
  Version?: number;
  ConfigJson?: string;
  IsPublished?: boolean;
  Remark?: string;
  CreatedTime?: string;
  CreatedBy?: string;
  UpdateTime?: string;
  UpdateBy?: string;
}

/** 查询列表 */
export const getMobilePageList = (params?: any) => {
  return http.get<SmMobilePageConfig[]>("/api/SmMobilePageConfig/QueryByFilter", params);
};

/** 根据ID查询 */
export const getMobilePageById = (id: string) => {
  return http.get<SmMobilePageConfig>(`/api/SmMobilePageConfig/${id}`);
};

/** 新增 */
export const createMobilePage = (data: Partial<SmMobilePageConfig>) => {
  return http.post<string>("/api/SmMobilePageConfig", data);
};

/** 更新 */
export const updateMobilePage = (id: string, data: Partial<SmMobilePageConfig>) => {
  return http.put<boolean>(`/api/SmMobilePageConfig/${id}`, data);
};

/** 删除 */
export const deleteMobilePage = (id: string) => {
  return http.delete<boolean>(`/api/SmMobilePageConfig/${id}`);
};

/** 发布配置 */
export const publishMobilePage = (id: string) => {
  return http.post<boolean>(`/api/SmMobilePageConfig/Publish/${id}`);
};
