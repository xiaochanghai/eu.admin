import { IModel, PageVo, DbEntity, Result, PageQuery } from "@/api/base";
import http from "@/api";
// 流程部署
export interface FlowDeployment extends DbEntity {
  json: string;
}

//流程字段配置信息
export interface FlowField extends IModel {
  fieldName: string; //字段标识
  title: string; //字段名
  type: string; //字段类型
  access: string; //访问性(显隐读写)
}

// 审批节点信息
export interface AuditInfo extends IModel {
  auditList: NodeUserInfo[]; // 常规审批对象
  emptyPass: string; // 审批人为空时策略
  handleType: string; // 办理人员类型
  auditLevel: AuditLevel; // 逐级审批对象
  transfer: boolean; // 转办
  emptyUserId: string; // 空办理人
  addSign: boolean; // 加签
  recall: boolean; //撤回
  rollback: boolean; //回退
  rejected: boolean; //拒绝
  fields: FlowField[]; //流程字段配置
}

// 审核节点配置
export interface IApproverSettings extends AuditInfo {
  entityType: string; //审核实体类型
  joinType: string; //会签类型
  passExecuteEl: string; //审核通过后触发接口
}
// 逐层审批配置
export interface AuditLevel extends IModel {}

// 节点参与对象信息
export interface NodeUserInfo extends IModel {
  userType: string; // 参办对象类型
  label: string;
  objectId: string; //  办理对象id (选择部分节点时 是表达式)
  el: string; // 参与人或组的表达式
}
/** 分页查询*/
export const page = async (req: PageQuery): Promise<Result<PageVo<FlowDeployment>>> => {
  let { Data } = await http.get<Result<PageVo<FlowDeployment>>>(`/sysDict/remove`, req);
  return Data;
};
/** 列表查询*/
export const list = async (req: PageQuery): Promise<Result<FlowDeployment[]>> => {
  let { Data } = await http.get<Result<FlowDeployment[]>>(`/flowDeployment/list`, req);
  return Data;
};
/** 保存*/
export const save = async (flowDeployment: FlowDeployment): Promise<Result<FlowDeployment>> => {
  let { Data } = await http.get<Result<FlowDeployment>>(`/flowDeployment/save`, flowDeployment);

  return Data;
};
/** 启动流程*/
export const start = async (req: { processDefinitionKey: string; instanceName: string }): Promise<Result<boolean>> => {
  let { Data } = await http.get<Result<boolean>>(`/flowDeployment/start`, { params: req });

  return Data;
};
/** xml*/
export const xml = async (): Promise<Result<string>> => {
  let { Data } = await http.get<Result<string>>(`/flowDeployment/xml`);

  return Data;
};
/** 是包含候选任务的*/
export const myTasks = async (): Promise<Result<number>> => {
  let { Data } = await http.get<Result<number>>(`/flowDeployment/myTasks`);

  return Data;
};
/** 完成待办任务*/
export const completeTask = async (): Promise<Result<boolean>> => {
  let { Data } = await http.get<Result<boolean>>(`/flowDeployment"/completeTask"`);

  return Data;
};
/** 明细查询*/
export const detail = async (req: { id: string }): Promise<Result<FlowDeployment>> => {
  let { Data } = await http.get<Result<FlowDeployment>>(`/flowDeployment/detail/${req.id}`);

  return Data;
};
/** 逻辑删除*/
export const remove = async (ids: String[]): Promise<Result<number>> => {
  let { Data } = await http.get<Result<number>>(`/flowDeployment/remove`, { data: ids });

  return Data;
};
