import React, { useEffect, useImperativeHandle, useState } from "react";
import { useDispatch } from "@/redux";
import { Flex, Tree, Skeleton, Card, Form, Space, Tabs, Checkbox, Collapse, message } from "antd";
import { Loading } from "@/components/Loading/index";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import Layout from "@/components/Elements/Layout";
import { RootState, useSelector } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import http from "@/api";

const { Panel } = Collapse;

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState(null);
  const [panelTitle, setPanelTitle] = useState(null);
  const [treeData, setTreeData] = useState<any>([]);
  const [functionData, setFunctionData] = useState<any>([]);
  const [checkFunction, setCheckFunction] = useState<any>([]);
  // const [treeFuncPrivData, setTreeFuncPrivData] = useState<any>([]);
  const [checkedModuleKeys, setCheckedModuleKeys] = useState<any>([]);
  const [selectModuleId, setSelectModuleId] = useState(["All"]);
  // const [checkedFuncPrivKeys, setCheckedFuncPrivKeys] = useState([]);

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
        let { Data, Success } = await http.get<any>("/api/SmRoleModule/GetRoleModule/" + Id);
        if (Success) setCheckedModuleKeys(Data);
      };
      getRoleModule();

      // const getRoleFuncPriv = async () => {
      //   let { Data, Success } = await http.get<any>("/api/SmRoleFunction/GetRoleFuncPriv/" + Id);
      //   if (Success) setCheckedFuncPrivKeys(Data);
      // };
      // getRoleFuncPriv();

      const getAllModuleList = async () => {
        let { Data, Success } = await http.get<any>("/api/SmRoleModule/GetAllModuleList");
        if (Success) setTreeData([Data]);
      };
      getAllModuleList();

      // const getAllFuncPriv = async () => {
      //   let { Data, Success } = await http.get<any>("/api/SmRoleFunction/GetAllFuncPriv");
      //   if (Success) setTreeFuncPrivData([Data]);
      // };
      // getAllFuncPriv();

      setIsLoading(false);

      setDisabled(false);
    } else setIsLoading(false);
  }, []);

  const component = () => {
    return (
      <Flex wrap="wrap">
        {formColumns.filter((f: { HideInForm: boolean }) => f.HideInForm === false)?.length === 0 ? (
          <div className="main-tooltip">请选择进行系统表单配置</div>
        ) : (
          formColumns
            .filter((f: any) => f.HideInForm === false)
            .map((item: any, index: any) => {
              return (
                <div
                  style={{
                    width: (item.GridSpan != null ? item?.GridSpan : 50) + "%"
                  }}
                  key={index}
                >
                  <Layout field={item} IsView={IsView} disabled={disabled} />
                </div>
              );
            })
        )}
      </Flex>
    );
  };
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

  const onModuleCheck = async (keys: any) => {
    setCheckedModuleKeys(keys);
    await http.post<any>("/api/SmRoleModule/UpdateRoleModule/" + Id, keys);
  };
  const getModuleFunction = async (param: any) => {
    let { Data, Success } = await http.get<any>("/api/SmRoleFunction/GetModuleFunction", param);
    if (Success) {
      let functionData1 = [];
      let checkFunction1 = [];
      if (Data) {
        if (Data.functionData) functionData1 = Data.functionData;
        if (Data.checkValue) checkFunction1 = Data.checkValue;
      }
      setFunctionData(functionData1);
      setCheckFunction(checkFunction1);
    }
  };
  const selectModule = (selectKeys: any, e: any) => {
    if (e.node.isLeaf) {
      setPanelTitle(e.node.title);
      let moduleId = selectKeys[0];
      setSelectModuleId(moduleId);
      setCheckFunction([]);
      getModuleFunction({ moduleId, roleId: Id });
    }
  };
  const changeFunc = async (values: any) => {
    if (values.length > 0) {
      let index = checkedModuleKeys.findIndex((id: any) => id === selectModuleId);
      if (index < 0) {
        let keys = [...checkedModuleKeys];
        keys.push(selectModuleId);
        setCheckedModuleKeys(keys);
      }
    }
    setCheckFunction(values);
    let param = { RoleFuncData: values, RoleId: Id, SmModuleId: selectModuleId };
    await http.put<any>("/api/SmRoleFunction/SaveModuleFunction", param);
  };
  // const onFuncPrivCheck = async (values: any) => {
  //   setCheckedFuncPrivKeys(values);
  //   let param = { FunctionList: values, RoleId: Id };
  //   await http.post<any>("/api/SmRoleFunction/SaveRoleFuncPriv", param);
  // };
  useImperativeHandle(formPageRef, function () {
    return { onSave, onSaveAdd };
  });

  let items: any[] = [
    {
      key: 1,
      label: "角色模块",
      children: (
        <Space style={{ display: "flex", flexDirection: "row", alignItems: "flex-start", justifyContent: "space-between" }}>
          <Tree
            defaultExpandedKeys={["All"]}
            // defaultExpandParent={true}
            checkedKeys={checkedModuleKeys}
            onCheck={onModuleCheck}
            checkable
            onSelect={selectModule}
            treeData={treeData}
          />
          {functionData.length > 0 ? (
            <Collapse defaultActiveKey={["1"]} expandIconPosition="right" style={{ width: "600px" }}>
              <Panel header={panelTitle} key="1">
                <Checkbox.Group value={checkFunction} options={functionData} onChange={value => changeFunc(value)} />
              </Panel>
            </Collapse>
          ) : null}
        </Space>
      )
      // },
      // {
      //   key: 2,
      //   label: "功能定义",
      //   children: (
      //     <Tree
      //       defaultExpandedKeys={["All"]}
      //       checkedKeys={checkedFuncPrivKeys}
      //       onCheck={onFuncPrivCheck}
      //       checkable
      //       treeData={treeFuncPrivData}
      //     />
      //   )
    }
  ];
  items.push();
  items.push();
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
            {isLoading ? <Loading /> : component()}
          </Form>
        </div>
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
