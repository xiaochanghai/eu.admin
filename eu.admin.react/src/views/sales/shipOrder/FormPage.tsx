import React, { useEffect, useImperativeHandle, useState, useRef } from "react";
import { Flex, Form, Card } from "antd";
import { querySingle, add, update } from "@/api/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { setId } from "@/redux/modules/module";
import http from "@/api";
import WaitShipSelect from "../salesOrder/WaitShipSelect";
import { message } from "@/hooks/useMessage";
import { Loading, Element, FormToolbar, EditableProTable } from "@/components";
import { ViewType } from "@/typings";

/**
 * 发货单表单页面组件
 * 功能：处理发货单的创建、编辑、查看和审核
 * 特性：
 * 1. 支持多种操作模式（新增/编辑/查看/审核）
 * 2. 包含物料信息表格
 * 3. 支持从待发货订单选择物料
 * 4. 内置表单验证和提交逻辑
 */
const FormPage: React.FC<{
  Id?: string | null; // 表单ID
  moduleCode: string; // 模块代码
  formPageRef?: React.Ref<any>; // 表单ref
  IsView?: boolean; // 是否查看模式
  onDisabled?: (disabled: boolean) => void; // 禁用状态回调
  masterId?: string; // 主表ID
  onReload?: () => void; // 重新加载回调
  changePage?: (page: ViewType) => void; // 页面切换回调
}> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState<string | null>(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [auditStatus, setAuditStatus] = useState("");
  const [orderStatus, setOrderStatus] = useState("");
  const [waitShipSelectVisible, setWaitShipSelectVisible] = useState(false);
  const [waitShipSelectType, setWaitShipSelectType] = useState("Ship");

  const [form] = Form.useForm();
  const tableRef = useRef<any>();
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const moduleInfo = moduleInfos[props.moduleCode] as ModuleInfo;
  const { formColumns, openType, url, isDetail, masterColumn, menuData } = moduleInfo;

  // 初始化操作权限
  const actionAuthButton: Record<string, boolean> = {};
  menuData?.forEach((item: any) => {
    actionAuthButton[item.FunctionCode] = true;
  });

  /**
   * 查询单条数据
   */
  const querySingleData = async () => {
    const { Data, Success } = await querySingle({
      Id: props.Id ?? id,
      moduleCode: props.moduleCode,
      url
    });

    if (Success) {
      dispatch(setId({ moduleCode: props.moduleCode, id: props.Id ?? id }));
      setAuditStatus(Data.AuditStatus);
      setOrderStatus(Data.OrderStatus);

      if (Data.AuditStatus !== ModifyType.Add) {
        setDisabled(true);
        setModifyType(ModifyType.AuditPass);
      }

      if (props.IsView) setModifyType(ModifyType.View);
      form.setFieldsValue(Data);
    }
  };

  // 初始化数据
  useEffect(() => {
    if (props.Id) {
      setModifyType(ModifyType.Edit);
      setViewId(props.Id);
      querySingleData();
    }
    setIsLoading(false);
    setDisabled(false);
  }, [props.Id]);

  /**
   * 渲染表单字段
   */
  const renderFormFields = () => (
    <Flex wrap="wrap">
      {formColumns
        .filter((f: any) => f.HideInForm === false)
        .map((item: any, index: number) => (
          <div style={{ width: `${item.GridSpan ?? 50}%` }} key={index}>
            <Element field={item} disabled={disabled ?? props.IsView} modifyType={modifyType} />
          </div>
        ))}
    </Flex>
  );

  /**
   * 表单提交处理
   * @param data 表单数据
   * @param type 提交类型 (Save/SaveAdd)
   */
  const handleSubmit = async (data: any, type = "Save") => {
    message.loading("数据提交中...", 0);

    const payload = {
      ...data,
      url,
      ...(id && { Id: id }),
      // ...(isDetail && { [masterColumn]: props.masterId }),
      ModuleCode: props.moduleCode
    };
    if (isDetail) data[masterColumn] = props.masterId;

    // 处理空值
    Object.keys(payload).forEach(key => {
      payload[key] = payload[key] ?? null;
    });

    const { Data, Success, Message } = id ? await update(payload) : await add(payload);

    message.destroy();

    if (Success) {
      message.success(Message);
      setDisabledToolbar(true);
      props.onDisabled?.(true);

      if (openType === "Modal" || openType === "Drawer") props.onReload?.();

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

  // 暴露方法给父组件
  useImperativeHandle(props.formPageRef, () => ({
    onSave: () => form.validateFields().then(handleSubmit),
    onSaveAdd: () => form.validateFields().then(values => handleSubmit(values, "SaveAdd"))
  }));

  return (
    <>
      <Form labelCol={{ span: 8 }} wrapperCol={{ span: 16 }} labelWrap onFinish={handleSubmit} form={form}>
        <FormToolbar
          moduleInfo={moduleInfo}
          disabled={props.IsView || disabled || disabledToolbar}
          modifyType={orderStatus === "WaitShip" ? modifyType : ModifyType.View}
          auditStatus={auditStatus}
          masterId={id}
          onBack={() => props.changePage?.(ViewType.INDEX)}
          onReload={querySingleData}
        />

        {isLoading ? (
          <Loading />
        ) : (
          <>
            <Card size="small" bordered={false}>
              {renderFormFields()}
            </Card>

            <div style={{ height: 20 }} />

            <Card title="物料信息" bordered={false} className="card-small">
              <EditableProTable
                moduleCode="SD_SHIP_ORDER_DETAIL_MNG"
                tableRef={tableRef}
                modifyType={modifyType}
                masterId={id}
                addCallBack={() => {
                  setWaitShipSelectVisible(true);
                  setWaitShipSelectType("Ship");
                }}
              />
            </Card>
          </>
        )}
      </Form>

      <WaitShipSelect
        modalVisible={waitShipSelectVisible}
        waitShipSelectType={waitShipSelectType}
        selectedRowIds={[]}
        onCancel={() => setWaitShipSelectVisible(false)}
        onSubmit={async (values: any) => {
          message.loading("数据提交中...", 0);
          const { Success } = await http.post<any>(`/api/SdShipOrder/BulkInsertDetail/${id}`, values);
          message.destroy();

          if (Success) {
            message.success("提交成功！");
            querySingleData();
            tableRef.current?.reload();
          }
        }}
      />
    </>
  );
};

export default FormPage;
