import http from "@/api";
import { ModuleInfo, RecordLogData } from "@/api/interface/index";

/**
 * 通用ID参数接口
 */
export interface IdParams {
  moduleCode: string;
  url: string;
  Id?: string | number | null;
}

/**
 * 批量操作参数接口
 */
export interface BatchParams {
  moduleCode: string;
  url: string;
  Ids: string[];
}

/**
 * 列配置记录参数接口
 */
export interface ColumnRecordParams {
  moduleId: string;
  map: Record<string, any>;
}

// ==================== 模块信息相关 API ====================

/**
 * 获取模块信息
 * @param moduleCode 模块代码
 * @returns 模块信息
 */
export const getModuleInfo = (moduleCode: string) => {
  return http.get<ModuleInfo>(`/api/SmModule/GetModuleInfo/${moduleCode}`);
};

/**
 * 获取模块日志信息
 * @param params 查询参数
 * @returns 记录日志数据
 */
export const getModuleLogInfo = (params: Record<string, any>) => {
  return http.get<RecordLogData>(`/api/SmModule/GetModuleLogInfo`, params);
};

/**
 * 记录用户模块列配置
 * @param params 列配置参数
 * @returns 操作结果
 */
export const recordUserModuleColumn = (params: ColumnRecordParams) => {
  return http.post<any>(`/api/SmModule/RecordUserModuleColumn/${params.moduleId}`, params.map);
};

/**
 * 导出模块SQL脚本
 * @param params 导出参数
 * @returns 导出结果
 */
export const exportModuleSqlScript = (params: Record<string, any>) => {
  return http.post<any>("/api/SmModule/ExportSqlScript", params);
};

/**
 * 获取模块完整SQL
 * @param id 模块ID
 * @returns SQL内容
 */
export const getModuleFullSql = (id: string) => {
  return http.post<any>(`/api/SmModuleSql/GetModuleFullSql/${id}`);
};

/**
 * 获取模块SQL信息
 * @param id 模块ID
 * @returns SQL信息
 */
export const getModuleSqlInfo = (id: string) => {
  return http.get<any>(`/api/SmModuleSql/ByModuleId/${id}`);
};

// ==================== 数据查询相关 API ====================

/**
 * 通用查询
 * @param params 查询参数
 * @returns 查询结果列表
 */
export const query = (params: Record<string, any>) => {
  return http.getGridList(`/api/Common/GetGridList`, params);
};

/**
 * 按过滤条件查询
 * @param moduleCode 模块代码
 * @param params 查询参数
 * @param filter 过滤条件
 * @returns 查询结果列表
 */
export const queryByFilter = (moduleCode: string, params: Record<string, any>, filter: any) => {
  return http.getGridList(`/api/Common/QueryByFilter/${moduleCode}`, params, { filter });
};

/**
 * 查询单条记录
 * @param params 包含url和Id的参数对象
 * @returns 单条记录数据
 */
export const querySingle = (params: IdParams) => {
  return http.get<any>(`${params.url}/${params.Id}`);
};

/**
 * 获取下拉表格数据
 * @param params 查询参数
 * @returns 下拉表格数据
 */
export const getComboGridData = (params: Record<string, any>) => {
  return http.post<any>("/api/Common/GetComboGridData", params);
};

/**
 * 获取LOV数据
 * @param code LOV代码
 * @returns LOV数据
 */
export const getLovData = (code: string) => {
  return http.get<any>(`/api/SmLov/QueryByCode/${code}`);
};

// ==================== 数据操作相关 API ====================

/**
 * 添加记录
 * @param params 包含url和数据的参数对象
 * @returns 操作结果
 */
export const add = (params: Record<string, any>) => {
  return http.post<any>(params.url, params);
};

/**
 * 更新记录
 * @param params 包含url、Id和数据的参数对象
 * @returns 操作结果
 */
export const update = (params: any) => {
  return http.put<any>(`${params.url}/${params.Id}`, params);
};

/**
 * 删除单条记录
 * @param params 包含url和Id的参数对象
 * @returns 操作结果
 */
export const singleDelete = (params: IdParams) => {
  return http.delete<any>(`${params.url}/${params.Id}`);
};

/**
 * 批量删除记录
 * @param params 包含url和Ids的参数对象
 * @returns 操作结果
 */
export const batchDelete = (params: BatchParams) => {
  return http.delete<any>(params.url, params.Ids);
};

/**
 * 批量审核
 * @param params 包含url和Ids的参数对象
 * @returns 操作结果
 */
export const batchAudit = (params: BatchParams) => {
  return http.put<any>(`${params.url}/BulkAudit`, params.Ids);
};

/**
 * 批量撤销
 * @param params 包含url和Ids的参数对象
 * @returns 操作结果
 */
export const batchRevocation = (params: BatchParams) => {
  return http.put<any>(`${params.url}/BulkRevocation`, params.Ids);
};

// ==================== 其他功能 API ====================

/**
 * 导出Excel
 * @param moduleCode 模块代码
 * @param params 导出参数
 * @param filter 过滤条件
 * @returns 导出结果
 */
export const exportExcel = (moduleCode: string, params: Record<string, any>, filter: any) => {
  return http.get<any>(`/api/Common/ExportExcel/${moduleCode}`, params, { filter });
};

/**
 * 上传文件
 * @param url 上传地址
 * @param params 上传参数
 * @returns 上传结果
 */
export const uploadFile = (url: string, params: any) => {
  return http.postForm<any>(url, params);
};

/**
 * 清除缓存
 * @returns 操作结果
 */
export const clearCache = () => http.get<any>("/api/Common/ClearCache");
