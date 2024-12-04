import React, { useEffect, useImperativeHandle, useState } from "react";
import { useDispatch } from "@/redux";
import { Form, Flex } from "antd";
import { Loading } from "@/components/Loading/index";
// import TableList from "../../common/components/TableList";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import Layout from "@/components/Elements/Layout";
// import FormToolbar from "@/components/FormToolbar/index";
import { RootState, useSelector } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { message } from "@/hooks/useMessage";

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  // const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [id, setViewId] = useState(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [form] = Form.useForm();
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const {
    Id,
    // changePage,
    moduleCode,
    formPageRef,
    onClose,
    IsView,
    onDisabled,
    masterId,
    onReload,
    // childrenItems,
    setFormPageId
  } = props;
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;

  let { formColumns, openType, url, isDetail, masterColumn } = moduleInfo;

  useEffect(() => {
    if (dispatch && Id) {
      setViewId(Id);
      setModifyType(ModifyType.Edit);
      const querySingleData = async () => {
        let { Data, Success } = await querySingle({ Id, moduleCode, url });
        if (Success) {
          dispatch(setId({ moduleCode, id: Id }));
          setIsLoading(false);
          form.setFieldsValue(Data);
        }
      };
      querySingleData();
      setDisabled(false);

      if (IsView) setDisabled(true);
    } else {
      setDisabled(false);
      setIsLoading(false);
    }
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
                  <Layout field={item} disabled={disabled} modifyType={modifyType} />
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

      if (type != "SaveAdd" && onClose) onClose();
      if (type === "SaveAdd") {
        setViewId(null);
        setDisabled(true);
        form.resetFields();
      } else if (!id) {
        if (setFormPageId) setFormPageId(Data);
        setViewId(Data);
      }
    }
  };
  const onSave = () => form.validateFields().then(onFinish);
  const onSaveAdd = () => form.validateFields().then(values => onFinish(values, "SaveAdd"));
  const onValuesChange = () => {
    if (onDisabled) onDisabled(false);
    // setDisabledToolbar(false);
    setDisabled(false);
  };
  useImperativeHandle(formPageRef, function () {
    return { onSave, onSaveAdd };
  });
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
    </>
  );
};

export default FormPage;
