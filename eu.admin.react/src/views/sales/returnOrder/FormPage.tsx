/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useEffect, useImperativeHandle, useState, useRef } from "react";
import { Form, Card, message } from "antd";
import { querySingle, add, update } from "@/api/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { setId } from "@/redux/modules/module";
import http from "@/api";
import WaitReturnSelect from "./WaitReturnSelect";
import { Loading, FormToolbar, EditableProTable, renderFormComponent } from "@/components";
import { SaveTypeEnum, ModifyType } from "@/typings";
import { STANDARD_FORM_LAYOUT } from "@/config";

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [auditStatus, setAuditStatus] = useState("");
  const [orderStatus, setOrderStatus] = useState("");
  const [WaitReturnSelectVisible, setWaitReturnSelectVisible] = useState(false);

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

  let { formColumns, url, isDetail, masterColumn, menuData } = moduleInfo;
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
      if (Data.AuditStatus != ModifyType.Add) {
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
  const onFinish = async (data: any, type = SaveTypeEnum.Save) => {
    message.loading("数据提交中...", 0);
    if (id) data = { ...data, url, Id: id ?? null };
    else data = { ...data, url };
    if (isDetail) data[masterColumn] = masterId;
    data["ModuleCode"] = moduleCode;

    for (let key in data) data[key] = data[key] ?? null;
    let { Data, Success, Message } = id ? await update(data) : await add(data);

    message.destroy();

    if (Success) {
      message.success(Message);
      setDisabledToolbar(true);
      onDisabled?.(true);
      if (type === SaveTypeEnum.SaveAdd) {
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
  const onSaveAdd = () => form.validateFields().then(values => onFinish(values, SaveTypeEnum.SaveAdd));
  const onValuesChange = () => {
    onDisabled?.(false);
    setDisabledToolbar(false);
    setDisabled(false);
  };

  useImperativeHandle(formPageRef, function () {
    return { onSave, onSaveAdd };
  });

  return (
    <>
      <Form {...STANDARD_FORM_LAYOUT} labelWrap onFinish={onFinish} onValuesChange={onValuesChange} form={form}>
        <FormToolbar
          moduleInfo={moduleInfo}
          disabled={IsView === true ? true : disabled === true ? true : disabledToolbar}
          onFinishAdd={onSaveAdd}
          modifyType={orderStatus === "WaitReturn" ? modifyType : ModifyType.View}
          auditStatus={auditStatus}
          masterId={id}
          onBack={() => {
            onReload?.();
            changePage("FormIndex");
          }}
          onReload={() => querySingleData()}
        />
        {isLoading ? (
          <Loading />
        ) : (
          <>
            <Card size="small" variant="borderless">
              {renderFormComponent(formColumns, disabled, modifyType)}
            </Card>

            <div style={{ height: 20 }}></div>

            <Card title="物料信息" variant="borderless" className="card-small">
              <EditableProTable
                moduleCode="SD_RETURN_ORDER_DETAIL_MNG"
                tableRef={tableRef}
                modifyType={modifyType}
                masterId={id}
                addCallBack={() => setWaitReturnSelectVisible(true)}
                editableCallBack={(originData: any, _data: any) => {
                  // originData.NoTaxAmount = data.NoTaxAmount;
                  // originData.TaxAmount = data.TaxAmount;
                  // originData.TaxIncludedAmount = data.TaxIncludedAmount;
                  return originData;
                }}
              />
            </Card>
          </>
        )}
      </Form>
      <WaitReturnSelect
        modalVisible={WaitReturnSelectVisible}
        selectedRowIds={[]}
        onCancel={() => setWaitReturnSelectVisible(false)}
        onSubmit={async (values: any) => {
          message.loading("数据提交中...", 0);
          let { Success, Message } = await http.post<any>("/api/SdReturnOrder/BulkInsertDetail/" + id, values);
          message.destroy();
          if (Success) {
            message.success(Message);
            querySingleData();
            tableRef.current?.reload();
          }
        }}
      />
    </>
  );
};

export default FormPage;
