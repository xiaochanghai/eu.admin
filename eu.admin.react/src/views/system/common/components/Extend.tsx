import { useImperativeHandle, useState } from "react";
import { Form, Input, Modal, Button, Row, Col, Drawer } from "antd";
import PermissionSet from "../../privilege/role/PermissionSet";
import { exportModuleSqlScript } from "@/api/modules/module";
import { downloadFile } from "@/utils";
import http from "@/api";
import { message } from "@/hooks/useMessage";
import { ComboGrid } from "@/components";

const FormItem = Form.Item;

/**
 * Extend组件 - 提供多种系统功能的扩展操作界面
 * 根据不同的moduleCode展示不同的功能界面
 */
const Extend: React.FC<any> = props => {
  // 状态定义
  const [modalVisible, setModalVisible] = useState(false); // 主模态框可见性
  const [isCronModalVisible, setIsCronModalVisible] = useState(false); // Cron模态框可见性
  const [tableCode, setTableCode] = useState<string | null>(null); // 表代码
  const [record, setRecord] = useState<any>({}); // 当前记录
  const [logContent, setLogContent] = useState(""); // 日志内容
  const [id, setId] = useState<string | null>(null); // 当前操作ID
  const [form] = Form.useForm(); // 表单实例

  // 从props中获取模块代码和引用
  const { moduleCode, extendPageRef } = props;

  // 临时存储action的引用
  let tempAction: any = {};

  /**
   * 表目录管理功能
   */
  // 初始化指定表
  const InitAssignmentTable = async () => {
    if (!tableCode) {
      message.error("请选择表代码!");
      return false;
    }
    message.loading("数据处理中...", 0);
    const { Success, Message } = await http.post<any>(`/api/SmTableCatalog/InitAssignmentTable/${tableCode}`);
    message.destroy();
    if (Success) message.success(Message);
  };

  // 初始化所有表
  const InitAllTable = async () => {
    message.loading("数据处理中...", 0);
    const { Success, Message } = await http.post<any>(`/api/SmTableCatalog/InitAllTable`);
    message.destroy();
    if (Success) message.success(Message);
  };

  /**
   * 模块管理功能
   */
  // 导出模块SQL脚本
  const ExportModuleSqlScript = async (action: any, selectedRows: any) => {
    if (selectedRows.length > 0) {
      const ids: string[] = selectedRows.map((item: { ID: any }) => item.ID);
      const { Success, Message, Data } = await exportModuleSqlScript(ids);

      if (Success) downloadFile(Data, "qaids");
      else message.error(Message);

      action.clearSelected();
    }
  };

  // 显示添加模态框
  const onSaveAdd = () => setModalVisible(true);

  /**
   * 任务管理功能
   */
  // 执行任务操作
  const jobExecute = async (Id: string, type = "LOG.CURRENT") => {
    message.loading("日志获取中...", 0);
    const { Success, Data } = await http.post<any>(`/api/SmQuartzJob/Operate/${Id}/${type}`, {});
    message.destroy();

    if (Success && type === "LOG.CURRENT") {
      setModalVisible(true);
      setLogContent(Data);
    }
  };

  // 修改任务Cron表达式
  const ModifyJobCron = async (value: string, action: any) => {
    tempAction = action;
    setIsCronModalVisible(true);
    setId(value);
  };

  // 处理Cron表达式修改确认
  const handleCronOk = () => {
    form.validateFields().then(async values => {
      message.loading("数据处理中...", 0);
      const { Success, Message } = await http.post<any>(`/api/SmQuartzJob/Operate/${id}/ARGS`, { ScheduleRule: values.args });

      if (Success) {
        setId(null);
        setIsCronModalVisible(false);
        form.resetFields();
        message.success(Message);
      }
    });
  };

  /**
   * 系统模块功能
   */
  // 复制系统模块
  const SysModuleCopy = (value: string, action: any) => {
    tempAction = action;
    setId(value);
    setIsCronModalVisible(true);
  };

  // 处理模块复制确认
  const handleModuleCopy = () => {
    form.validateFields().then(async values => {
      message.loading("数据处理中...", 0);
      const { Success, Message } = await http.post<any>(`/api/SmModule/Copy/${id}`, values);

      if (Success) {
        message.destroy();
        setIsCronModalVisible(false);
        tempAction?.reload();
        form.resetFields();
        message.success(Message);
      }
    });
  };

  /**
   * 用户角色功能
   */
  // 设置角色权限
  const SysRolePermissionSet = (value: string, action: any, record: any) => {
    tempAction = action;
    setId(value);
    setIsCronModalVisible(true);
    setRecord(record);
  };

  // 向父组件暴露方法
  useImperativeHandle(extendPageRef, () => ({
    onSaveAdd,
    ExportModuleSqlScript,
    InitAllTable,
    InitAssignmentTable,
    jobExecute,
    ModifyJobCron,
    SysModuleCopy,
    SysRolePermissionSet
  }));

  /**
   * 渲染不同模块的UI
   */
  return (
    <>
      {/* 表目录管理模块 */}
      {moduleCode === "SM_TABLE_CATALOG_MNG" && (
        <Modal
          destroyOnClose
          title="初始化表"
          open={modalVisible}
          maskClosable={false}
          width={1000}
          onCancel={() => setModalVisible(false)}
          footer={null}
        >
          <Row gutter={24} justify="center">
            <Col span={8}></Col>
            <Col span={8}>
              <ComboGrid code="SmTableCatalog1" onChange={(value: string) => setTableCode(value)} />
            </Col>
            <Col span={8}></Col>
          </Row>
          <div style={{ height: 10 }}></div>
          <Row gutter={24} justify="center">
            <Col span={6}></Col>
            <Col span={6} style={{ textAlign: "right" }}>
              <Button type="primary" onClick={InitAllTable}>
                初始化所有表
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
      )}

      {/* 任务管理模块 */}
      {moduleCode === "SM_JOB_MNG" && (
        <>
          {/* 日志查看模态框 */}
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

          {/* Cron表达式修改模态框 */}
          <Modal
            title="修改参数"
            open={isCronModalVisible}
            onOk={handleCronOk}
            onCancel={() => {
              setIsCronModalVisible(false);
              setId(null);
            }}
          >
            <Form labelCol={{ span: 6, xl: 6, md: 12, sm: 12 }} wrapperCol={{ span: 16 }} form={form}>
              <Row gutter={24} justify="center">
                <Col span={24}>
                  <FormItem name="args" label="Cron表达式" rules={[{ required: true }]}>
                    <Input placeholder="请输入" />
                  </FormItem>
                </Col>
              </Row>
            </Form>
          </Modal>
        </>
      )}

      {/* 系统模块管理 */}
      {moduleCode === "SM_MODULE_MNG" && (
        <Modal
          destroyOnClose
          title="复制模块"
          open={isCronModalVisible}
          onOk={handleModuleCopy}
          onCancel={() => {
            setIsCronModalVisible(false);
            setId(null);
          }}
        >
          <Form labelCol={{ span: 6, xl: 6, md: 12, sm: 12 }} wrapperCol={{ span: 16 }} form={form}>
            <Row gutter={24} justify="center">
              <Col span={24}>
                <FormItem name="ModuleCode" label="模块代码" rules={[{ required: true }]}>
                  <Input placeholder="请输入" />
                </FormItem>
              </Col>
            </Row>
            <Row gutter={24} justify="center">
              <Col span={24}>
                <FormItem name="ModuleName" label="模块名称" rules={[{ required: true }]}>
                  <Input placeholder="请输入" />
                </FormItem>
              </Col>
            </Row>
          </Form>
        </Modal>
      )}

      {/* 角色管理模块 */}
      {moduleCode === "SM_ROLE_MNG" && (
        <Drawer
          closable
          destroyOnClose
          title={`${record.RoleName} - 权限设置`}
          placement="right"
          open={isCronModalVisible}
          width={1000}
          onClose={() => {
            setIsCronModalVisible(false);
            setId(null);
          }}
        >
          <PermissionSet id={id} />
        </Drawer>
      )}
    </>
  );
};

export default Extend;
