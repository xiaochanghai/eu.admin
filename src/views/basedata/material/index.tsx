import React from "react";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";
const ImportTemplate: React.FC<any> = () => {
  return <TableList moduleCode="BD_MATERIAL_MNG" DynamicFormPage={FormPage} />;
};

export default ImportTemplate;
