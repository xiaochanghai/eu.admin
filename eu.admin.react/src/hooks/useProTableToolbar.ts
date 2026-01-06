import { useState } from "react";
import { message } from "@/hooks/useMessage";
import { exportExcel, getModuleLogInfo } from "@/api/modules/module";
import { RecordLogData } from "@/api/interface/index";
import { downloadFile } from "@/utils";
import { LOADING_MESSAGES } from "@/components/ProTable/constants";

/**
 * ProTable 工具栏管理 Hook
 * 负责工具栏状态、弹窗管理和导出等功能
 *
 * @param moduleCode 模块代码
 * @param tableParam 表格参数（用于导出）
 * @returns 工具栏相关的状态和处理函数
 */
export const useProTableToolbar = (moduleCode: string, tableParam?: any) => {
  const [moreToolBarVisible, setMoreToolBarVisible] = useState(false);
  const [uploadExcelVisible, setUploadExcelVisible] = useState(false);
  const [recordLogVisible, setRecordLogVisible] = useState(false);
  const [recordLogData, setRecordLogData] = useState<RecordLogData | null>(null);

  /**
   * 导出 Excel
   */
  const handleExportExcel = async () => {
    setMoreToolBarVisible(false);
    message.success(LOADING_MESSAGES.EXPORTING);
    const filter = tableParam?.filter || {};
    const { Success, Data } = await exportExcel(moduleCode, {}, filter);
    if (Success) {
      downloadFile(Data, Data);
    }
  };

  /**
   * 显示日志记录
   */
  const handleShowLog = async (selectedRows: any[]) => {
    setRecordLogVisible(true);
    const { Data } = await getModuleLogInfo(moduleCode, selectedRows[0].ID);
    setRecordLogData(Data);
  };

  /**
   * 关闭日志弹窗
   */
  const handleLogClose = () => {
    setRecordLogVisible(false);
    setRecordLogData(null);
  };

  /**
   * 更多工具栏可见性变更
   */
  const handleMoreToolBarVisibleChange = (flag: boolean) => {
    setMoreToolBarVisible(flag);
  };

  /**
   * 打开 Excel 导入弹窗
   */
  const handleUploadExcelOpen = () => {
    setUploadExcelVisible(true);
  };

  /**
   * 关闭 Excel 导入弹窗
   */
  const handleUploadExcelClose = () => {
    setUploadExcelVisible(false);
  };

  return {
    moreToolBarVisible,
    uploadExcelVisible,
    recordLogVisible,
    recordLogData,
    handleMoreToolBarVisibleChange,
    handleUploadExcelOpen,
    handleUploadExcelClose,
    handleShowLog,
    handleLogClose,
    handleExportExcel
  };
};
