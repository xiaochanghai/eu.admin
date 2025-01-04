import React, { useRef, useState, useEffect } from "react";
import { Loading } from "@/components/Loading/index";
import SmProTable from "@/components/ProTable";
import { useDispatch, RootState, useSelector } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { Drawer, Modal, Button, Space } from "antd";
import { Icon } from "@/components/Icon";
import FormPage from "./FormPage";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo, setId } from "@/redux/modules/module";
import Extend from "./Extend";
import type { ActionType } from "@ant-design/pro-components";

const MenuMange: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [isVIew, setIsView] = useState(false);
  const [disabled, setDisabled] = useState(true);
  const { moduleCode, masterId, changePage, IsView, DynamicFormPage } = props;

  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const ids = useSelector((state: RootState) => state.module.ids);
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  let id = ids[moduleCode];
  const formRef = React.createRef<any>();
  const tableRef = useRef<ActionType>();
  const formPageRef = React.createRef<any>();
  const extendPageRef = React.createRef<any>();

  useEffect(() => {
    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) getModuleInfo1();
    setIsView(IsView);
  }, []);

  const onClose = () => {
    setDrawerOpen(false);
    setDisabled(true);
    setModalVisible(false);
    // dispatch({
    //   type: "smcommon/setId",
    //   payload: { moduleCode, id: null }
    // });
  };
  // const onClose = () => {
  //   this.setState({ drawerOpen: false })
  // };

  const component = () => {
    return (
      <>
        {DynamicFormPage ? (
          <DynamicFormPage
            moduleCode={moduleCode}
            Id={id}
            masterId={masterId}
            IsView={isVIew}
            onReload={() => tableRef.current?.reload()}
            onClose={onClose}
            formPageRef={formPageRef}
            onDisabled={(value: any) => setDisabled(value)}
          />
        ) : (
          <FormPage
            moduleCode={moduleCode}
            Id={id}
            masterId={masterId}
            IsView={isVIew}
            onReload={() => tableRef.current?.reload()}
            onClose={onClose}
            formPageRef={formPageRef}
            onDisabled={(value: any) => setDisabled(value)}
          />
        )}
      </>
    );
  };
  let action = {};
  if (moduleInfo && moduleInfo.menuData.length > 0)
    moduleInfo.menuData.map((item: any) => {
      if (item.FunctionJs) eval(item.FunctionJs);
    });

  if (moduleInfo && moduleInfo.actionData.length > 0)
    moduleInfo.actionData.map((item: any) => {
      if (item.FunctionJs) eval(item.FunctionJs);
    });

  if (moduleInfo && moduleInfo.hideMenu.length > 0)
    moduleInfo.hideMenu.map((item: any) => {
      if (item.FunctionJs) eval(item.FunctionJs);
    });
  //#endregion
  return (
    <>
      {moduleInfo && moduleInfo.Success === true ? (
        <SmProTable
          moduleCode={moduleCode}
          moduleInfo={moduleInfo}
          actionRef={tableRef}
          formRef={formRef}
          IsView={IsView}
          masterId={masterId}
          onEdit={(id: any, isVIew: any) => {
            dispatch(setId({ moduleCode, id }));

            if (moduleInfo.openType === "Modal") {
              setModalVisible(true);
              setIsView(isVIew);
            } else if (moduleInfo.openType === "Drawer") {
              setDrawerOpen(true);
              setIsView(isVIew);
            } else changePage("FormPage", id, isVIew);
          }}
          {...action}
          {...props}
        />
      ) : (
        <Loading />
      )}
      {moduleInfo && moduleInfo.Success === true && moduleInfo.openType === "Modal" ? (
        <Modal
          destroyOnClose
          title={moduleInfo.moduleName + (id ? "->编辑" : "->新增")}
          open={modalVisible}
          //  maskClosable={false}
          width={moduleInfo.formPageWidth}
          footer={null}
          onCancel={() => {
            setModalVisible(false);
            // dispatch({
            //   type: "smcommon/setId",
            //   payload: { moduleCode, id: null }
            // });
          }}
        >
          {modalVisible ? component() : null}
        </Modal>
      ) : null}
      {moduleInfo && moduleInfo.Success === true && moduleInfo.openType === "Drawer" ? (
        <Drawer
          title={moduleInfo.moduleName + (id ? "->编辑" : "->新增")}
          width={moduleInfo.formPageWidth}
          onClose={onClose}
          open={drawerOpen}
          extra={
            <Space>
              <Button
                key="submit"
                disabled={isVIew ?? disabled}
                type="primary"
                block={true}
                onClick={() => formPageRef.current.onSave()}
              >
                <a title="保存">保存</a>
              </Button>
              <Button key="submit1" disabled={isVIew ?? disabled} block={true} onClick={() => formPageRef.current.onSaveAdd()}>
                保存并新建
              </Button>

              <Button key="back" type="text" block={true} onClick={() => onClose()}>
                <Icon name="RollbackOutlined" />
              </Button>
            </Space>
          }
        >
          {drawerOpen ? component() : null}
        </Drawer>
      ) : null}
      <Extend moduleCode={moduleCode} extendPageRef={extendPageRef} />
    </>
  );
};

export default MenuMange;
