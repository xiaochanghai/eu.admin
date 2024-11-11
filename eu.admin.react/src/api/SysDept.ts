import http from "@/api";

/** 所有部门*/
export const list = async (): Promise<any> => {
  let { Data } = await http.get<any>(`/api/SmDepartment`);
  return Data;
};
