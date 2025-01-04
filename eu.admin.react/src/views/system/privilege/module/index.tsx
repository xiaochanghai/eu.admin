import React, { useState } from "react";
import TableList from "../../common/components/TableList";
import SqlEdit from "./SqlEdit";
import FormDesign from "../../config/form/components/FormDesign";
import FormPage from "./FormPage";

// import { useDispatch } from "@/redux";

const SystemModule: React.FC<any> = () => {
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");

  const changePage = (value: any, id: string, isView: any) => {
    if (value == "FormIndex") {
      setViewType(value);
      setFormPageId("");
      setFormPageIsView("");
    } else {
      setViewType(value);
      setFormPageId(id);
      setFormPageIsView(isView);
    }
  };

  return (
    <>
      {viewType == "FormIndex" ? (
        <TableList moduleCode="SM_MODULE_MNG" changePage={changePage} DynamicFormPage={FormPage} />
      ) : null}
      {viewType == "SqlEdit" ? (
        <SqlEdit moduleCode="SM_IMPORT_TEMPLATE_MNG" ModuleId={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}
      {viewType == "FormCollocate" ? (
        <FormDesign moduleCode="SD_SALES_ORDER_MNG" ModuleId={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}
    </>
  );
};

export default SystemModule;
