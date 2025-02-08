import React, { useEffect, useImperativeHandle, useState } from "react";
import { Flex, Form, TreeSelect, Tabs } from "antd";
import { message } from "@/hooks/useMessage";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import http from "@/api";
import { Layout, UploadImage, Attachment, Loading } from "@/components";
const FormItem = Form.Item;

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState(null);
  const [treeData, setTreeData] = useState<any>([]);
  const [modifyType, setModifyType] = useState(ModifyType.Add);

  const [form] = Form.useForm();

  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const {
    Id,
    moduleCode,
    formPageRef,
    // onReload,
    IsView,
    onDisabled,
    masterId,
    onReload
  } = props;
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;

  let { formColumns, openType, url, isDetail, masterColumn } = moduleInfo;

  // let Id = !openType ? props.Id : ids[moduleCode];

  useEffect(() => {
    if (dispatch && Id) {
      setModifyType(ModifyType.Edit);
      setViewId(Id);
      const querySingleData = async () => {
        let { Data, Success } = await querySingle({ Id, moduleCode, url });
        if (Success) {
          dispatch(setId({ moduleCode, id: Id }));
          form.setFieldsValue(Data);
          getAllMaterialType(Data.MaterialTypeId);
        }
      };
      querySingleData();

      setIsLoading(false);

      setDisabled(false);
    } else {
      // getAllMaterialType();
      setDisabled(false);
      setIsLoading(false);
    }
  }, []);

  const getAllMaterialType = async (classId: any) => {
    let { Data, Success } = await http.get<any>("/api/MaterialType/QueryClass/" + classId);
    if (Success) setTreeData([Data]);
  };
  // const component = () => {
  //   return (
  //     <Flex wrap="wrap">
  //       {formColumns.filter((f: { HideInForm: boolean }) => f.HideInForm === false)?.length === 0 ? (
  //         <div className="main-tooltip">请选择进行系统表单配置</div>
  //       ) : (
  //         formColumns
  //           .filter((f: any) => f.HideInForm === false)
  //           .map((item: any, index: any) => {
  //             return (
  //               <div
  //                 style={{
  //                   width: (item.GridSpan != null ? item?.GridSpan : 50) + "%"
  //                 }}
  //                 key={index}
  //               >
  //                 <Layout field={item} IsView={IsView} disabled={disabled} />
  //               </div>
  //             );
  //           })
  //       )}
  //     </Flex>
  //   );
  // };
  const component = (group: any | 1) => {
    return (
      <Flex wrap="wrap">
        {formColumns.filter((f: { HideInForm: boolean; FromFieldGroup: any }) => f.HideInForm === false)?.length === 0
          ? null
          : formColumns
              .filter((f: any) => f.HideInForm === false && f.FromFieldGroup === group)
              .map((item: any, index: any) => {
                const width = (item.GridSpan != null ? item?.GridSpan : 50) + "%";
                if (item.DataIndex == "MaterialTypeId")
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <FormItem name="MaterialTypeId" label="物料分类" rules={[{ required: true }]}>
                        <TreeSelect
                          // value={this.state.MaterialTypeId}
                          dropdownStyle={{ maxHeight: 400, overflow: "auto" }}
                          treeData={treeData}
                          placeholder="请选择物料类型"
                          disabled={disabled ?? IsView}
                          treeDefaultExpandAll
                        />
                      </FormItem>
                    </div>
                  );
                else if (item.DataIndex == "MaterialClassId")
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <Layout
                        field={item}
                        IsView={disabled ?? IsView}
                        modifyType={modifyType}
                        onChange={(value: any) => {
                          form.setFieldsValue({
                            MaterialTypeId: null
                          });

                          getAllMaterialType(value);
                        }}
                      />
                    </div>
                  );
                else
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <Layout field={item} IsView={disabled ?? IsView} modifyType={modifyType} />
                    </div>
                  );
              })}
      </Flex>
    );
  };
  // const component = (group: any | 1) => {
  //   return (
  //     <Flex wrap="wrap">
  //       {formColumns.filter((f: { HideInForm: boolean; FromFieldGroup: any }) => f.HideInForm === false)?.length === 0
  //         ? null
  //         : formColumns
  //             .filter((f: any) => f.HideInForm === false && f.FromFieldGroup === group)
  //             .map((item: any, index: any) => {
  //               return (
  //                 <div
  //                   style={{
  //                     width: (item.GridSpan != null ? item?.GridSpan : 50) + "%"
  //                   }}
  //                   key={index}
  //                 >
  //                   {item.DataIndex == "MaterialTypeId" ? (
  //                     <FormItem name="MaterialTypeId" label="物料类型" rules={[{ required: true }]}>
  //                       <TreeSelect
  //                         // value={this.state.MaterialTypeId}
  //                         dropdownStyle={{ maxHeight: 400, overflow: "auto" }}
  //                         treeData={treeData}
  //                         placeholder="请选择物料类型"
  //                         disabled={disabled ?? IsView}
  //                         treeDefaultExpandAll
  //                       />
  //                     </FormItem>
  //                   ) : (
  //                     <Layout field={item} IsView={disabled ?? IsView} modifyType={modifyType} />
  //                   )}
  //                 </div>
  //               );
  //             })}
  //     </Flex>
  //   );
  // };
  const onFinish = async (data: any, type = "Save") => {
    if (id) data = { ...data, url, Id: id ?? null };
    else data = { ...data, url };
    if (isDetail) data[masterColumn] = masterId;
    if (moduleCode != "SM_MODULE_MNG") data["ModuleCode"] = moduleCode;

    for (let key in data) data[key] = data[key] ?? null;
    let { Data, Success, Message } = id ? await update(data) : await add(data);

    if (Success) {
      message.success(Message);
      if (onDisabled) onDisabled(true);
      if (openType === "Modal" || openType === "Drawer") onReload();
      if (type === "SaveAdd") {
        setViewId(null);
        setDisabled(true);
        form.resetFields();
      } else if (!id) setViewId(Data);
    }
  };
  const onSave = () => form.validateFields().then(onFinish);
  const onSaveAdd = () => form.validateFields().then(values => onFinish(values, "SaveAdd"));
  const onValuesChange = () => {
    if (onDisabled) onDisabled(false);
    setDisabled(false);
  };

  // const onModuleCheck = async (keys: any) => {
  //   setCheckedModuleKeys(keys);
  //   let param = { roleList: keys, UserId: Id };
  //   await http.post<any>("/api/SmUserRole/BatchInsertUserRole", param);
  // };
  useImperativeHandle(formPageRef, function () {
    return { onSave, onSaveAdd };
  });

  let items: any[] = [
    {
      key: 1,
      label: "基本信息",
      children: component(2)
    },
    {
      key: 2,
      label: "管控设置",
      children: component(3)
    },
    {
      key: 3,
      label: "产品图片",
      children: <UploadImage Id={id} isUnique={true} filePath="product" masterTable="BdMaterial" masterColumn="ImageUrl" />
    },
    {
      key: 4,
      label: "附件",
      children: <Attachment Id={id} />
    }
  ];
  return (
    <>
      {openType === "Modal" || openType === "Drawer" ? (
        <div style={{ marginTop: 20, marginBottom: 20 }}>
          <Form
            labelCol={{ span: 6, xl: 6, md: 8, sm: 8 }}
            labelWrap
            wrapperCol={{ span: 16 }}
            onFinish={onFinish}
            onValuesChange={onValuesChange}
            form={form}
          >
            {isLoading ? <Loading /> : component(1)}
            <div style={{ height: 20 }}></div>
            <Tabs items={items} />
          </Form>
        </div>
      ) : null}
      {/* Tabs Begin */}

      {/* Tabs Begin */}
    </>
  );
};

export default FormPage;
