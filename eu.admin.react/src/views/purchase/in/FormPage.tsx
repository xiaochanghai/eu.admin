/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useEffect, useImperativeHandle, useState, useRef } from "react";
import { Flex, Form, Card, Popconfirm } from "antd";
import { getModuleInfo, querySingle, add, update } from "@/api/modules/module";
import { Element } from "@/components";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { setModuleInfo, setId } from "@/redux/modules/module";
import http from "@/api";
import WaitSelect from "./WaitSelect";
import { message } from "@/hooks/useMessage";
import { EditableProTable, FormToolbar, ComboGrid, Loading } from "@/components";

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
  const [supplierId, setSupplierId] = useState(null);
  const [auditStatus, setAuditStatus] = useState("");
  const [orderStatus, setOrderStatus] = useState("");
  const [orderSource, setOrderSource] = useState("");
  const [waitSelectVisible, setWaitSelectVisible] = useState(false);

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
  let moduleCode1 = "PO_IN_ORDER_DETAIL_MNG";
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
      // debugger;
      if (Data.AuditStatus != ModifyType.Add) {
        setDisabled(true);
        setModifyType(ModifyType.AuditPass);
      }
      setStockId(Data.StockId);
      setSupplierId(Data.SupplierId);
      setOrderSource(Data.OrderSource);
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
                      <Element
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
                      <Element
                        field={item}
                        disabled={stockId ? (disabled ?? IsView) : true}
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
          let { Success, Message } = await http.delete<any>("/api/PoArrivalOrderDetail/" + record.ID);
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
  let test1 = {
    title: "仓库",
    dataIndex: "StockId",
    width: 200,
    editable: true,
    hideInTable: false,
    align: "center",
    renderFormItem: (item: any, { isEditable }: any, _form: any) => {
      return isEditable ? (
        <ComboGrid
          code="BdStock"
          comboValue={item.entity.StockId ?? null}
          onChange={async (value: any) => {
            if (value) {
              item.entity = { ...item.entity, StockId: value };
              item.entity = { ...item.entity, GoodsLocationId: null };
              setStockId1(value);
              setGoodsLocationId(null);
              // let data = _form.getFieldsValue();
              // data[item.entity.ID].GoodsLocationId = null;
              // form.setFieldsValue(data);
              _form.setFieldsValue({
                [item.entity.ID]: {
                  GoodsLocationId: null
                }
              });

              // tableRef.current?.setRowData?.(item.entityID, { GoodsLocationId: null });
            }
            // form.setFieldsValue({ GoodsLocationId: null });
          }}
        />
      ) : (
        <>{item.entity.StockNo}-111</>
      );
    },
    render: (_text: any, record: any) => {
      return <>{record.StockName}</>;
    }
  };
  columns = [...columns, test1];

  test1 = {
    title: "货位",
    dataIndex: "GoodsLocationId",
    width: 150,
    editable: true,
    hideInTable: false,
    align: "center",
    renderFormItem: (item: any, { isEditable }: any, _form: any) => {
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
        <>{item.entity.StockNo}-111</>
      );
    },
    render: (_text: any, record: any) => {
      return <>{record.GoodsLocationName}</>;
    }
  };
  columns = [...columns, test1];
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
                  moduleCode={moduleCode1}
                  tableRef={tableRef}
                  modifyType={modifyType}
                  masterId={id}
                  columns={columns}
                  addCallBack={() => {
                    setWaitSelectVisible(true);
                  }}
                  editableCallBack={(originData: any, data: any) => {
                    originData.StockName = data.StockName;
                    originData.GoodsLocationName = data.GoodsLocationName;
                    return originData;
                  }}
                />
              ) : (
                <Loading />
              )}
            </Card>
          </>
        )}
      </Form>
      {supplierId && orderSource ? (
        <WaitSelect
          modalVisible={waitSelectVisible}
          moduleCode={orderSource == "NoticeOrder" ? "PO_NOTICE_ORDER_WAIT_IN_MNG" : "PO_ORDER_WAIT_IN_MNG"}
          supplierId={supplierId}
          onCancel={() => setWaitSelectVisible(false)}
          onSubmit={async (values: any) => {
            message.loading("数据提交中...", 0);
            let { Success } = await http.post<any>("/api/PoInOrder/BulkInsertDetail/" + id, values);
            message.destroy();
            if (Success) {
              message.success("提交成功！");
              querySingleData();
              if (tableRef.current) tableRef.current.reload();
              // tableAction?.clearSelected();
              // tableAction?.reload();
              // if (tableRef.current) tableRef.current.reload();
            }
          }}
        />
      ) : null}
    </>
  );
};

export default FormPage;
