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

/**
 * 获取栏位多语配置（RefType=ModuleColumn）
 * @param columnId 栏位ID
 * @returns 多语配置列表
 */
export const getColumnLanguageConfig = (columnId: string, refField: string) => {
  return http.get<any>(`/api/SmLanguageConfig/ByColumn/${columnId}/${refField}`);
};

/**
 * 批量保存栏位多语配置
 * @param configs 配置数组（有 Id 走 PUT，无 Id 走 POST）
 * @returns 操作结果
 */
export const saveColumnLanguageConfigs = async (configs: Array<Record<string, any>>) => {
  const results = [];
  for (const config of configs) {
    const result = config.Id ? await updateLanguageConfig(config) : await addLanguageConfig(config);
    results.push(result);
  }
  return results;
};
