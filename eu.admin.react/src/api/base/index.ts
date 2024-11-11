import { ConditionGroup, Conditions } from "@/dsl/base";

/**
 * 与后台相关对应的基类
 *
 */

// 数据传输dto
export interface IdBean {
  id: string;
}

/**
 * 支持字典和外键name形成选择项
 */
export interface IFkItem extends IdBean {
  name: string;
}

export interface ITree extends IdBean {
  code: string;
  pcode: string;
  name: string;
}

export interface INo extends IdBean {
  no: string;
}

export interface IUser {
  password: string;
  username: string;
}

//export interface IModel<T extends IdBean>{}
/**
 * 所有模型bean基类，空实现
 */
export interface IModel {}

//数据逻辑状态
export interface IStatus {
  status?: string;
}
//数据库实体
export interface DbEntity extends IdBean, IStatus {
  // createId: string; //创建人
  // modifyId?: string; //修订人
  // createDate: Date; //创建日期
  // modifyDate?: Date; // 修订日期
}
// 后端返回数据封装
export interface Result<D> {
  code: string;
  msg: string;
  data: D | undefined;
}
/**
 * 分页查询条件
 */
export interface PageQuery extends CustomQuery {
  pager?: {
    page: number;
    size: number;
  };
}
/**
 * 一般分页
 */
export interface CustomQuery {
  conditions?: Partial<Conditions>;
  conditionGroups?: ConditionGroup[];
}

/**
 *查询分页结果
 */
export interface PageVo<T extends IdBean> {
  page: number;
  size: number;
  total: number;
  totalPage: number;
  first: boolean;
  lasts: boolean;
  result: T[];
}
export interface VoBean extends IdBean {}
export interface SaveBean extends IdBean {}

export enum ItemType {
  entity = "entity",
  save = "save",
  req = "req",
  vo = "vo",
  bean = "bean",
  basic = "basic",
  list = "list"
}
