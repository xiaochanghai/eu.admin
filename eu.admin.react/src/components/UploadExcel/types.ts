/**
 * UploadExcel 组件相关类型定义
 */

/**
 * 导入模板信息接口
 */
export interface ImportTemplateInfo {
  /** 模板代码 */
  TemplateCode: string;
  /** 文件ID */
  FileId?: string;
  /** 模板名称 */
  TemplateName: string;
  /** 是否允许覆盖导入 */
  IsAllowOverride: boolean;
}

/**
 * 错误信息接口
 */
export interface ErrorItem {
  /** 序号 */
  Key: number;
  /** Sheet名称 */
  SheetName: string;
  /** 错误信息 */
  ErrorName: string;
}

/**
 * 导入列定义接口
 */
export interface ImportColumn {
  /** 列标题 */
  title: string;
  /** 数据索引 */
  dataIndex: string;
  /** 列键值 */
  key: string;
  /** 列宽度 */
  width?: number;
  /** 是否省略显示 */
  ellipsis?: boolean;
  /** 是否固定列 */
  fixed?: "left" | "right" | boolean;
}

/**
 * 模块信息接口
 */
export interface ModuleInfo {
  /** 主记录ID */
  masterId?: string;
  /** 模块代码 */
  moduleCode: string;
  /** 模块ID */
  moduleId: string;
  /** 模块名称 */
  moduleName: string;
}

/**
 * 上传响应数据接口
 */
export interface UploadResponseData {
  /** 导入数据列表 */
  ImportList: Record<string, any>[];
  /** 导入列名 */
  ImportColumns: string[];
  /** 导入列显示名 */
  ImportColumnNames: string[];
  /** 导入数据ID */
  ImportDataId: string;
  /** 错误列表 */
  errorList?: Omit<ErrorItem, "Key">[];
}

/**
 * Chat查询响应数据接口
 */
export interface ChatQueryResponseData {
  /** 导入数据列表 */
  ImportList: Record<string, any>[];
  ImportMasterList: Record<string, any>[];
  /** 导入列名 */
  ImportColumns: string[];
  /** 导入列显示名 */
  ImportColumnNames: string[];
  /** 模板信息 */
  Template: ImportTemplateInfo;
  /** 模块代码 */
  moduleCode: string;
  ErrorList: ErrorItem[];
}

/**
 * UploadExcel组件属性接口
 */
export interface UploadExcelProps {
  /** 模块信息 */
  moduleInfo: ModuleInfo;
  /** 数据重新加载回调 */
  onReload: () => void;
  /** 取消操作回调 */
  onCancel: () => void;
}

/**
 * ChatUploadExcel组件属性接口
 */
export interface ChatUploadExcelProps {
  /** 导入数据ID */
  ImportDataId: string;
  /** 模板ID */
  TemplateId: string;
}

/**
 * 上传步骤类型
 */
export type UploadStepType = 0 | 1 | 2;

/**
 * Chat步骤类型
 */
export type ChatStepType = 0 | 1;

/**
 * 导入类型
 */
export type ImportType = "append" | "override";

/**
 * 文件上传状态
 */
export type UploadStatus = "idle" | "uploading" | "success" | "error";

/**
 * 组件状态接口
 */
export interface ComponentState {
  /** 当前步骤 */
  stepsCurrent: number;
  /** 页面加载状态 */
  pageLoading: boolean;
  /** 上传状态 */
  uploading: boolean;
  /** 错误列表 */
  errorList: ErrorItem[];
  /** 导入列配置 */
  importColumns: ImportColumn[];
  /** 导入数据列表 */
  importList: Record<string, any>[];
  /** 导入数据ID */
  importDataId: string | null;
  /** 导入模板信息 */
  importTemplateInfo: ImportTemplateInfo;
}
