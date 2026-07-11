// Request response parameters (excluding data)
import type { ProColumns } from "@ant-design/pro-components";
export interface Result {
  Status: number;
  Message: string;
}

// Request response parameters (including data)
export interface ResultData<T = any> extends Result {
  Data: T;
  Success: boolean;
}

export interface ResultPage {
  current: number;
  data: [];
  message: string;
  pageSize: number;
  total: number;
  pageCount: number;
  status: number;
  success: boolean;
}
// paging request parameters
export interface ReqPage {
  current?: number;
  pageSize?: number;
}

// paging response parameters
export interface ResPage<T> {
  list: T[];
  current: number;
  pageSize: number;
  total: number;
}

export interface ReqLogin {
  username: string;
  password: string;
}

export interface ResLogin {
  Token: string;
  UserId: string;
  UserInfo: { UserName: ""; UserId: ""; AvatarFileId: ""; UserType: "" };
}

export interface UserList {
  id: string;
  username: string;
  gender: 1 | 2;
  age: number;
  idCard: string;
  email: string;
  address: string;
  createTime: string;
  status: boolean;
  avatar: string;
}
export interface ModuleInfo {
  IsShowAudit: boolean;
  Success: boolean;
  UserModuleColumn: {};
  actionCount: number;
  actionData: any[];
  actions: string[];
  beforeActions: ModuleInfoBeforeAction[];
  children: any[];
  columns: ProColumns[];
  dropActions: any[];
  formColumns: any[];
  formPage: string;
  formPageWidth: number;
  hideMenu: any[];
  isDetail: false;
  masterColumn: string;
  menuData: any[];
  moduleId: string;
  moduleName: string;
  moduleName_EN: string;
  moduleCode: string;
  moduleType: string;
  openType: string;
  url: string;
  customActionData: any[];
  IsShowRowSelection?: boolean;
}
export interface ModuleInfo1 {
  ModuleName: string;
  ModuleCode: string;
  IsWorkflow: boolean;
}
export interface ModuleInfoBeforeAction {
  id: string;
  taxisNo: number;
}

/**
 * 模块日志数据接口
 */
export interface RecordLogData {
  /** 表名称 */
  TableName: string;
  /** 创建人 */
  CreatedBy: string;
  /** 记录ID */
  ID: string;
  /** 更新人 */
  UpdateBy: string;
  /** 创建时间 */
  CreatedTime: string;
  /** 更新时间 */
  UpdateTime: string;
  /** 模块代码 */
  ModuleCode: string;
}
