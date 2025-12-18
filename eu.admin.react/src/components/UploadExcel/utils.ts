/**
 * UploadExcel 组件工具函数
 */

import { message } from "@/hooks/useMessage";
import { EXCEL_MIME_TYPES, MAX_FILE_SIZE } from "./constants";
import { ErrorItem, ImportColumn } from "./types";

/**
 * 验证Excel文件
 * @param file 文件对象
 * @returns 是否为有效的Excel文件
 */
export const validateExcelFile = (file: File): boolean => {
  const isValidType = EXCEL_MIME_TYPES.includes(file.type as any);
  const isValidSize = file.size <= MAX_FILE_SIZE;

  if (!isValidType) {
    message.error("请选择正确的Excel文件(.xlsx或.xls)!");
    return false;
  }

  if (!isValidSize) {
    message.error(`文件大小不能超过${MAX_FILE_SIZE / 1024 / 1024}MB!`);
    return false;
  }

  return true;
};

/**
 * 处理导入数据
 * @param importList 导入数据列表
 * @param importColumns 导入列名
 * @param importColumnNames 导入列显示名
 * @returns 处理后的数据和列配置
 */
export const processImportData = (importList: Record<string, any>[], importColumns: string[], importColumnNames: string[]) => {
  const processedList = importList.map((item, index) => ({
    ...item,
    Key: index + 1
  }));

  const columns: ImportColumn[] = [
    {
      title: "序号",
      dataIndex: "Key",
      key: "Key",
      width: 80,
      fixed: "left"
    },
    ...importColumns.map((col, j) => ({
      title: importColumnNames[j] || col,
      dataIndex: col,
      key: `Key_${j}`,
      ellipsis: true,
      width: 120
    }))
  ];

  return { processedList, columns };
};

/**
 * 处理错误数据
 * @param errorList 错误列表
 * @returns 处理后的错误数据
 */
export const processErrorData = (errorList: Omit<ErrorItem, "Key">[]): ErrorItem[] => {
  return errorList.map((item, index) => ({
    ...item,
    Key: index + 1
  }));
};
