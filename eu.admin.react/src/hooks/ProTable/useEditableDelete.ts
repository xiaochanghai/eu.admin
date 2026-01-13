import { useCallback } from "react";
import http from "@/api";
import { message } from "@/hooks/useMessage";

/**
 * 可编辑表格删除操作 Hook
 *
 * @param url API 基础 URL
 * @param tableRef 表格引用
 * @returns handleDelete 删除处理函数
 */
export const useEditableDelete = (url: string, tableRef?: React.RefObject<any>) => {
  /**
   * 删除记录处理函数
   */
  const handleDelete = useCallback(
    async (record: any) => {
      const { Success, Message } = await http.delete<any>(`${url}/${record.ID}`);
      if (Success) {
        message.success(Message);
        if (tableRef?.current) tableRef.current.reload();
      }
    },
    [url, tableRef]
  );

  return { handleDelete };
};
