import React, { useState, useRef, useCallback } from "react";
import { TableList } from "./TableList";
import { FormPage } from "./FormPage";
import { ViewType, ActionType } from "@/typings";

/**
 * 定义组件props类型
 * @property {string} moduleCode - 模块代码
 * @property {React.ReactNode} extendAction - 扩展操作
 */
interface FormIndexProps {
  /**
   * 模块代码
   */
  moduleCode: string;
  /**
   * 扩展操作
   */
  extendAction?: React.ReactNode;
}

export const FormIndex: React.FC<FormIndexProps> = ({ moduleCode, extendAction }) => {
  const [viewType, setViewType] = useState(ViewType.INDEX);
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState(false);
  const tableRef = useRef<ActionType>();

  // 切换页面处理函数
  const handlePageChange = useCallback((page: ViewType, id: string = "", isView: boolean = false) => {
    setViewType(page);
    setFormPageId(id);
    setFormPageIsView(page === ViewType.PAGE && isView);
  }, []);

  // 重新加载列表
  const onReload = useCallback(() => {
    tableRef.current?.reload();
  }, []);

  return (
    <>
      <div style={{ display: viewType === ViewType.INDEX ? "block" : "none" }}>
        <TableList moduleCode={moduleCode} changePage={handlePageChange} extendAction={extendAction} tableActionRef={tableRef} />
      </div>
      {viewType === ViewType.PAGE && (
        <FormPage
          moduleCode={moduleCode}
          Id={formPageId}
          IsView={formPageIsView}
          changePage={handlePageChange}
          onReload={onReload}
        />
      )}
    </>
  );
};
