import http from "@/api";
import { ModuleInfo, RecordLogData } from "@/api/interface/index";

export const getModuleInfo = (moduleCode: string) => {
  return http.get<ModuleInfo>(`/api/SmModule/GetModuleInfo/` + moduleCode);
};
export const getModuleLogInfo = (params: any) => {
  return http.get<RecordLogData>(`/api/SmModule/GetModuleLogInfo`, params);
};
export const query = (params: any) => {
  return http.getGridList(`/api/Common/GetGridList`, params);
};
export const querySingle = (params: any) => {
  return http.get<any>(params.url + "/" + params.Id);
};
export const add = (params: any) => {
  return http.post<any>(params.url, params);
};
export const update = (params: any) => {
  return http.put<any>(params.url + "/" + params.Id, params);
};
export const singleDelete = (params: any) => {
  return http.delete<any>(params.url + "/" + params.Id);
};
export const batchDelete = (params: any) => {
  return http.delete<any>(params.url, params.Ids);
};
export const batchAudit = (params: any) => {
  return http.put<any>(params.url + "/BulkAudit", params.Ids);
};
export const batchRevocation = (params: any) => {
  return http.put<any>(params.url + "/BulkRevocation", params.Ids);
};
export const clearCache = () => {
  return http.get<any>("/api/Common/ClearCache");
};
export const getLovData = (code: any) => {
  return http.get<any>("/api/SmLov/QueryByCode/" + code);
};
export const getComboGridData = (params: any) => {
  return http.post<any>("/api/Common/GetComboGridData", params);
};

export const uploadFile = (url: string, params: any) => {
  return http.postForm<any>(url, params);
};

export const recordUserModuleColumn = (params: any) => {
  return http.post<any>("/api/SmModule/RecordUserModuleColumn/" + params.moduleId, params.map);
};

export const exportModuleSqlScript = (params: any) => {
  return http.post<any>("/api/SmModule/ExportSqlScript", params);
};
export const getModuleFullSql = (id: string) => {
  return http.post<any>("/api/SmModuleSql/GetModuleFullSql/" + id);
};
export const getModuleSqlInfo = (id: string) => {
  return http.get<any>("/api/SmModuleSql/ByModuleId/" + id);
};
