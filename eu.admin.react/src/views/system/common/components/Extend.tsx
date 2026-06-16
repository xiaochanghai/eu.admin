import { useImperativeHandle, useState, useCallback, useRef } from "react";
import { Form, Input, Modal, Button, Row, Col, Drawer } from "antd";
import { useTranslation } from "react-i18next";
import PermissionSet from "../../privilege/role/PermissionSet";
import { exportModuleSqlScript } from "@/api/modules/module";
import { downloadFile } from "@/utils";
import http from "@/api";
import { message } from "@/hooks/useMessage";
import { ComboGrid } from "@/components";

const FormItem = Form.Item;

/**
 * 模块代码常量
 */
const MODULE_CODES = {
  TABLE_CATALOG: "SM_TABLE_CATALOG_MNG",
  JOB: "SM_JOB_MNG",
  MODULE: "SM_MODULE_MNG",
  ROLE: "SM_ROLE_MNG",
  USER: "SM_USER_MNG"
} as const;

/**
 * 表单布局配置
 */
const FORM_LAYOUT = {
  labelCol: { span: 6, xl: 6, md: 12, sm: 12 },
  wrapperCol: { span: 16 }
} as const;

/**
 * Extend组件 Props 接口定义
 */
interface ExtendProps {
  /**
   * 模块代码
   */
  moduleCode: string;
  /**
   * 扩展页面引用
   */
  extendPageRef?: React.Ref<ExtendPageRef>;
}

/**
 * 暴露给父组件的方法接口
 */
export interface ExtendPageRef {
  onSaveAdd: () => void;
  ExportModuleSqlScript: (action: ActionRef, selectedRows: SelectedRow[]) => Promise<void>;
  InitAllTable: () => Promise<void>;
  InitAssignmentTable: () => Promise<false | undefined>;
  jobExecute: (id: string, type?: string) => Promise<void>;
  ModifyJobCron: (value: string, action: ActionRef) => void;
  SysModuleCopy: (value: string, action: ActionRef) => void;
  SysRolePermissionSet: (value: string, action: ActionRef, record: RoleRecord) => void;
  UserPasswordReset: (value: string, action: ActionRef, record: RoleRecord) => void;
}

/**
 * Action引用接口
 */
interface ActionRef {
  reload?: () => void;
  clearSelected?: () => void;
}

/**
 * 选中行接口
 */
interface SelectedRow {
  ID: string;
}

/**
 * 角色记录接口
 */
interface RoleRecord {
  RoleName: string;
  [key: string]: unknown;
}

/**
 * Extend组件 - 提供多种系统功能的扩展操作界面
 * 根据不同的moduleCode展示不同的功能界面
 */
const Extend: React.FC<ExtendProps> = ({ moduleCode, extendPageRef }) => {
  const { t } = useTranslation();
  // 状态定义
  const [modalVisible, setModalVisible] = useState(false); // 主模态框可见性
  const [isCronModalVisible, setIsCronModalVisible] = useState(false); // Cron/抽屉可见性
  const [tableCode, setTableCode] = useState<string | null>(null); // 表代码
  const [record, setRecord] = useState<RoleRecord>({ RoleName: "" }); // 当前记录
  const [logContent, setLogContent] = useState(""); // 日志内容
  const [id, setId] = useState<string | null>(null); // 当前操作ID
  const [form] = Form.useForm(); // 表单实例

  // 使用useRef存储action引用
  const actionRef = useRef<ActionRef>({});

  /**
   * 表目录管理功能
   */
  // 初始化指定表
  const InitAssignmentTable = useCallback(async () => {
    if (!tableCode) {
      message.error(t("extend.selectTableCode"));
      return false;
    }
    message.loading(t("extend.dataProcessing"), 0);
    const { Success, Message } = await http.post<any>(`/api/SmTableCatalog/InitAssignmentTable/${tableCode}`);
    message.destroy();
    if (Success) message.success(Message);
  }, [tableCode, t]);

  // 初始化所有表
  const InitAllTable = useCallback(async () => {
    message.loading(t("extend.dataProcessing"), 0);
    const { Success, Message } = await http.post<any>(`/api/SmTableCatalog/InitAllTable`);
    message.destroy();
    if (Success) message.success(Message);
  }, [t]);

  /**
   * 模块管理功能
   */
  // 导出模块SQL脚本
  const ExportModuleSqlScript = useCallback(async (action: ActionRef, selectedRows: SelectedRow[]) => {
    if (selectedRows.length > 0) {
      const ids: string[] = selectedRows.map(item => item.ID);
      const { Success, Message, Data } = await exportModuleSqlScript(ids);

      if (Success) downloadFile(Data, "qaids");
      else message.error(Message);

      action.clearSelected?.();
    }
  }, []);

  // 显示添加模态框
  const onSaveAdd = useCallback(() => setModalVisible(true), []);

  /**
   * 关闭主模态框
   */
  const closeMainModal = useCallback(() => {
    setModalVisible(false);
    setLogContent("");
  }, []);

  /**
   * 关闭Cron/抽屉模态框
   */
  const closeCronModal = useCallback(() => {
    setIsCronModalVisible(false);
    setId(null);
    form.resetFields();
  }, [form]);

  /**
   * 任务管理功能
   */
  // 执行任务操作
  const jobExecute = useCallback(async (jobId: string, type = "LOG.CURRENT") => {
    message.loading(t("extend.gettingLogs"), 0);
    const { Success, Data } = await http.post<any>(`/api/SmQuartzJob/Operate/${jobId}/${type}`, {});
    message.destroy();

    if (Success && type === "LOG.CURRENT") {
      setModalVisible(true);
      setLogContent(Data);
    }
  }, [t]);

  // 修改任务Cron表达式
  const ModifyJobCron = useCallback((value: string, action: ActionRef) => {
    actionRef.current = action;
    setIsCronModalVisible(true);
    setId(value);
  }, []);

  // 处理Cron表达式修改确认
  const handleCronOk = useCallback(() => {
    form.validateFields().then(async values => {
      message.loading(t("extend.dataProcessing"), 0);
      const { Success, Message } = await http.post<any>(`/api/SmQuartzJob/Operate/${id}/ARGS`, {
        ScheduleRule: values.args
      });
      message.destroy();

      if (Success) {
        closeCronModal();
        message.success(Message);
        actionRef.current.reload?.();
      }
    });
  }, [id, form, closeCronModal, t]);

  /**
   * 系统模块功能
   */
  // 复制系统模块
  const SysModuleCopy = useCallback((value: string, action: ActionRef) => {
    actionRef.current = action;
    setId(value);
    setIsCronModalVisible(true);
  }, []);

  // 处理模块复制确认
  const handleModuleCopy = useCallback(() => {
    form.validateFields().then(async values => {
      message.loading(t("extend.dataProcessing"), 0);
      const { Success, Message } = await http.post<any>(`/api/SmModule/Copy/${id}`, values);
      message.destroy();

      if (Success) {
        closeCronModal();
        message.success(Message);
        actionRef.current.reload?.();
      }
    });
  }, [id, form, closeCronModal, t]);

  const handleUserPasswordReset = useCallback(() => {
    form.validateFields().then(async values => {
      message.loading(t("extend.dataProcessing"), 0);
      const { Success, Message } = await http.put<any>(`/api/SmUser/ResetPassword/${id}`, {
        PassWord: values.PassWord
      });
      message.destroy();

      if (Success) {
        closeCronModal();
        message.success(Message);
        actionRef.current.reload?.();
      }
    });
  }, [id, form, closeCronModal, t]);

  /**
   * 用户角色功能
   */
  // 设置角色权限
  const SysRolePermissionSet = useCallback((value: string, action: ActionRef, roleRecord: RoleRecord) => {
    actionRef.current = action;
    setId(value);
    setIsCronModalVisible(true);
    setRecord(roleRecord);
  }, []);

  /**
 * 用户管理功能
 */
  // 重置密码
  const UserPasswordReset = useCallback((value: string, action: ActionRef, roleRecord: RoleRecord) => {
    actionRef.current = action;
    setId(value);
    setIsCronModalVisible(true);
    setRecord(roleRecord);
  }, []);

  // 向父组件暴露方法
  useImperativeHandle(
    extendPageRef,
    () => ({
      onSaveAdd,
      ExportModuleSqlScript,
      InitAllTable,
      InitAssignmentTable,
      jobExecute,
      ModifyJobCron,
      SysModuleCopy,
      SysRolePermissionSet,
      UserPasswordReset
    }),
    [
      onSaveAdd,
      ExportModuleSqlScript,
      InitAllTable,
      InitAssignmentTable,
      jobExecute,
      ModifyJobCron,
      SysModuleCopy,
      SysRolePermissionSet,
      UserPasswordReset
    ]
  );

  /**
   * 渲染不同模块的UI
   */
  return (
    <>
      {/* 表目录管理模块 */}
      {moduleCode === MODULE_CODES.TABLE_CATALOG && (
        <Modal
          destroyOnHidden
          title={t("extend.initTable")}
          open={modalVisible}
          maskClosable={false}
          width={1000}
          onCancel={() => setModalVisible(false)}
          footer={null}
        >
          <Row gutter={24} justify="center">
            <Col span={8} />
            <Col span={8}>
              <ComboGrid code="SmTableCatalog1" onChange={(value: string | null) => setTableCode(value)} />
            </Col>
            <Col span={8} />
          </Row>
          <div style={{ height: 10 }} />
          <Row gutter={24} justify="center">
            <Col span={6} />
            <Col span={6} style={{ textAlign: "right" }}>
              <Button type="primary" onClick={InitAllTable}>
                {t("extend.initAllTables")}
              </Button>
            </Col>
            <Col span={6}>
              <Button type="primary" onClick={InitAssignmentTable}>
                {t("extend.initSpecifiedTable")}
              </Button>
            </Col>
            <Col span={6} />
          </Row>
        </Modal>
      )}

      {/* 任务管理模块 */}
      {moduleCode === MODULE_CODES.JOB && (
        <>
          {/* 日志查看模态框 */}
          <Modal
            title={t("extend.currentLog")}
            open={modalVisible}
            width={1200}
            destroyOnHidden
            onOk={closeMainModal}
            onCancel={closeMainModal}
          >
            <div dangerouslySetInnerHTML={{ __html: logContent }} />
          </Modal>

          {/* Cron表达式修改模态框 */}
          <Modal title={t("extend.modifyParams")} open={isCronModalVisible} onOk={handleCronOk} onCancel={closeCronModal}>
            <Form {...FORM_LAYOUT} form={form}>
              <Row gutter={24} justify="center">
                <Col span={24}>
                  <FormItem name="args" label={t("extend.cronExpression")} rules={[{ required: true }]}>
                    <Input placeholder={t("formOption.inputPlaceholder")} />
                  </FormItem>
                </Col>
              </Row>
            </Form>
          </Modal>
        </>
      )}

      {/* 系统模块管理 */}
      {moduleCode === MODULE_CODES.MODULE && (
        <Modal destroyOnHidden title={t("extend.copyModule")} open={isCronModalVisible} onOk={handleModuleCopy} onCancel={closeCronModal}>
          <Form {...FORM_LAYOUT} form={form}>
            <Row gutter={24} justify="center">
              <Col span={24}>
                <FormItem name="ModuleCode" label={t("extend.moduleCode")} rules={[{ required: true }]}>
                  <Input placeholder={t("formOption.inputPlaceholder")} />
                </FormItem>
              </Col>
            </Row>
            <Row gutter={24} justify="center">
              <Col span={24}>
                <FormItem name="ModuleName" label={t("extend.moduleName")} rules={[{ required: true }]}>
                  <Input placeholder={t("formOption.inputPlaceholder")} />
                </FormItem>
              </Col>
            </Row>
          </Form>
        </Modal>
      )}

      {/* 角色管理模块 */}
      {moduleCode === MODULE_CODES.ROLE && (
        <Drawer
          closable
          destroyOnHidden
          title={`${record.RoleName} - ${t("extend.permissionSettings")}`}
          placement="right"
          open={isCronModalVisible}
          size={1000}
          onClose={closeCronModal}
        >
          <PermissionSet id={id} />
        </Drawer>
      )}

      {moduleCode === MODULE_CODES.USER && (
        <Modal
          destroyOnHidden
          title={`${record.UserName} - ${t("extend.resetPassword")}`}
          open={isCronModalVisible}
          onOk={handleUserPasswordReset}
          onCancel={closeCronModal}
        >
          <Form {...FORM_LAYOUT} form={form}>
            <Row gutter={24} justify="center">
              <Col span={24}>
                <FormItem name="PassWord" label={t("extend.newPassword")} rules={[{ required: true, message: t("extend.enterNewPassword") }]}>
                  <Input.Password placeholder={t("extend.enterNewPassword")} />
                </FormItem>
              </Col>
            </Row>
            <Row gutter={24} justify="center">
              <Col span={24}>
                <FormItem
                  name="ConfirmPassWord"
                  label={t("extend.confirmPassword")}
                  dependencies={["PassWord"]}
                  rules={[
                    { required: true, message: t("extend.enterNewPasswordAgain") },
                    ({ getFieldValue }) => ({
                      validator(_, value) {
                        if (!value || getFieldValue("PassWord") === value) {
                          return Promise.resolve();
                        }
                        return Promise.reject(new Error(t("extend.passwordMismatch")));
                      }
                    })
                  ]}
                >
                  <Input.Password placeholder={t("extend.enterNewPasswordAgain")} />
                </FormItem>
              </Col>
            </Row>
          </Form>
        </Modal>
      )}
    </>
  );
};

export default Extend;
