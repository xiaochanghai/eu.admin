import React, { useRef, useState, useEffect } from "react";
import { Drawer, Modal, Button, Space } from "antd";
import { useDispatch, RootState, useSelector } from "@/redux";
import { Skeleton, Icon } from "@/components";
import SmProTable from "@/components/ProTable";
import type { ColumnCustomizerMap } from "@/components/ProTable/columnCustomizers";
import { ModuleInfo } from "@/api/interface/index";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo, setId } from "@/redux/modules/module";
import Extend from "@/views/system/common/components/Extend";
import { FormPage } from "./FormPage";
import { ViewType, EditOpenType, ActionType } from "@/typings";
import { useTranslation } from "react-i18next";
import { getModuleName } from "@/components/ProTable/utils";

/**
 * 模块表格组件接口定义
 */
interface TableListProps {
  moduleCode: string; // 模块代码
  masterId?: string | null; // 主表ID
  columnCustomizers?: ColumnCustomizerMap; // 按 dataIndex 定制或替换动态列
  customConditions?: string; // 自定义SQL WHERE条件
  changePage?: (page: ViewType, id?: any, isView?: boolean) => void; // 页面切换回调
  IsView?: boolean | null; // 是否为查看模式
  DynamicFormPage?: React.ComponentType<any>; // 动态表单页面组件
  [key: string]: any; // 其他属性
  tableActionRef?: React.RefObject<ActionType | undefined>; // 视图操作引用
  extendAction?: any;
}

/**
 * 模块表格列表组件
 * 用于展示模块数据并处理表单操作（新增、编辑、查看）
 * @param props 组件属性
 */
export const TableList: React.FC<TableListProps> = props => {
  const dispatch = useDispatch();
  const { t } = useTranslation();
  const language = useSelector((state: RootState) => state.global.language);

  // 状态管理
  const [drawerOpen, setDrawerOpen] = useState(false); // 抽屉是否打开
  const [modalVisible, setModalVisible] = useState(false); // 模态框是否可见
  const [isView, setIsView] = useState<boolean | null | undefined>(false); // 是否为查看模式
  const [disabled, setDisabled] = useState(true); // 表单是否禁用

  // 从props中解构属性
  const { moduleCode, masterId, changePage, IsView, DynamicFormPage, extendAction, tableActionRef } = props;

  // 从Redux获取模块信息和ID
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const ids = useSelector((state: RootState) => state.module.ids);
  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  const id = ids[moduleCode];

  // 引用管理
  const formRef = React.createRef<any>();
  const tableRef = useRef<ActionType>();
  const formPageRef = React.createRef<any>();
  const extendPageRef = React.createRef<any>();

  /**
   * 初始化模块信息
   */
  useEffect(() => {
    const fetchModuleInfo = async () => {
      const { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };

    // 如果模块信息不存在，则获取
    if (!moduleInfo) fetchModuleInfo();
    setIsView(IsView);
  }, []);

  /**
   * 关闭表单抽屉或模态框
   */
  const onClose = () => {
    setDrawerOpen(false);
    setDisabled(true);
    setModalVisible(false);
  };

  const onReload = () => {
    (tableActionRef ?? tableRef).current?.reload();
  };
  /**
   * 渲染表单组件
   * @returns 表单组件
   */
  const renderFormComponent = () => {
    return (
      <>
        {DynamicFormPage ? (
          <DynamicFormPage
            moduleCode={moduleCode}
            Id={id}
            masterId={masterId}
            IsView={isView}
            onReload={onReload}
            onClose={onClose}
            formPageRef={formPageRef}
            onDisabled={(value: boolean) => setDisabled(value)}
          />
        ) : (
          <FormPage
            moduleCode={moduleCode}
            Id={id}
            masterId={masterId}
            IsView={isView}
            onReload={onReload}
            onClose={onClose}
            formPageRef={formPageRef}
            onDisabled={(value: boolean) => setDisabled(value)}
          />
        )}
      </>
    );
  };

  // 动态操作对象，用于存储模块配置的操作函数
  let action = {};
  if (extendAction) action = { ...action, ...extendAction };

  // 处理模块菜单、操作和隐藏菜单的JavaScript函数
  if (moduleInfo) {
    // 处理菜单数据
    if (moduleInfo.menuData?.length > 0) {
      moduleInfo.menuData.forEach((item: any) => {
        try {
          eval(item.FunctionJs);
        } catch (error) {
          console.error(`菜单函数执行错误: ${item.FunctionJs}`, error);
        }
      });
    }

    // 处理操作数据
    if (moduleInfo.actionData?.length > 0) {
      moduleInfo.actionData.forEach((item: any) => {
        try {
          eval(item.FunctionJs);
        } catch (error) {
          console.error(`操作函数执行错误: ${item.FunctionJs}`, error);
        }
      });
    }

    // 处理隐藏菜单
    if (moduleInfo.hideMenu?.length > 0) {
      moduleInfo.hideMenu.forEach((item: any) => {
        try {
          eval(item.FunctionJs);
        } catch (error) {
          console.error(`隐藏菜单函数执行错误: ${item.FunctionJs}`, error);
        }
      });
    }
  }

  /**
   * 处理编辑操作
   * @param recordId 记录ID
   * @param viewMode 是否为查看模式
   */
  const handleEdit = (recordId: string | null, viewMode?: boolean) => {
    dispatch(setId({ moduleCode, id: recordId }));

    if (moduleInfo.openType === EditOpenType.Modal) {
      setModalVisible(true);
      setIsView(viewMode);
    } else if (moduleInfo.openType === EditOpenType.Drawer) {
      setDrawerOpen(true);
      setIsView(viewMode);
    } else if (changePage) {
      changePage(ViewType.PAGE, recordId, viewMode);
    }
  };
  const buttonDisable = isView === true ? isView : disabled;
  return (
    <>
      {moduleInfo && moduleInfo.Success === true ? (
        <>
          <SmProTable
            // moduleCode={moduleCode}
            moduleInfo={moduleInfo}
            actionRef={tableActionRef ?? tableRef}
            formRef={formRef}
            IsView={IsView}
            masterId={masterId}
            onEdit={handleEdit}
            {...action}
            {...props}
          />

          {/* 模态框表单 */}
          {moduleInfo.openType === EditOpenType.Modal && (
            <Modal
              destroyOnHidden
              title={`${getModuleName(moduleInfo, language)}->${id ? t("formOption.edit") : t("formOption.add")}`}
              open={modalVisible}
              width={moduleInfo.formPageWidth}
              footer={null}
              onCancel={() => setModalVisible(false)}
            >
              {modalVisible ? renderFormComponent() : null}
            </Modal>
          )}

          {/* 抽屉表单 */}
          {moduleInfo.openType === EditOpenType.Drawer && (
            <Drawer
              title={`${getModuleName(moduleInfo, language)}->${id ? t("formOption.edit") : t("formOption.add")}`}
              size={moduleInfo.formPageWidth}
              onClose={onClose}
              open={drawerOpen}
              extra={
                <Space>
                  <Button
                    key="save"
                    disabled={buttonDisable}
                    type="primary"
                    block={true}
                    onClick={() => formPageRef.current?.onSave()}
                  >
                    {t("formOption.save")}
                  </Button>
                  <Button key="saveAndAdd" disabled={buttonDisable} block={true} onClick={() => formPageRef.current?.onSaveAdd()}>
                    {t("formOption.saveAndAdd")}
                  </Button>
                  <Button key="back" type="text" block={true} onClick={onClose}>
                    <Icon name="RollbackOutlined" />
                  </Button>
                </Space>
              }
            >
              {drawerOpen && renderFormComponent()}
            </Drawer>
          )}
        </>
      ) : (
        <Skeleton type="table" />
      )}

      {/* 扩展功能组件 */}
      <Extend moduleCode={moduleCode} extendPageRef={extendPageRef} />
    </>
  );
};
