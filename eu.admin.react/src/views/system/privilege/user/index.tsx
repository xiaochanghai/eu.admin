import React from "react";
import TableList from "../../common/components/TableList";
import FormPage from "./FormPage";

const Index: React.FC<any> = () => {
  return <TableList moduleCode="SM_USER_MNG" DynamicFormPage={FormPage} />;
};

export default React.memo(Index);
