import React, { useState } from "react";
import TableList from "./TableList";
import FormPage from "./FormPage";
import { ViewType } from "@/typings";

/**
 * 定义组件props类型
 * @property {string} moduleCode - 模块代码
 */
interface FormIndexProps {
  /**
   * 模块代码
   */
  moduleCode: string;
  extendAction?: any;
}

const FormIndex: React.FC<FormIndexProps> = ({ moduleCode, extendAction }) => {
  const [viewType, setViewType] = useState(ViewType.INDEX);
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");

  //切换页面处理函数
  const handlePageChange = (page: ViewType, id: string = "", isView: any) => {
    setViewType(page);
    setFormPageId(id);
    if (page == ViewType.PAGE) {
      setFormPageIsView(isView);
    } else if (page == ViewType.INDEX) {
      setFormPageIsView("");
    }
  };
  return (
    <>
      {viewType === ViewType.INDEX && (
        <TableList moduleCode={moduleCode} changePage={handlePageChange} extendAction={extendAction} />
      )}
      {viewType === ViewType.PAGE && (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={handlePageChange} />
      )}
    </>
  );
};

export default FormIndex;
