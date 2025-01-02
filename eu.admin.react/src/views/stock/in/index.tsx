import React, { useState } from "react";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";

const Index: React.FC<any> = () => {
  let moduleCode = "IV_IN_MNG";
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");

  const changePage = (value: any, id: string, isView: any) => {
    if (value == "FormPage") {
      setViewType(value);
      setFormPageId(id);
      setFormPageIsView(isView);
    } else if (value == "FormIndex") {
      setViewType(value);
      setFormPageId("");
      setFormPageIsView("");
    }
  };
  const action = {};

  return (
    <>
      {viewType == "FormIndex" ? <TableList moduleCode={moduleCode} changePage={changePage} {...action} /> : null}
      {viewType == "FormPage" ? (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}
    </>
  );
};

export default Index;
