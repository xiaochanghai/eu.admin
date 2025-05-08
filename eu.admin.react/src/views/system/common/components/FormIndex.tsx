import React, { useState } from "react";
import TableList from "./TableList";
import FormPage from "./FormPage";

// 定义页面类型枚举
enum ViewType {
  INDEX = "FormIndex",
  PAGE = "FormPage"
}

/**
 * 定义组件props类型
 * @property {string} moduleCode - 模块代码
 */
interface FormIndexProps {
  /**
   * 模块代码
   */
  moduleCode: string;
}

const FormIndex: React.FC<FormIndexProps> = ({ moduleCode }) => {
  const [viewType, setViewType] = useState(ViewType.INDEX);
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");

  //切换页面处理函数
  const handlePageChange = (type: ViewType, id: string = "", isView: any) => {
    setViewType(type);
    setFormPageId(id);
    if (type == ViewType.PAGE) {
      setFormPageIsView(isView);
    } else if (type == ViewType.INDEX) {
      setFormPageIsView("");
    }
  };
  return (
    <>
      {viewType === ViewType.INDEX && <TableList moduleCode={moduleCode} changePage={handlePageChange} />}
      {viewType === ViewType.PAGE && (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={handlePageChange} />
      )}
    </>
  );
};

export default FormIndex;
