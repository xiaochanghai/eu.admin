import http from "@/api";
import { ResultData } from "@/api/interface";

/** 配置项 */
export interface SmConfigItem {
  ID: string;
  ConfigGroupId?: string;
  ConfigName: string;
  ConfigCode: string;
  ConfigValue: string;
  InputType: string; // SWITCH | INPUT | SELECT | NUMBER | TEXTAREA
  AvailableValue?: string; // SELECT 选项格式: "标签1:值1;标签2:值2"
  Sequence?: number;
  Remark?: string;
}

/** 配置分组（含明细） */
export interface SmConfigGroupView {
  ID: string;
  ParentId?: string;
  Name: string;
  Type?: string;
  Sequence?: number;
  detail: SmConfigItem[];
}

/**
 * 获取系统参数（按分组）
 */
export const getConfigListByGroup = () => {
  return http.get<SmConfigGroupView[]>("/api/SmConfig/GetListByGroup");
};

/** 后端允许提供给前端的配置，主题键不含 theme. 前缀，值保持字符串形式。 */
export interface ClientConfig {
  Theme: Record<string, string>;
}

/** 获取前端配置，使用统一错误处理并支持取消请求。 */
export const getClientConfig = (signal: AbortSignal) =>
  http.get<ClientConfig>("/api/SmConfig/GetClientConfig", undefined, { signal });

/**
 * 更新单个系统参数
 */
export const updateConfig = (params: Partial<SmConfigItem>) => {
  return http.put<any>(`/api/SmConfig/${params.ID}`, params);
};

/** 新增参数；服务端返回新记录 ID。 */
export const createConfig = (params: Omit<SmConfigItem, "ID">) => {
  return http.post<string>("/api/SmConfig", params);
};

/**
 * 批量保存系统参数
 */
export const batchUpdateConfigs = async (configs: Partial<SmConfigItem>[]) => {
  const results: ResultData<any>[] = [];
  for (const config of configs) {
    const result = await updateConfig(config);
    results.push(result);
  }
  return results;
};
