import React, { useState } from "react";
import TableList from "../../common/components/TableList";
import SqlEdit from "./SqlEdit";

// import { useDispatch } from "@/redux";

const SystemModule: React.FC<any> = () => {
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");

  const changePage = (value: any, id: string, isView: any) => {
    if (value == "SqlEdit") {
      setViewType(value);
      setFormPageId(id);
      setFormPageIsView(isView);
    } else if (value == "FormIndex") {
      setViewType(value);
      setFormPageId("");
      setFormPageIsView("");
    }
  };

  return (
    <>
      {viewType == "FormIndex" ? <TableList moduleCode="SM_MODULE_MNG" changePage={changePage} /> : null}
      {viewType == "SqlEdit" ? (
        <SqlEdit moduleCode="SM_IMPORT_TEMPLATE_MNG" ModuleId={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}
    </>
  );
};

export default SystemModule;
