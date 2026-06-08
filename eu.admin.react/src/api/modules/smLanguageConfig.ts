import http from "@/api";

/**
 * 获取模块多语配置
 * @param moduleId 模块ID
 * @returns 多语配置数据
 */
export const getModuleLanguageConfig = (moduleId: string) => {
  return http.get<any>(`/api/SmLanguageConfig/ByModule/${moduleId}`);
};

/**
 * 添加多语配置
 * @param params 配置数据
 * @returns 操作结果
 */
export const addLanguageConfig = (params: Record<string, any>) => {
  return http.post<any>("/api/SmLanguageConfig", params);
};

/**
 * 更新多语配置
 * @param params 配置数据（需包含 Id）
 * @returns 操作结果
 */
export const updateLanguageConfig = (params: Record<string, any>) => {
  return http.put<any>(`/api/SmLanguageConfig/${params.Id}`, params);
};
