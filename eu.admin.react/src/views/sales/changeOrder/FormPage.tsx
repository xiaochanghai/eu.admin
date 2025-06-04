/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useEffect, useImperativeHandle, useState, useRef } from "react";
import { Flex, Form, Card, message } from "antd";
import { getModuleInfo, querySingle, add, update } from "@/api/modules/module";
import MaterialQuery from "../salesOrder/MaterialQuery";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { setModuleInfo, setId } from "@/redux/modules/module";
import http from "@/api";
import { Loading, Element, FormToolbar, EditableProTable } from "@/components";

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
  const [materialQueryVisible, setMaterialQueryVisible] = useState(false);
  const [id, setViewId] = useState(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [taxType, setTaxType] = useState("");
  const [auditStatus, setAuditStatus] = useState("");
  const [orderStatus, setOrderStatus] = useState("");
  // const [materialTotal, setMaterialTotal] = useState(0);

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
  let moduleInfo1 = moduleInfos["SD_SALES_CHANGE_ORDER_DETAIL_MNG"];

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
      setOrderStatus(Data.SalesOrderStatus);
      // debugger;
      if (Data.AuditStatus != ModifyType.Add) {
        setDisabled(true);
        setModifyType(ModifyType.AuditPass);
      }
      if (IsView || Data.OrderStatus == "Valid") setModifyType(ModifyType.View);
      form.setFieldsValue(Data);
    }
  };
  useEffect(() => {
    if (Id) {
      setModifyType(ModifyType.Edit);
      setViewId(Id);
      querySingleData();
    }

    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo("SD_SALES_CHANGE_ORDER_DETAIL_MNG");
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo1) getModuleInfo1();

    setIsLoading(false);

    setDisabled(false);
  }, []);

  const component = () => {
    return (
      <Flex wrap="wrap">
        {formColumns.filter((f: { HideInForm: boolean; FromFieldGroup: any }) => f.HideInForm === false)?.length === 0
          ? null
          : formColumns
              .filter((f: any) => f.HideInForm === false)
              .map((item: any, index: any) => {
                const width = (item.GridSpan != null ? item?.GridSpan : 50) + "%";
                if (item.DataIndex == "CustomerId")
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <Element
                        field={item}
                        disabled={disabled ?? IsView}
                        modifyType={modifyType}
                        onChange={async (value: any) => {
                          if (value) {
                            let { Data, Success } = await http.get<any>("/api/Customer/" + value);
                            if (Success)
                              form.setFieldsValue({
                                TaxType: Data.TaxType,
                                TaxRate: Data.TaxRate,
                                CurrencyId: Data.CurrencyId,
                                SettlementWayId: Data.SettlementWayId,
                                SalesmanId: Data.EmployeeId
                              });
                            let result = await http.get<any>("/api/CustomerDeliveryAddress/GetDefaultData/" + value);
                            if (result.Success && result.Data)
                              form.setFieldsValue({
                                Contact: result.Data.Contact,
                                Phone: result.Data.Phone,
                                Address: result.Data.Address
                              });
                          }
                        }}
                      />
                    </div>
                  );
                else if (item.DataIndex == "TaxType")
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <Element
                        field={item}
                        disabled={disabled ?? IsView}
                        modifyType={modifyType}
                        onChange={(value: any) => {
                          if (value == "ZeroTax")
                            form.setFieldsValue({
                              TaxRate: 0
                            });

                          setTaxType(value);
                        }}
                      />
                    </div>
                  );
                else if (item.DataIndex == "TaxRate")
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <Element
                        field={item}
                        disabled={disabled ?? IsView}
                        modifyType={taxType == "ZeroTax" ? ModifyType.Add : modifyType}
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
                      <Element field={item} disabled={disabled ?? IsView} modifyType={modifyType} />
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
      if (tableRef.current) tableRef.current.reload();

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
            <MaterialQuery
              modalVisible={materialQueryVisible}
              onCancel={() => setMaterialQueryVisible(false)}
              onSubmit={async (values: any) => {
                for (let i = 0; i < values.length; i++) values[i].OrderId = id;
                let { Success } = await http.post<any>("/api/SdOrderDetail/BulkInsert", values);
                if (Success) {
                  if (tableRef.current) tableRef.current.reload();
                }
              }}
            />
            <Card title="物料信息" bordered={false} className="card-small">
              <EditableProTable
                moduleCode="SD_SALES_CHANGE_ORDER_DETAIL_MNG"
                tableRef={tableRef}
                modifyType={modifyType}
                masterId={id}
                editableCallBack={(originData: any, data: any) => {
                  originData.NoTaxAmount = data.NoTaxAmount;
                  originData.TaxAmount = data.TaxAmount;
                  originData.TaxIncludedAmount = data.TaxIncludedAmount;
                  return originData;
                }}
              />
            </Card>
          </>
        )}
      </Form>
    </>
  );
};

export default FormPage;
