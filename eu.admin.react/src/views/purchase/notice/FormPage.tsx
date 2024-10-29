/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useEffect, useImperativeHandle, useState, useRef } from "react";
import { useDispatch } from "@/redux";
import { Flex, Form, Card } from "antd";
// import { Icon } from "@/components/Icon/index";
import FormToolbar from "@/components/FormToolbar/index";
import EditableProTable from "@/components/ProTableEditable/FormPage";
import { Loading } from "@/components/Loading/index";
import { querySingle, add, update } from "@/api/modules/module";
import Layout from "@/components/Elements/Layout";
import { RootState, useSelector } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { setId } from "@/redux/modules/module";
import http from "@/api";
import WaitSelect from "./WaitNoticeSelect";
// const { confirm } = Modal;
import { message } from "@/hooks/useMessage";

// type DataSourceType = {
//   id: React.Key;
//   title?: string;
//   readonly?: string;
//   decs?: string;
//   state?: string;
//   created_at?: string;
//   update_at?: string;
//   children?: DataSourceType[];
// };

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [auditStatus, setAuditStatus] = useState("");
  const [orderStatus, setOrderStatus] = useState("");
  const [waitSelectVisible, setWaitSelectVisible] = useState(false);

  const [form] = Form.useForm();
  // const tableRef = React.createRef<any>();
  const tableRef = useRef<any>();
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let {
    Id,
    moduleCode,
    formPageRef,
    // onReload,
    IsView,
    onDisabled,
    masterId,
    onReload,
    changePage
  } = props;
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;

  let { formColumns, openType, url, isDetail, masterColumn, menuData } = moduleInfo;
  let actionAuthButton: { [key: string]: boolean } = {};
  menuData?.forEach((item: any) => {
    actionAuthButton[item.FunctionCode] = true;
  });
  const querySingleData = async () => {
    let { Data, Success } = await querySingle({ Id: Id ?? id, moduleCode, url });
    if (Success) {
      dispatch(setId({ moduleCode, id: Id ?? id }));
      setAuditStatus(Data.AuditStatus);
      setOrderStatus(Data.OrderStatus);
      // debugger;
      if (Data.AuditStatus != "Add") {
        setDisabled(true);
        setModifyType(ModifyType.AuditPass);
      }
      if (IsView) setModifyType(ModifyType.View);
      form.setFieldsValue(Data);
    }
  };
  useEffect(() => {
    if (Id) {
      setModifyType(ModifyType.Edit);
      setViewId(Id);
      querySingleData();
    }

    setIsLoading(false);

    setDisabled(false);
  }, []);

  // const getAllMaterialType = async () => {
  //   let { Data, Success } = await http.get<any>("/api/MaterialType/GetAllMaterialType");
  //   if (Success) setTreeData([Data]);
  // };
  const component = () => {
    return (
      <Flex wrap="wrap">
        {formColumns.filter((f: { HideInForm: boolean; FromFieldGroup: any }) => f.HideInForm === false)?.length === 0
          ? null
          : formColumns
              .filter((f: any) => f.HideInForm === false)
              .map((item: any, index: any) => {
                const width = (item.GridSpan != null ? item?.GridSpan : 50) + "%";
                return (
                  <div
                    style={{
                      width
                    }}
                    key={index}
                  >
                    <Layout field={item} disabled={disabled ?? IsView} modifyType={modifyType} />
                  </div>
                );
              })}
      </Flex>
    );
  };
  const onFinish = async (data: any, type = "Save") => {
    message.loading("数据提交中...", 0);
    if (id) data = { ...data, url, Id: id ?? null };
    else data = { ...data, url };
    if (isDetail) data[masterColumn] = masterId;
    data["ModuleCode"] = moduleCode;

    for (let key in data) data[key] = data[key] ?? null;
    let { Data, Success, Message } = id ? await update(data) : await add(data);

    message.destroy();
    if (modifyType == ModifyType.View) {
      // modifyType = "1";
    }
    if (Success) {
      message.success(Message);
      setDisabledToolbar(true);
      if (onDisabled) onDisabled(true);
      if (openType === "Modal" || openType === "Drawer") onReload();
      if (type === "SaveAdd") {
        setViewId(null);
        setDisabled(true);
        form.resetFields();
      } else if (!id) {
        setViewId(Data);
        setModifyType(ModifyType.Edit);
        setOrderStatus("WaitShip");
        setAuditStatus("Add");
      }
    }
  };
  const onSave = () => form.validateFields().then(onFinish);
  const onSaveAdd = () => form.validateFields().then(values => onFinish(values, "SaveAdd"));
  const onValuesChange = () => {
    if (onDisabled) onDisabled(false);
    setDisabledToolbar(false);
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

  return (
    <>
      <Form
        labelCol={{
          xs: { span: 8 },
          sm: { span: 8 },
          md: { span: 8 }
        }}
        wrapperCol={{
          xs: { span: 16 },
          sm: { span: 16 },
          md: { span: 16 }
        }}
        labelWrap
        onFinish={onFinish}
        onValuesChange={onValuesChange}
        form={form}
      >
        <FormToolbar
          moduleInfo={moduleInfo}
          disabled={IsView === true ? true : disabled === true ? true : disabledToolbar}
          onFinishAdd={onSaveAdd}
          modifyType={orderStatus == "WaitShip" ? modifyType : ModifyType.View}
          auditStatus={auditStatus}
          masterId={id}
          onBack={() => changePage("FormIndex")}
          onReload={() => querySingleData()}
        />
        {isLoading ? (
          <Loading />
        ) : (
          <>
            <Card size="small" bordered={false}>
              {component()}
            </Card>

            <div style={{ height: 20 }}></div>
            <Card title="物料信息" bordered={false} className="card-small">
              <EditableProTable
                moduleCode="PO_ARRIVAL_ORDER_DETAIL_MNG"
                tableRef={tableRef}
                modifyType={modifyType}
                masterId={id}
                addCallBack={() => {
                  setWaitSelectVisible(true);
                }}
                editableCallBack={(originData: any, _data: any) => {
                  return originData;
                }}
              />
            </Card>
          </>
        )}
      </Form>
      <WaitSelect
        modalVisible={waitSelectVisible}
        selectedRowIds={[]}
        onCancel={() => setWaitSelectVisible(false)}
        onSubmit={async (values: any) => {
          message.loading("数据提交中...", 0);
          let { Success } = await http.post<any>("/api/PoArrivalOrder/BulkInsertDetail/" + id, values);
          message.destroy();
          if (Success) {
            message.success("提交成功！");
            querySingleData();
            if (tableRef.current) tableRef.current.reload();
          }
        }}
      />
    </>
  );
};

export default FormPage;
