import React from "react";
import TableList from "../../common/components/TableList";
import FormPage from "./FormPage";
const ImportTemplate: React.FC<any> = () => {
  return <TableList moduleCode="SM_ROLE_MNG" DynamicFormPage={FormPage} />;
};

export default ImportTemplate;
