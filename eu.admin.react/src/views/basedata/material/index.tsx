import React from "react";
import { TableList } from "@/components/";
import FormPage from "./FormPage";
const Index: React.FC<any> = () => {
  return <TableList moduleCode="BD_MATERIAL_MNG" DynamicFormPage={FormPage} />;
};

export default Index;
