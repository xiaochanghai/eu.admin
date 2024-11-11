import { DbEntity } from "@/api/base";

import { AuditInfo, NodeUserInfo } from "@/workflow-editor/classes/vlife";
// import { FormDto } from "../Form";
export interface flow extends DbEntity {
  json: string; //流程脚本
  entityType: string; //实体类型
  flowDefineKey: string; //流程定义key
}
export interface FlowReq {
  defineKey: string; //流程定义key
  businessKeys: string[]; //业务keys
  businessKey: string; //业务key
  activeKey: string; //当前激活的页签key
}
export interface FlowDto {
  defineKey: string; //流程定义key
  businessKey: string; //业务key
  comment: string; //备注
  formData: any; //表单数据
  description: string; //描述
}
export interface RecordFlowInfo {
  nodeId: string; //节点id
  businessKey: string; //业务key
  started: boolean; //  是否已经开始
  ended: boolean; //是否已经结束
  nodeType: string; //当前节点类型
  processStage: string; //当前流程阶段
  currNodeFzr: string[]; //当前任务负责人
  auditInfo: AuditInfo; //审批节点信息
  currTask: boolean; // 是否用户的当前任务
  recallable: boolean; // 用户是否能够从当前节点撤回；
}

// 流转信息
export interface FlowNode {
  nodeName: string; // 节点名称
  nodeType: string; // 节点类型
  nodeStatus: string; // 节点状态 进行中/已完成/已拒绝
  endTime: Date; // 开始时间(上一步的完成时间)
  count: number; //通知数量
  auditTaskList: FlowTask[]; // 审核详情列表
  target: NodeUserInfo[]; // 节点目标处理人
  activityKey: string; // 节点key
}

//流程任务实例信息解析
export interface FlowTask {
  assignee: string; //任务执行人id
  assigneeName: string; //任务执行人姓名
  status: string; // 操作结果类型
  auditTime: Date; // 审核时间
  comment: string; // 审核说明
  description: string; //流转描述
}
