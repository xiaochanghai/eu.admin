import React, { useEffect, useImperativeHandle, useState } from "react";
import { Tree, Skeleton, Card, Form, Tabs, message } from "antd";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import { useDispatch, RootState, useSelector } from "@/redux";
import http from "@/api";
import { Loading, renderFormComponent } from "@/components";
import { ModuleInfo } from "@/api/interface";
import { SaveTypeEnum, EditOpenType, ModifyType } from "@/typings";
import { STANDARD_FORM_LAYOUT } from "@/config";

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState(null);
  const [treeData, setTreeData] = useState<any>([]);
  const [checkedModuleKeys, setCheckedModuleKeys] = useState<any>([]);
  const [modifyType] = useState(ModifyType.Add);

  const [form] = Form.useForm();

  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const {
    Id,
    moduleCode,
    formPageRef,
    // onReload,

    onDisabled,
    masterId,
    onReload
  } = props;
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;

  let { formColumns, openType, url, isDetail, masterColumn } = moduleInfo;

  // let Id = !openType ? props.Id : ids[moduleCode];

  useEffect(() => {
    if (dispatch && Id) {
      setViewId(Id);

      const querySingleData = async () => {
        let { Data, Success } = await querySingle({ Id, moduleCode, url });
        if (Success) {
          dispatch(setId({ moduleCode, id: Id }));
          form.setFieldsValue(Data);
        }
      };
      querySingleData();
      const getRoleModule = async () => {
        let { Data, Success } = await http.get<any>(`/api/SmUserRole/QueryUserRole/${Id}`);
        if (Success) {
          let keys: any[] = [];
          Data.map(
            (item: any) => keys.push(item.SmRoleId) //item表示数组中的每一个元素
          );
          setCheckedModuleKeys(keys);
        }
      };
      getRoleModule();

      const getAllModuleList = async () => {
        let { Data, Success } = await http.get<any>("/api/SmUserRole/QueryRole");
        if (Success) setTreeData([Data]);
      };
      getAllModuleList();

      setIsLoading(false);

      setDisabled(false);
    } else setIsLoading(false);
  }, []);

  const onFinish = async (data: any, type = SaveTypeEnum.Save) => {
    if (id) data = { ...data, url, Id: id ?? null };
    else data = { ...data, url };
    if (isDetail) data[masterColumn] = masterId;
    if (moduleCode != "SM_MODULE_MNG") data["ModuleCode"] = moduleCode;

    for (let key in data) data[key] = data[key] ?? null;
    let { Data, Success, Message } = id ? await update(data) : await add(data);

    if (Success) {
      message.success(Message);
      onDisabled?.(true);
      if (openType === EditOpenType.Modal || openType === EditOpenType.Drawer) onReload();
      if (type === SaveTypeEnum.SaveAdd) {
        setViewId(null);
        setDisabled(true);
        form.resetFields();
      } else if (!id) setViewId(Data);
    }
  };
  const onSave = () => form.validateFields().then(onFinish);
  const onSaveAdd = () => form.validateFields().then(values => onFinish(values, SaveTypeEnum.SaveAdd));
  const onValuesChange = () => {
    onDisabled?.(false);
    setDisabled(false);
  };

  const onModuleCheck = async (keys: any) => {
    setCheckedModuleKeys(keys);
    let param = { roleList: keys, UserId: Id };
    await http.post<any>("/api/SmUserRole/BatchInsertUserRole", param);
  };
  useImperativeHandle(formPageRef, function () {
    return { onSave, onSaveAdd };
  });

  let items: any[] = [
    {
      key: 1,
      label: "功能角色",
      children: (
        <Tree
          defaultExpandedKeys={["All"]}
          // defaultExpandParent={true}
          checkedKeys={checkedModuleKeys}
          onCheck={onModuleCheck}
          checkable
          treeData={treeData}
        />
      )
    }
  ];
  items.push();
  items.push();
  return (
    <>
      {openType === EditOpenType.Modal || openType === EditOpenType.Drawer ? (
        <Form {...STANDARD_FORM_LAYOUT} labelWrap onFinish={onFinish} onValuesChange={onValuesChange} form={form}>
          {isLoading ? <Loading /> : renderFormComponent(formColumns, disabled, modifyType)}
        </Form>
      ) : null}
      {/* Tabs Begin */}
      <div style={{ height: 20 }}></div>
      <Card>
        {treeData.length == 0 ? (
          <>
            <Skeleton active />
            <Skeleton active />
            <Skeleton active />
          </>
        ) : (
          <Tabs items={items} />
        )}
      </Card>

      {/* Tabs Begin */}
    </>
  );
};

export default FormPage;
