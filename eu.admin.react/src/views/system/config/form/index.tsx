import React from "react";
import FormDesign from "./components/FormDesign";

const Index: React.FC = () => {
  return (
    <>
      <FormDesign
        moduleCode="SD_SALES_ORDER_MNG"
        // onModelChange={formVo => {
        //   setCurrModel(m => {
        //     return (
        //       m && {
        //         ...m,
        //         modelSize: formVo.modelSize,
        //         fields: formVo.fields
        //       }
        //     );
        //   });
        // }}
      />
    </>
  );
};

export default Index;
