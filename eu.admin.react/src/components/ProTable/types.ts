import { ModuleInfo, RecordLogData } from "@/api/interface/index";

/**
 * ProTable 主组件 Props
 */
export interface ProTableProps {
  moduleInfo: ModuleInfo;
  IsView?: boolean | null;
  onEdit?: (id: string | null, isView?: boolean) => void;
  masterId?: string | null;
  customConditions?: string; // 自定义SQL WHERE条件
  formRef?: React.RefObject<any>;
  expendHideAction?: (action: any, selectedRows: any[]) => React.ReactNode;
  expendAction?: (action: any, selectedRows: any[], selectedRowKeys: any[]) => React.ReactNode;
  delete?: (record: any) => void;
  batchDelete?: (ids: string[]) => void;
  deleteConfirm?: (action: any, record: any) => void;
  submitAudit?: (moduleId: string, selectedRows: any[]) => Promise<any>;
  [key: string]: any; // 支持自定义 action handlers
}

/**
 * 操作按钮 Props
 */
export interface ActionButtonProps {
  icon: string;
  onClick: () => void;
  disabled?: boolean;
  tooltip?: string;
}

/**
 * 工具栏 Props
 */
export interface ToolbarProps {
  moduleInfo: ModuleInfo;
  IsView?: boolean | null;
  action: any;
  selectedRows: any[];
  selectedRowKeys: any[];
  masterId?: string | null;
  onAdd: () => void;
  onUploadExcel: () => void;
  onExportExcel: () => void;
  onShowLog: () => void;
  onBatchDelete: () => void;
  onBatchAudit: () => void;
  onBatchRevocation: () => void;
  onSearchToggle: () => void;
  moreToolBarVisible: boolean;
  onMoreToolBarVisibleChange: (flag: boolean) => void;
  customMenuActions?: Record<string, (...args: any[]) => void>;
  expendHideAction?: (action: any, selectedRows: any[]) => React.ReactNode;
  expendAction?: (action: any, selectedRows: any[], selectedRowKeys: any[]) => React.ReactNode;
}

/**
 * 操作列 Props
 */
export interface ActionColumnProps {
  moduleInfo: ModuleInfo;
  IsView?: boolean | null;
  onEdit: (record: any) => void;
  onView: (record: any) => void;
  onDelete: (action: any, record: any) => void;
  customActions?: Record<string, (id: string, action: any, record: any) => void>;
}

/**
 * 批量操作参数
 */
export interface BatchOperationParams {
  action: any;
  selectedRows: any[];
  operationType: "delete" | "audit" | "revocation";
  confirmTitle: string;
  apiFunc: (params: any) => Promise<any>;
}

/**
 * 日志弹窗 Props
 */
export interface RecordLogModalProps {
  visible: boolean;
  data: RecordLogData | null;
  onCancel: () => void;
}

/**
 * Excel导入弹窗 Props
 */
export interface UploadExcelModalProps {
  visible: boolean;
  moduleInfo: ModuleInfo;
  onCancel: () => void;
  onReload: () => void;
}
