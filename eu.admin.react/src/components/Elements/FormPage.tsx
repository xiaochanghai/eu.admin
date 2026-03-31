import React, { useEffect, useImperativeHandle, useState, useCallback, useMemo } from "react";
import { Card, Form, Flex, Tabs } from "antd";
import { TableList } from "./TableList";
import { querySingle, add, update, getModuleInfo } from "@/api/modules/module";
import { useDispatch, RootState, useSelector } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { message } from "@/hooks/useMessage";
import { FormToolbar, Element, Skeleton } from "@/components";
import { setModuleInfo, setId } from "@/redux/modules/module";
import { ViewType, EditOpenType, SaveType, SaveTypeEnum, ModifyType } from "@/typings";
import { STANDARD_FORM_LAYOUT, MODAL_FORM_LAYOUT } from "@/config";

/**
 * 表单页面组件的 Props 接口
 */
export interface FormPageProps {
  Id?: string;
  changePage?: (viewType: ViewType, id: string, isView: any) => void;
  moduleCode: string;
  // formPageRef?: React.RefObject<FormPageRef>;
  formPageRef?: any;
  onClose?: () => void;
  IsView?: boolean | null | undefined;
  onDisabled?: (disabled: boolean) => void;
  masterId?: string | null;
  onReload?: () => void;
  childrenItems?: any;
  setFormPageId?: (id: string) => void;
  displayToolBar?: boolean;
}

/**
 * 渲染表单组件
 * 根据模块配置的表单列生成对应的表单项
 */
export const renderFormComponent = (formColumns: any[], disabled: boolean, modifyType: ModifyType) => {
  const visibleColumns = formColumns?.filter((f: any) => {
    if (f.HideInForm !== false) return false;
    if (modifyType === ModifyType.Add && f.CreateHide === true) return false;
    return true;
  });

  if (!visibleColumns || visibleColumns.length === 0)
    return (
      <Flex wrap="wrap">
        <div className="main-tooltip">请选择进行系统表单配置</div>
      </Flex>
    );

  return (
    <Flex wrap="wrap">
      {visibleColumns.map((item: any, index: number) => (
        <div style={{ width: `${item?.GridSpan ?? 50}%` }} key={item.FieldName || index}>
          <Element
            field={item}
            disabled={disabled}
            modifyType={modifyType}
            style={visibleColumns.length - 1 === index ? { marginBottom: 0 } : undefined}
          />
        </div>
      ))}
    </Flex>
  );
};
/**
 * 表单页面组件
 * 用于处理模块的表单数据展示、编辑、保存等操作
 */
export const FormPage: React.FC<FormPageProps> = props => {
  // Redux相关
  const dispatch = useDispatch();
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);

  // 状态定义
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [id, setViewId] = useState<string | null>(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [form] = Form.useForm();

  // 从props中获取参数
  const {
    Id,
    changePage,
    moduleCode,
    formPageRef,
    onClose,
    IsView,
    onDisabled,
    masterId,
    onReload,
    childrenItems,
    setFormPageId,
    displayToolBar = false
  } = props;

  // 获取当前模块信息
  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  const { formColumns, openType, children, url, isDetail, masterColumn } = moduleInfo || {};

  /**
   * 查询单条数据
   * 根据ID获取记录详情并填充表单
   */
  const querySingleData = useCallback(async () => {
    if (!Id || !url) return;

    const { Data, Success } = await querySingle({ Id, moduleCode, url });
    if (Success) {
      dispatch(setId({ moduleCode, id: Id }));
      setIsLoading(false);
      form.setFieldsValue(Data);
    }
  }, [Id, moduleCode, url, dispatch, form]);

  /**
   * 初始化模块信息
   */
  useEffect(() => {
    const fetchModuleInfo = async () => {
      const { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };

    if (!moduleInfo) fetchModuleInfo();
  }, [moduleCode, moduleInfo, dispatch]);

  /**
   * 初始化表单
   * 根据是否有ID决定是新增还是编辑模式
   */
  useEffect(() => {
    if (!moduleInfo) return;

    setDisabled(false);
    if (Id) {
      setViewId(Id);
      setModifyType(ModifyType.Edit);
      querySingleData();

      if (IsView) setDisabled(true);
    } else setIsLoading(false);
  }, [Id, IsView, moduleInfo, querySingleData]);

  /**
   * 准备提交数据
   */
  const prepareSubmitData = useCallback(
    (data: Record<string, any>) => {
      const formData: Record<string, any> = {
        ...data,
        url,
        ModuleCode: moduleCode !== "SM_MODULE_MNG" ? moduleCode : null,
        ...(id && { Id: id })
      };

      // 如果是明细表，添加主表关联
      if (isDetail && masterId) formData[masterColumn] = masterId;

      // 将undefined转换为null
      Object.keys(formData).forEach(key => {
        if (formData[key] === undefined) formData[key] = null;
      });

      return formData;
    },
    [url, moduleCode, id, isDetail, masterId, masterColumn]
  );

  /**
   * 表单提交处理
   * @param data 表单数据
   * @param type 保存类型（Save/SaveAdd）
   */
  const onFinish = useCallback(
    async (data: Record<string, any>, type: SaveType = SaveTypeEnum.Save) => {
      onDisabled?.(true);
      message.loading("数据提交中...", 0);

      const formData = prepareSubmitData(data);
      const { Data, Success, Message } = id ? await update(formData) : await add(formData);

      message.destroy();

      if (Success) {
        onDisabled?.(false);
        message.success(Message);

        // 如果是模态框或抽屉，则重新加载数据
        if (openType === EditOpenType.Modal || openType === EditOpenType.Drawer) onReload?.();

        // 根据保存类型处理后续操作
        if (type !== SaveTypeEnum.SaveAdd && onClose) onClose();
        else if (type === SaveTypeEnum.SaveAdd) {
          // 保存并新增：重置表单
          setViewId(null);
          setDisabled(false);
          form.resetFields();
        } else if (!id) {
          // 新增成功：更新ID
          setFormPageId?.(Data);
          setViewId(Data);
        }
      }
    },
    [onDisabled, prepareSubmitData, id, openType, onReload, onClose, setFormPageId, form]
  );

  /**
   * 保存表单
   */
  const onSave = useCallback(() => form.validateFields().then(onFinish), [form, onFinish]);

  /**
   * 保存并新增
   */
  const onSaveAdd = useCallback(
    () => form.validateFields().then(values => onFinish(values, SaveTypeEnum.SaveAdd)),
    [form, onFinish]
  );

  /**
   * 表单值变化处理
   */
  const onValuesChange = useCallback(() => {
    onDisabled?.(false);
    setDisabledToolbar(false);
    setDisabled(false);
  }, [onDisabled]);

  /**
   * 返回按钮处理
   */
  const handleBack = useCallback(() => {
    onReload?.();
    changePage?.(ViewType.INDEX, "", false);
  }, [onReload, changePage]);

  /**
   * 向父组件暴露方法
   */
  useImperativeHandle(formPageRef, () => ({
    onSave,
    onSaveAdd
  }));

  /**
   * 准备子表选项卡
   */
  const tabItems: any = useMemo(() => {
    // 如果有自定义子项，直接使用
    if (childrenItems) {
      return childrenItems;
    }

    // 如果没有子表配置，返回空数组
    if (!moduleInfo || !children || children.length === 0) return [];

    // 根据子表配置生成选项卡
    return children.map((childModule: any, index: number) => ({
      key: String(index),
      label: childModule.ModuleName,
      children: <TableList moduleCode={childModule.ModuleCode} masterId={id} IsView={IsView} modifyType={modifyType} />
    }));
  }, [childrenItems, moduleInfo, children, id, IsView, modifyType]);

  /**
   * 计算是否需要禁用表单
   */
  const isFormDisabled = IsView || disabled || disabledToolbar;

  // 判断当前是否为模态框或抽屉模式
  const isModalOrDrawer = openType === EditOpenType.Modal || openType === EditOpenType.Drawer;

  return (
    <>
      {moduleInfo?.Success === true ? (
        <>
          {/* 标准页面表单 */}
          {!isModalOrDrawer && (
            <Form {...STANDARD_FORM_LAYOUT} labelWrap onFinish={onFinish} onValuesChange={onValuesChange} form={form}>
              {isLoading ? (
                <Skeleton type="form" />
              ) : (
                <>
                  <FormToolbar
                    moduleInfo={moduleInfo}
                    disabled={isFormDisabled}
                    onFinishAdd={onSaveAdd}
                    modifyType={modifyType}
                    onBack={handleBack}
                  />
                  <Card size="small" variant="borderless">
                    {renderFormComponent(formColumns, disabled, modifyType)}
                  </Card>
                </>
              )}
            </Form>
          )}

          {/* 模态框/抽屉表单 */}
          {isModalOrDrawer && (
            <div style={{ marginTop: displayToolBar ? 0 : 20, marginBottom: displayToolBar ? 0 : 20 }}>
              <Form {...MODAL_FORM_LAYOUT} labelWrap onFinish={onFinish} onValuesChange={onValuesChange} form={form}>
                {displayToolBar && (
                  <div style={{ paddingBottom: 10 }}>
                    <FormToolbar
                      moduleInfo={moduleInfo}
                      disabled={isFormDisabled}
                      onFinishAdd={onSaveAdd}
                      modifyType={modifyType}
                    />
                  </div>
                )}
                {isLoading ? <Skeleton type="form" /> : renderFormComponent(formColumns, disabled, modifyType)}
              </Form>
            </div>
          )}

          {/* 子表选项卡 */}
          {tabItems && tabItems.length > 0 && (
            <Card size="small" variant="borderless" className="mt-10">
              {isLoading ? <Skeleton type="table" /> : <Tabs items={tabItems} />}
            </Card>
          )}
        </>
      ) : (
        <Skeleton type="form" />
      )}
    </>
  );
};
