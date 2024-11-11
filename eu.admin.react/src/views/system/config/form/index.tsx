import React from "react";
// import FormDesign from "./components/FormDesign";
// import { FormVo } from "@/api/Form";
import { ItemType } from "@/api/base";
import FlowDesign from "./FlowDesign";

const Index: React.FC = () => {
  let currModel = {
    // parentForm: 1,
    sort: 0,
    // createId: "",
    // createDate: 2024/01/8,
    rules: [],
    id: "4028b8818cec22b7018cec22cdc1004b",
    title: "产品",
    type: "product",
    typeParentsStr: "Item,IStatus,IdBean,Serializable,IModel,DbEntity,Object",
    entityType: "product",
    itemType: ItemType.entity,
    name: "产品",
    icon: "",
    modelSize: 2,
    pageSize: 10,
    version: 0,
    listApiPath: "",
    saveApiPath: "",
    prefixNo: "",
    resources: [],
    itemName: "",
    sysMenuId: "4028b8818cec22b7018cec23b70600f4",
    formDesc: "",
    helpDoc: "",
    // labelField: "",
    fields: [],
    formTabDtos: [],
    flowJson: "",
    unpublishJson: "{}",
    unpublishForm: "",
    custom: false,
    state: "1",
    orders: "",
    supportFilter: false,
    supportNo: false
  };

  return (
    <>
      {/* <FormDesign
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
      /> */}

      <FlowDesign
        type={""}
        formVo={currModel}
        onDataChange={function (): void {
          // setCurrModel(m => {
          //   //只更新未发布的
          //   return m && { ...m, unpublishJson: flowJson };
          // });
        }}
      />
    </>
  );
};

export default Index;
