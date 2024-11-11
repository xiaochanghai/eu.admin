import { DbEntity, ItemType, SaveBean, VoBean } from "@/api/base";
import { FormFieldDto, FormFieldVo } from "@/api/FormField";

/**
 * DB的模型信息
 */
export interface Form extends DbEntity {
  title: string; //模型名称
  type: string; //标识
  itemType: ItemType; // 类型
  entityType: string;
  name: string; //前端命名的名称
  sort: number; //排序号s
  icon: string; //图标
  modelSize: number; //模块大小 12网格里的占比大小
  pageSize: number; //分页大小
  version: number; //模型版本
  listApiPath: string; //列表请求ts方法地址
  saveApiPath: string; //数据保存ts方法地址
  prefixNo: string; //编号前缀
  sysMenuId: string; //所属应用
  formDesc: string; //描述
  itemName: string; //主字段表达式
  helpDoc: string; //开发帮助文档
  typeParentsStr: string; //模型类接口
  flowJson: string; //已发布流程脚本
  unpublishJson: string; //未发布的流程脚本
  unpublishForm: string;
  orders: string;
  custom: boolean; //用户定义模型
  state: string; //可用状态 1可用，0不可用
  supportFilter: boolean; //支持过滤
  supportNo: boolean; //支持序号
  // flowDefineKey:string;//流程定义key
}

/**
 * 保存的模型结构
 */
export interface FormDto extends SaveBean, Form {
  fields: FormFieldDto[]; //字段信息
  // formTabDtos?: FormTabDto[]; //页签信息
  // resources?: SysResources[]; //关联接口
}

/**
 * 视图展示层的VO模型，包涵
 * 1. java模型 modelInfo
 * 2. 关联表数据 FormDto
 */
export interface FormVo extends VoBean, Omit<FormDto, "fields"> {
  // rules:FormRuleDto[]// 业务规则
  rules: any[]; // 业务规则
  // parentsName:string[];//继承和实现的类的名称
  // parentForm: FormVo; //当前form模型作为子表它所在的父表信息
  fields: FormFieldVo[]; //字段信息
}
