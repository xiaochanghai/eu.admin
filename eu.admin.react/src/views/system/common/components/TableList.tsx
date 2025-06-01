import React, { useRef, useState, useEffect } from "react";
import { Drawer, Modal, Button, Space } from "antd";
import type { ActionType } from "@ant-design/pro-components";
import { useDispatch, RootState, useSelector } from "@/redux";
import { Loading, SmProTable, Icon } from "@/components";
import { ModuleInfo } from "@/api/interface/index";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo, setId } from "@/redux/modules/module";
import Extend from "./Extend";
import FormPage from "./FormPage";
import { ViewType } from "@/typings";

/**
 * 模块表格组件接口定义
 */
interface TableListProps {
  moduleCode: string; // 模块代码
  masterId?: string | null; // 主表ID
  changePage?: (page: ViewType, id?: any, isView?: boolean) => void; // 页面切换回调
  IsView?: boolean | null; // 是否为查看模式
  DynamicFormPage?: React.ComponentType<any>; // 动态表单页面组件
  [key: string]: any; // 其他属性
}

/**
 * 模块表格列表组件
 * 用于展示模块数据并处理表单操作（新增、编辑、查看）
 * @param props 组件属性
 */
const TableList: React.FC<TableListProps> = props => {
  const dispatch = useDispatch();
  // 状态管理
  const [drawerOpen, setDrawerOpen] = useState(false); // 抽屉是否打开
  const [modalVisible, setModalVisible] = useState(false); // 模态框是否可见
  const [isView, setIsView] = useState<boolean | null | undefined>(false); // 是否为查看模式
  const [disabled, setDisabled] = useState(true); // 表单是否禁用

  // 从props中解构属性
  const { moduleCode, masterId, changePage, IsView, DynamicFormPage } = props;

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
    // 注释掉的代码保留，可能后续需要恢复
    // dispatch({
    //   type: "smcommon/setId",
    //   payload: { moduleCode, id: null }
    // });
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
            onReload={() => tableRef.current?.reload()}
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
            onReload={() => tableRef.current?.reload()}
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
  const handleEdit = (recordId: string, viewMode: boolean) => {
    dispatch(setId({ moduleCode, id: recordId }));

    if (moduleInfo.openType === "Modal") {
      setModalVisible(true);
      setIsView(viewMode);
    } else if (moduleInfo.openType === "Drawer") {
      setDrawerOpen(true);
      setIsView(viewMode);
    } else if (changePage) {
      changePage(ViewType.PAGE, recordId, viewMode);
    }
  };
  return (
    <>
      {moduleInfo && moduleInfo.Success === true ? (
        <>
          <SmProTable
            // moduleCode={moduleCode}
            moduleInfo={moduleInfo}
            actionRef={tableRef}
            formRef={formRef}
            IsView={IsView}
            masterId={masterId}
            onEdit={handleEdit}
            {...action}
            {...props}
          />

          {/* 模态框表单 */}
          {moduleInfo.openType === "Modal" && (
            <Modal
              destroyOnClose
              title={`${moduleInfo.moduleName}${id ? "->编辑" : "->新增"}`}
              open={modalVisible}
              width={moduleInfo.formPageWidth}
              footer={null}
              onCancel={() => setModalVisible(false)}
            >
              {modalVisible ? renderFormComponent() : null}
            </Modal>
          )}

          {/* 抽屉表单 */}
          {moduleInfo.openType === "Drawer" && (
            <Drawer
              title={`${moduleInfo.moduleName}${id ? "->编辑" : "->新增"}`}
              width={moduleInfo.formPageWidth}
              onClose={onClose}
              open={drawerOpen}
              extra={
                <Space>
                  <Button
                    key="submit"
                    disabled={isView ?? disabled}
                    type="primary"
                    block={true}
                    onClick={() => formPageRef.current?.onSave()}
                  >
                    保存
                  </Button>
                  <Button
                    key="submit1"
                    disabled={isView ?? disabled}
                    block={true}
                    onClick={() => formPageRef.current?.onSaveAdd()}
                  >
                    保存并新建
                  </Button>
                  <Button key="back" type="text" block={true} onClick={onClose}>
                    <Icon name="RollbackOutlined" />
                  </Button>
                </Space>
              }
            >
              {drawerOpen ? renderFormComponent() : null}
            </Drawer>
          )}
        </>
      ) : (
        <Loading />
      )}

      {/* 扩展功能组件 */}
      <Extend moduleCode={moduleCode} extendPageRef={extendPageRef} />
    </>
  );
};

export default TableList;
