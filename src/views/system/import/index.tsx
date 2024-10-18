import React, { useState } from "react";
import TableList from "../common/components/TableList";
import FormPage from "../common/components/FormPage";
import Attachment from "@/components/Attachment";

// import { useDispatch } from "@/redux";

const ImportTemplate: React.FC<any> = () => {
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

  let childrenItems: any[] = [];
  if (viewType == "FormPage") {
    childrenItems = [
      {
        key: 1,
        label: "模板明细",
        children: <TableList moduleCode="SM_IMPORT_TEMPLATE_DETAIL_MNG" masterId={formPageId} IsView={formPageIsView} />
      },
      {
        key: 2,
        label: "模板文件",
        children: <Attachment Id={formPageId} accept=".xlsx,.xls" filePath="ImportTemplate" isUnique={true} />
      }
    ];
  }
  const setFormPageId1 = (id: any) => {
    setFormPageId(id);
  };
  return (
    <>
      {viewType == "FormIndex" ? <TableList moduleCode="SM_IMPORT_TEMPLATE_MNG" changePage={changePage} /> : null}
      {viewType == "FormPage" ? (
        <FormPage
          moduleCode="SM_IMPORT_TEMPLATE_MNG"
          Id={formPageId}
          IsView={formPageIsView}
          changePage={changePage}
          setFormPageId={setFormPageId1}
          childrenItems={childrenItems}
        />
      ) : null}
    </>
  );
};

export default ImportTemplate;
