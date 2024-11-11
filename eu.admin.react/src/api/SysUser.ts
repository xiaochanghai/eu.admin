import { IFkItem, DbEntity, IUser } from "@/api/base";
import http from "@/api";
// 用户表
export interface SysUser extends DbEntity, IFkItem, IUser {
  thirdId: string; // 第三方id
  avatar: string; // 头像
  sysDeptId: string; // 部门
  source: string; // 用户来源
  idno: string; // 证件号
  loginNum: number; // 登录次数
  superUser: boolean; // 超级用户
  PassWord: string; // 密码
  UserName: string; // 姓名
  tel: string; // 电话
  IsActive: string; // 启用状态
  thirdToken: string; // 第三方的token
  usetype: string; // 用户类型
  sysGroupId: string; // 权限组
  email: string; // 邮箱
  age: number; // 年龄
  UserAccount: string; // 用户名
  ID: string; // 用户ID
}

/** 用户查询 */
export const list = async (): Promise<SysUser[]> => {
  let { Data } = await http.get<SysUser[]>(`/api/SmUser`);
  return Data;
};
