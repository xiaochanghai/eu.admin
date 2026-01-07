import React, { ReactNode } from "react";
import { Flex, Form, Card, Tabs } from "antd";
import { Loading, FormToolbar, EditableProTable, Attachment, Element, Skeleton } from "@/components";
import { useFormPage, UseFormPageOptions } from "@/hooks/useFormPage";
import { ViewType, ModifyType, EditOpenType } from "@/typings";
import { STANDARD_FORM_LAYOUT, MODAL_FORM_LAYOUT } from "@/config";
import { TableList } from "@/components/Elements/TableList";

/**
 * BaseFormPage Props
 */
export interface BaseFormPageProps extends UseFormPageOptions {
  /** 页面切换函数（可选，标准页面时需要） */
  changePage?: (viewType: ViewType, id: string, isView?: any) => void;

  /** 自定义渲染表单字段（可选） */
  renderFormFields?: (params: {
    formColumns: any[];
    form: any;
    disabled: boolean;
    moduleInfo: any;
    modifyType: ModifyType;
  }) => ReactNode;

  /** 自定义渲染明细表（可选） */
  renderDetailTable?: (params: { tableRef: any; masterId: string | null; modifyType: ModifyType; moduleInfo: any }) => ReactNode;

  /** 明细表模块代码 */
  detailModuleCode?: string;

  /** 明细表添加按钮回调 */
  onDetailAdd?: () => void;

  /** 明细表编辑回调 */
  onDetailEdit?: (originData: any, data: any) => any;

  /** 是否显示附件 */
  showAttachment?: boolean;

  /** FormToolbar 额外操作按钮 */
  expendAction?: () => React.ReactNode;

  /** 自定义 modifyType 计算 */
  computeModifyType?: (params: { modifyType: ModifyType; auditStatus: string; orderStatus: string }) => ModifyType;

  /** 明细表标题 */
  detailTableTitle?: string;

  /** 关闭回调（模态框/抽屉时需要） */
  onClose?: () => void;

  /** 设置表单 ID 回调 */
  setFormPageId?: (id: string) => void;

  /** 是否显示工具栏（模态框/抽屉模式） */
  displayToolBar?: boolean;

  /** 自定义子项（替代默认的子表 Tabs） */
  childrenItems?: any[];
}

/**
 * 默认渲染表单字段
 */
const defaultRenderFormFields = ({
  formColumns,
  disabled,
  modifyType
}: {
  formColumns: any[];
  disabled: boolean;
  modifyType: ModifyType;
}) => {
  const visibleColumns = formColumns?.filter((f: any) => f.HideInForm === false);

  // 空表单提示
  if (!visibleColumns || visibleColumns.length === 0) {
    return (
      <Flex wrap="wrap">
        <div className="main-tooltip">请选择进行系统表单配置</div>
      </Flex>
    );
  }

  return (
    <Flex wrap="wrap">
      {visibleColumns.map((item: any, index: number) => {
        const width = `${item?.GridSpan ?? 50}%`;
        return (
          <div key={item.FieldName || index} style={{ width }}>
            <Element
              field={item}
              disabled={disabled}
              modifyType={modifyType}
              style={visibleColumns.length - 1 === index ? { marginBottom: 0 } : undefined}
            />
          </div>
        );
      })}
    </Flex>
  );
};

/**
 * 渲染表单组件（导出供外部使用）
 * 根据模块配置的表单列生成对应的表单项
 * @deprecated 建议使用 BaseFormPage 的 renderFormFields 回调
 */
export const renderFormComponent = (formColumns: any[], disabled: boolean, modifyType: ModifyType = ModifyType.Add) => {
  return defaultRenderFormFields({ formColumns, disabled, modifyType });
};

/**
 * BaseFormPage 通用表单页面组件
 * 封装表单页面的通用 UI 结构和逻辑
 * 支持标准页面、模态框、抽屉三种模式
 */
const BaseFormPage: React.FC<BaseFormPageProps> = props => {
  const {
    changePage,
    renderFormFields = defaultRenderFormFields,
    renderDetailTable,
    detailModuleCode,
    onDetailAdd,
    onDetailEdit,
    showAttachment = false,
    expendAction,
    computeModifyType,
    detailTableTitle = "明细信息",
    onClose,
    setFormPageId,
    displayToolBar = false,
    childrenItems,
    ...hookOptions
  } = props;

  // 使用通用 Hook
  const {
    form,
    tableRef,
    isLoading,
    disabled,
    id,
    modifyType,
    disabledToolbar,
    auditStatus,
    moduleInfo,
    querySingleData,
    onFinish,
    onSaveAdd,
    onValuesChange
  } = useFormPage({ ...hookOptions, onClose, setFormPageId, computeModifyType });

  if (!moduleInfo) {
    return <Loading />;
  }

  const { formColumns, openType, children } = moduleInfo;

  // 计算是否禁用表单
  const isFormDisabled = hookOptions.IsView === true ? true : disabled === true ? true : disabledToolbar;

  // 判断是否为模态框或抽屉模式
  const isModalOrDrawer = openType === EditOpenType.Modal || openType === EditOpenType.Drawer;

  // 准备子表选项卡
  const tabItems: any = React.useMemo(() => {
    // 如果有自定义子项，直接使用
    if (childrenItems) {
      return childrenItems;
    }

    // 如果没有子表配置，返回空数组
    if (!children || children.length === 0) return [];

    // 根据子表配置生成选项卡
    return children.map((childModule: any, index: number) => ({
      key: String(index),
      label: childModule.ModuleName,
      children: (
        <TableList moduleCode={childModule.ModuleCode} masterId={id} IsView={hookOptions.IsView} modifyType={modifyType} />
      )
    }));
  }, [childrenItems, children, id, hookOptions.IsView, modifyType]);

  // 表单内容渲染
  const renderFormContent = () => (
    <>
      {renderFormFields({
        formColumns: formColumns || [],
        form,
        disabled,
        moduleInfo,
        modifyType: modifyType
      })}
    </>
  );

  // 工具栏渲染
  const renderToolbar = () => (
    <FormToolbar
      moduleInfo={moduleInfo}
      disabled={isFormDisabled}
      onFinishAdd={onSaveAdd}
      modifyType={modifyType}
      auditStatus={auditStatus}
      masterId={id}
      onBack={
        changePage
          ? () => {
              hookOptions.onReload?.();
              changePage(ViewType.INDEX, "", false);
            }
          : undefined
      }
      onReload={() => querySingleData()}
      expendAction={expendAction}
    />
  );

  return (
    <>
      {moduleInfo?.Success === false && <Skeleton type="form" />}
      {moduleInfo?.Success === true && (
        <>
          {/* 标准页面表单 */}
          {!isModalOrDrawer && (
            <Form {...STANDARD_FORM_LAYOUT} labelWrap onFinish={onFinish} onValuesChange={onValuesChange} form={form}>
              {isLoading ? (
                <Skeleton type="form" />
              ) : (
                <>
                  {renderToolbar()}
                  <Card size="small" variant="borderless">
                    {renderFormContent()}
                  </Card>
                </>
              )}
            </Form>
          )}

          {/* 模态框/抽屉表单 */}
          {isModalOrDrawer && (
            <div style={{ marginTop: displayToolBar ? 0 : 20, marginBottom: displayToolBar ? 0 : 20 }}>
              <Form {...MODAL_FORM_LAYOUT} labelWrap onFinish={onFinish} onValuesChange={onValuesChange} form={form}>
                {displayToolBar && <div style={{ paddingBottom: 10 }}>{renderToolbar()}</div>}
                {isLoading ? <Skeleton type="form" /> : renderFormContent()}
              </Form>
            </div>
          )}

          {/* 明细表 */}
          {!isModalOrDrawer && detailModuleCode && (
            <>
              <div style={{ height: 20 }}></div>
              <Card title={detailTableTitle} variant="borderless">
                {renderDetailTable ? (
                  renderDetailTable({
                    tableRef,
                    masterId: id,
                    modifyType: modifyType,
                    moduleInfo
                  })
                ) : (
                  <EditableProTable
                    moduleCode={detailModuleCode}
                    tableRef={tableRef}
                    modifyType={modifyType}
                    masterId={id}
                    addCallBack={onDetailAdd}
                    editableCallBack={onDetailEdit}
                  />
                )}
              </Card>
            </>
          )}

          {/* 附件 */}
          {!isModalOrDrawer && showAttachment && (
            <>
              <div style={{ height: 20 }}></div>
              <Card title="附件" variant="borderless">
                <Attachment Id={id} IsView={modifyType === ModifyType.Edit ? false : true} />
              </Card>
            </>
          )}

          {/* 子表选项卡 */}
          {!isModalOrDrawer && tabItems && tabItems.length > 0 && (
            <Card size="small" variant="borderless" className="mt-10">
              {isLoading ? <Skeleton type="table" /> : <Tabs items={tabItems} />}
            </Card>
          )}
        </>
      )}
    </>
  );
};

export default BaseFormPage;
