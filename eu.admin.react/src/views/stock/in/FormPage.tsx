/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useEffect, useImperativeHandle, useState, useRef } from "react";
import { useDispatch } from "@/redux";
import { Flex, Form, Card, Popconfirm } from "antd";
import FormToolbar from "@/components/FormToolbar/index";
import EditableProTable from "@/components/ProTableEditable/index";
import { Loading } from "@/components/Loading/index";
import { getModuleInfo, querySingle, add, update } from "@/api/modules/module";
import Layout from "@/components/Elements/Layout";
import { RootState, useSelector } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { setModuleInfo, setId } from "@/redux/modules/module";
import http from "@/api";
import { message } from "@/hooks/useMessage";
import ComboGrid from "@/components/ComBoGrid/index";
import { createUuid } from "@/utils";

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [stockId, setStockId] = useState(null);
  const [stockId1, setStockId1] = useState(null);
  const [goodsLocationId, setGoodsLocationId] = useState(null);
  const [auditStatus, setAuditStatus] = useState("");
  const [orderStatus, setOrderStatus] = useState("");
  const [dataSource, setDataSource] = useState<any>([]);
  // const [editableKeys, setEditableRowKeys] = useState<React.Key[]>([]);

  const [form] = Form.useForm();
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
  let moduleCode1 = "IV_IN_DETAIL_MNG";
  let moduleInfo1 = moduleInfos[moduleCode1];

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

    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo(moduleCode1);
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
                if (item.DataIndex == "StockId")
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <Layout
                        field={item}
                        disabled={disabled ?? IsView}
                        modifyType={modifyType}
                        onChange={async (value: any) => {
                          setStockId(value);
                          form.setFieldsValue({ GoodsLocationId: null });
                        }}
                      />
                    </div>
                  );
                else if (item.DataIndex == "GoodsLocationId")
                  return (
                    <div
                      style={{
                        width
                      }}
                      key={index}
                    >
                      <Layout
                        field={item}
                        disabled={stockId ? disabled ?? IsView : true}
                        modifyType={modifyType}
                        parentColumn="StockId"
                        parentId={stockId}
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

      if (tableRef.current) tableRef.current.reload();
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

  useImperativeHandle(formPageRef, function () {
    return { onSave, onSaveAdd };
  });

  const actionColumn = {
    title: "操作",
    dataIndex: "option",
    fixed: "right",
    valueType: "option",
    width: 150,
    // render: (text, record, _, action) => component(text, record, _, action)

    render: (_text: any, record: any, _: any, action: any) => [
      <a
        key="editable"
        onClick={() => {
          setStockId1(null);

          // if (editableKeys.length > 0) action?.saveEditable?.(editableKeys[0]);
          setGoodsLocationId(record.GoodsLocationId);
          setStockId1(record.StockId);
          action?.startEditable?.(record.ID);
        }}
      >
        编辑
      </a>,
      <Popconfirm
        title="提醒"
        description="是否确定删除记录?"
        onConfirm={async () => {
          let { Success, Message } = await http.delete<any>("/api/IvInDetail/" + record.ID);
          if (Success) message.success(Message);
          if (tableRef.current) tableRef.current.reload();
        }}
        okType="danger"
        okText="确定"
        cancelText="取消"
      >
        <a key="delete">删除</a>
      </Popconfirm>
    ]
  };
  let columns: any = [];
  if (modifyType == ModifyType.Edit) {
    if (moduleInfo1 && moduleInfo1.columns) columns = [...moduleInfo1.columns, actionColumn];
  } else if (moduleInfo1 && moduleInfo1.columns) columns = [...moduleInfo1.columns];

  columns &&
    columns.map((item: any, index: any) => {
      let hasChange = false;
      let column = columns[index];
      let formItemProps = () => {
        return {
          rules: [{ required: true, message: "此项为必填项" }]
        };
      };
      if (item.dataIndex == "MaterialId") {
        let renderFormItem = (_item: any, { isEditable }: any, _form: any) => {
          return isEditable ? <ComboGrid code="BdMaterial" /> : null;
        };
        let render = (_text: any, record: any) => {
          return <>{record.MaterialName}</>;
        };

        column = { ...column, renderFormItem, render };
        hasChange = true;
      } else if (item.dataIndex == "StockId") {
        let renderFormItem = (item: any, { isEditable }: any, _form: any) => {
          return isEditable ? (
            <ComboGrid
              code="BdStock"
              onChange={async (value: any) => {
                if (value) {
                  item.entity = { ...item.entity, StockId: value };
                  item.entity = { ...item.entity, GoodsLocationId: null };
                  setStockId1(value);
                  setGoodsLocationId(null);
                  _form.setFieldsValue({
                    [item.entity.ID]: {
                      GoodsLocationId: null
                    }
                  });
                }
              }}
            />
          ) : null;
        };
        let render = (_text: any, record: any) => {
          return <>{record.StockName != "" ? record.StockName : "-"}</>;
        };

        column = { ...column, renderFormItem, render };
        hasChange = true;
      } else if (item.dataIndex == "GoodsLocationId") {
        let renderFormItem = (item: any, { isEditable }: any, _form: any) => {
          _form.setFieldsValue({
            GoodsLocationId: goodsLocationId
          });
          return isEditable ? (
            <ComboGrid
              code="BdGoodsLocation"
              value={goodsLocationId}
              onChange={async (value: any) => {
                if (value) item.entity = { ...item.entity, GoodsLocationId: value };
              }}
              disabled={stockId1 ? false : true}
              parentColumn="StockId"
              parentId={stockId1 ? stockId1 : null}
            />
          ) : (
            <>{item.entity.MaterialName}-111</>
          );
        };
        let render = (_text: any, record: any) => {
          return <>{record.GoodsLocationName != "" ? record.GoodsLocationName : "-"}</>;
        };

        column = { ...column, renderFormItem, render };
        hasChange = true;
      }

      if (item.required === true) {
        hasChange = true;
        column = { ...column, formItemProps };
      }

      if (hasChange == true) columns[index] = column;
    });
  const tableProps: any = {
    moduleCode: moduleCode1,
    tableRef,
    modifyType,
    masterId: id,
    moduleInfo: moduleInfo1,
    columns
  };
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
              {moduleInfo1 && columns ? (
                <EditableProTable
                  // addCallBack={() => {
                  //   tableRef.current.addEditRecord?.({
                  //     ID: createUuid()
                  //   });
                  //   // setWaitSelectVisible(true);
                  // }}
                  recordCreatorProps={{
                    position: "end",
                    // newRecordType: "dataSource",
                    record: () => ({
                      ID: createUuid()
                    })
                  }}
                  value={dataSource}
                  onChange={setDataSource}
                  successCallBack={(originData: any, data: any) => {
                    // let originData1 = Object.assign({}, originData, data);
                    originData.MaterialName = data.MaterialName;
                    originData.Specifications = data.Specifications;
                    originData.UnitName = data.UnitName;
                    originData.GoodsLocationName = data.GoodsLocationName;
                    originData.StockName = data.StockName;
                    originData.QTY = data.QTY;
                    originData.Amount = data.Amount;
                    return originData;
                  }}
                  failCallBack={() => {
                    if (tableRef.current) tableRef.current.reload();
                  }}
                  {...tableProps}
                />
              ) : (
                <Loading />
              )}
            </Card>
          </>
        )}
      </Form>
    </>
  );
};

export default FormPage;
