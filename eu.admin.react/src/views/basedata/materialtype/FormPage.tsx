import React, { useEffect, useImperativeHandle, useState } from "react";
import { Card, Form } from "antd";
import { message } from "@/hooks/useMessage";
import { Button, Input, Row, Col, Space, InputNumber } from "antd";
import { querySingle, add, update } from "@/api/modules/module";
import { Loading } from "@/components";
// // import { ModuleInfo } from "@/api/interface/index";
// import { getModuleInfo } from "@/api/modules/module";
// import { setModuleInfo, setId } from "@/redux/modules/module";
// import http from "@/api";
const FormItem = Form.Item;

const FormPage: React.FC<any> = props => {
  // const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState(null);

  const [form] = Form.useForm();

  // const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const {
    Id,
    moduleCode,
    formPageRef,
    // onReload,
    IsView,
    onDisabled,
    parentTypeId,
    getAllMaterialType
    // masterId,
    // onReload
  } = props;
  // let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;

  // let { openType, url, isDetail, masterColumn } = moduleInfo;

  // let Id = !openType ? props.Id : ids[moduleCode];

  useEffect(() => {
    if (Id) {
      setViewId(Id);

      const querySingleData = async () => {
        let { Data, Success } = await querySingle({ Id, moduleCode, url: "/api/MaterialType" });
        if (Success) {
          // dispatch(setId({ moduleCode, id: Id }));
          form.setFieldsValue(Data);
        }
      };
      querySingleData();

      setIsLoading(false);

      setDisabled(false);
    } else {
      form.resetFields();
      setIsLoading(false);
      setViewId(null);
    }
    // const getModuleInfo1 = async () => {
    //   let { Data } = await getModuleInfo(moduleCode);
    //   dispatch(setModuleInfo(Data));
    // };
    // if (!moduleInfo) getModuleInfo1();
  }, [Id]);
  const onFinish = async (data: any, type = "Save") => {
    if (id) data = { ...data, url: "/api/MaterialType", Id: id ?? null };
    else data = { ...data, url: "/api/MaterialType" };
    // if (isDetail) data[masterColumn] = masterId;
    if (moduleCode != "SM_MODULE_MNG") data["ModuleCode"] = moduleCode;
    if (parentTypeId) data.ParentTypeId = parentTypeId;
    for (let key in data) data[key] = data[key] ?? null;
    let { Data, Success, Message } = id ? await update(data) : await add(data);

    if (Success) {
      message.success(Message);
      getAllMaterialType();
      if (onDisabled) onDisabled(true);
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

  useImperativeHandle(formPageRef, function () {
    return { onSave, onSaveAdd };
  });

  return (
    <>
      <div style={{ marginTop: 20, marginBottom: 20 }}>
        <Form
          labelCol={{ span: 6, xl: 6, md: 8, sm: 8 }}
          labelWrap
          wrapperCol={{ span: 16 }}
          onFinish={onFinish}
          onValuesChange={onValuesChange}
          form={form}
        >
          {isLoading ? (
            <Loading />
          ) : (
            <Card size="small" bordered={false}>
              <Row gutter={24} justify={"center"}>
                <Col span={12}>
                  <FormItem name="MaterialTypeNo" label="分类编号" rules={[{ required: true }]}>
                    <Input placeholder="请输入" disabled={disabled} />
                  </FormItem>
                </Col>
                <Col span={12}>
                  <FormItem name="MaterialTypeNames" label="分类名称" rules={[{ required: true }]}>
                    <Input placeholder="请输入" disabled={disabled} />
                  </FormItem>
                </Col>
              </Row>
              <Row gutter={24} justify={"center"}>
                <Col span={12}>
                  <FormItem name="TaxisNo" label="排序号">
                    <InputNumber placeholder="请输入" disabled={IsView} />
                  </FormItem>
                </Col>
                <Col span={12}></Col>
              </Row>
              <Space style={{ display: "flex", justifyContent: "center" }}>
                {!IsView ? (
                  <Button type="primary" htmlType="submit">
                    保存
                  </Button>
                ) : (
                  ""
                )}
                {/* <Button type="default" onClick={() => Index.changePage(<TableList />)}>返回</Button> */}
              </Space>
            </Card>
          )}
        </Form>
      </div>
    </>
  );
};

export default FormPage;
