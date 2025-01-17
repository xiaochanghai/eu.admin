import { useImperativeHandle, useState } from "react";
import { Form, Input, Modal, Button, Row, Col, Drawer } from "antd";
import ComboGrid from "@/components/ComBoGrid/index";
import PermissionSet from "../../privilege/role/PermissionSet";
import { exportModuleSqlScript } from "@/api/modules/module";
import { downloadFile } from "@/utils";
import http from "@/api";
import { message } from "@/hooks/useMessage";

const FormItem = Form.Item;
let tempAction: any = {};
const Extend: React.FC<any> = props => {
  const [modalVisible, setModalVisible] = useState(false);
  const [isCronModalVisible, setIsCronModalVisible] = useState(false);
  const [tableCode, setTableCode] = useState(null);
  const [record, setRecord] = useState<any>({});
  const [logContent, setLogContent] = useState("");
  const [id, setId] = useState(null);
  const [form] = Form.useForm();
  const { moduleCode, extendPageRef } = props;

  const InitAssignmentTable = async () => {
    if (!tableCode) {
      message.error("请选择表代码!");
      return false;
    }
    message.loading("数据处理中...", 0);
    let { Success, Message } = await http.post<any>(`/api/SmTableCatalog/InitAssignmentTable/${tableCode}`);
    if (Success) message.success(Message);
  };
  const InitAllTable = async () => {
    message.loading("数据处理中...", 0);
    let { Success, Message } = await http.post<any>(`/api/SmTableCatalog/InitAllTable`);
    if (Success) message.success(Message);
  };
  const ExportModuleSqlScript = async (action: any, selectedRows: any) => {
    if (selectedRows.length > 0) {
      let { Success, Message, Data } = await exportModuleSqlScript(selectedRows);
      if (Success) downloadFile(Data, "qaids");
      else message.error(Message);
      action.clearSelected();
    }
  };
  const onSaveAdd = () => {
    setModalVisible(true);
  };

  // 任务管理 Begin
  const jobExecute = async (Id: any, type = "LOG.CURRENT") => {
    message.loading("日志获取中...", 0);
    let { Success, Data } = await http.post<any>(`/api/SmQuartzJob/Operate/${Id}/${type}`, {});
    message.destroy();
    if (Success) {
      if (type == "LOG.CURRENT") {
        setModalVisible(true);
        setLogContent(Data);
      }
      // me.actionRef.current.reload();
      // me.formRef1.current.resetFields();
      // message.success(Message);
    }
  };
  const ModifyJobCron = async (value: any, action: any) => {
    tempAction = action;
    setIsCronModalVisible(true);
    setId(value);
  };
  const handleCronOk = () => {
    form.validateFields().then(async values => {
      message.loading("数据处理中...", 0);
      let { Success, Message } = await http.post<any>(`/api/SmQuartzJob/Operate/${id}/ARGS`, { ScheduleRule: values.args });
      if (Success) {
        setId(null);
        setIsCronModalVisible(false);
        form.resetFields();
        // tempAction.reload();
        message.success(Message);
      }
    });
  };
  // 任务管理 End

  //系统模块 Begin
  const SysModuleCopy = (value: any, action: any) => {
    tempAction = action;
    setId(value);
    setIsCronModalVisible(true);
  };
  const handleModuleCopy = () => {
    form.validateFields().then(async values => {
      message.loading("数据处理中...", 0);
      let { Success, Message } = await http.post<any>(`/api/SmModule/Copy/${id}`, values);

      if (Success) {
        message.destroy();
        setIsCronModalVisible(false);
        tempAction?.reload();
        form.resetFields();
        message.success(Message);
      }
    });
  };
  //系统模块 End

  //用户角色 Begin
  const SysRolePermissionSet = (value: any, action: any, record: any) => {
    tempAction = action;
    setId(value);
    setIsCronModalVisible(true);
    setRecord(record);
  };
  //用户角色 End

  useImperativeHandle(extendPageRef, function () {
    return {
      onSaveAdd,
      ExportModuleSqlScript,
      InitAllTable,
      InitAssignmentTable,
      jobExecute,
      ModifyJobCron,
      SysModuleCopy,
      SysRolePermissionSet
    };
  });
  return (
    <>
      {moduleCode == "SM_TABLE_CATALOG_MNG" ? (
        <Modal
          destroyOnClose
          title="初始化表"
          open={modalVisible}
          // onOk={this.okHandle}
          maskClosable={false}
          width={1000}
          onCancel={() => setModalVisible(false)}
          footer={null}
        >
          <Row gutter={24} justify={"center"}>
            <Col span={8}></Col>
            <Col span={8}>
              <ComboGrid
                code="SmTableCatalog1"
                onChange={(value: any) => {
                  setTableCode(value);
                }}
              />
            </Col>
            <Col span={8}></Col>
          </Row>
          <div style={{ height: 10 }}></div>
          <Row gutter={24} justify={"center"}>
            <Col span={6}></Col>
            <Col span={6} style={{ textAlign: "right" }}>
              <Button type="primary" onClick={InitAllTable}>
                c
              </Button>
            </Col>
            <Col span={6}>
              <Button type="primary" onClick={InitAssignmentTable}>
                初始化指定表
              </Button>
            </Col>
            <Col span={6}></Col>
          </Row>
        </Modal>
      ) : null}
      {moduleCode == "SM_JOB_MNG" ? (
        <>
          <Modal
            title="当前日志"
            open={modalVisible}
            width={1200}
            destroyOnClose={true}
            onOk={() => {
              setModalVisible(false);
              setLogContent("");
            }}
            onCancel={() => {
              setModalVisible(false);
              setLogContent("");
            }}
          >
            <div dangerouslySetInnerHTML={{ __html: logContent }} />
          </Modal>
          <Modal
            title="修改参数"
            open={isCronModalVisible}
            onOk={() => handleCronOk()}
            onCancel={() => {
              setIsCronModalVisible(false);
              setId(null);
            }}
          >
            <Form labelCol={{ span: 6, xl: 6, md: 12, sm: 12 }} wrapperCol={{ span: 16 }} form={form}>
              <Row gutter={24} justify={"center"}>
                <Col span={24}>
                  <FormItem name="args" label="Cron表达式" rules={[{ required: true }]}>
                    <Input placeholder="请输入" />
                  </FormItem>
                </Col>
              </Row>
            </Form>
          </Modal>
        </>
      ) : null}
      {moduleCode == "SM_MODULE_MNG" ? (
        <>
          <Modal
            destroyOnClose
            title="复制模块"
            open={isCronModalVisible}
            onOk={() => handleModuleCopy()}
            onCancel={() => {
              setIsCronModalVisible(false);
              setId(null);
            }}
          >
            <Form labelCol={{ span: 6, xl: 6, md: 12, sm: 12 }} wrapperCol={{ span: 16 }} form={form}>
              <Row gutter={24} justify={"center"}>
                <Col span={24}>
                  <FormItem name="ModuleCode" label="模块代码" rules={[{ required: true }]}>
                    <Input placeholder="请输入" />
                  </FormItem>
                </Col>
              </Row>
              <Row gutter={24} justify={"center"}>
                <Col span={24}>
                  <FormItem name="ModuleName" label="模块名称" rules={[{ required: true }]}>
                    <Input placeholder="请输入" />
                  </FormItem>
                </Col>
              </Row>
            </Form>
          </Modal>
        </>
      ) : null}
      {moduleCode == "SM_ROLE_MNG" ? (
        <>
          <Drawer
            closable
            destroyOnClose
            title={record.RoleName + " - 权限设置"}
            placement="right"
            open={isCronModalVisible}
            // loading={loading}
            width={1000}
            onClose={() => {
              setIsCronModalVisible(false);
              setId(null);
            }}
          >
            <PermissionSet id={id} />
          </Drawer>
        </>
      ) : null}
    </>
  );
};
export default Extend;
