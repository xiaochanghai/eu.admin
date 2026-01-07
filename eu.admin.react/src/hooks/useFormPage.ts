import { useState, useRef, useImperativeHandle, useEffect, useCallback, useMemo } from "react";
import { Form } from "antd";
import { useDispatch, useSelector, RootState } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { setId, setModuleInfo } from "@/redux/modules/module";
import { querySingle, add, update, getModuleInfo } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import { SaveTypeEnum, ModifyType, EditOpenType } from "@/typings";

/**
 * FormPage 通用 Hook
 * 封装表单页面的通用逻辑，包括数据加载、保存、状态管理等
 */

export interface UseFormPageOptions {
  /** 记录 ID */
  Id?: string | null;
  /** 模块代码 */
  moduleCode: string;
  /** 是否只读视图 */
  IsView?: boolean;
  /** 主表 ID（明细表使用） */
  masterId?: string;
  /** FormPage ref */
  formPageRef?: any;
  /** 禁用状态变化回调 */
  onDisabled?: (disabled: boolean) => void;
  /** 重新加载回调 */
  onReload?: () => void;
  /** 自定义查询成功后的处理 */
  onQuerySuccess?: (data: any) => void;
  /** 自定义保存前的数据处理 */
  beforeSave?: (formData: any) => any;
  /** 关闭回调（模态框/抽屉时需要） */
  onClose?: () => void;
  /** 设置表单 ID 回调 */
  setFormPageId?: (id: string) => void;
  /** 自定义 modifyType 计算 */
  computeModifyType?: (params: { modifyType: ModifyType; auditStatus: string; orderStatus: string }) => ModifyType;
}

export const useFormPage = (options: UseFormPageOptions) => {
  const {
    Id,
    moduleCode,
    IsView,
    masterId,
    formPageRef,
    onDisabled,
    onQuerySuccess,
    beforeSave,
    onClose,
    setFormPageId,
    computeModifyType
  } = options;

  const dispatch = useDispatch();
  const [form] = Form.useForm();
  const tableRef = useRef<any>();

  // State 管理
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState<string | null>(Id ?? null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [disabledToolbar, setDisabledToolbar] = useState(true);
  const [auditStatus, setAuditStatus] = useState("");
  const [orderStatus, setOrderStatus] = useState("");

  // Redux
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;

  const { url, isDetail, masterColumn, menuData, openType } = moduleInfo || {};

  // 构建权限按钮映射
  const actionAuthButton: { [key: string]: boolean } = {};
  menuData?.forEach((item: any) => {
    actionAuthButton[item.FunctionCode] = true;
  });

  /**
   * 查询单条数据
   */
  const querySingleData = useCallback(async () => {
    if (!Id && !id) return;

    const { Data, Success } = await querySingle({
      Id: Id ?? id,
      moduleCode,
      url
    });

    if (Success) {
      dispatch(setId({ moduleCode, id: Id ?? id }));
      setAuditStatus(Data.AuditStatus);

      // 只在提供了 computeModifyType 时才设置 orderStatus
      if (computeModifyType) {
        setOrderStatus(Data.OrderStatus || Data.SalesOrderStatus || "");
      }

      // 审核通过的记录禁用编辑
      if (Data.AuditStatus !== ModifyType.Add) {
        setDisabled(true);
        setModifyType(ModifyType.AuditPass);
      }

      // 只读视图模式
      if (IsView) {
        setModifyType(ModifyType.View);
      }

      form.setFieldsValue(Data);

      // 自定义回调
      onQuerySuccess?.(Data);
    }
  }, [Id, id, moduleCode, url, IsView, dispatch, form, onQuerySuccess, computeModifyType]);

  /**
   * 表单提交处理
   */
  const onFinish = useCallback(
    async (values: any, type: SaveTypeEnum = SaveTypeEnum.Save) => {
      message.loading("数据提交中...", 0);

      let formData = {
        ...values,
        moduleCode: moduleCode !== "SM_MODULE_MNG" ? moduleCode : null,
        url,
        ...(id && { Id: id })
      };

      // 明细表添加主表 ID
      if (isDetail && masterColumn) {
        formData[masterColumn] = masterId;
      }

      // 处理 undefined 值为 null
      Object.keys(formData).forEach(key => {
        if (formData[key] === undefined) {
          formData[key] = null;
        }
      });

      // 自定义数据处理
      if (beforeSave) {
        formData = beforeSave(formData);
      }

      const { Data, Success, Message } = id
        ? await update(formData)
        : await add(formData);

      message.destroy();

      if (Success) {
        message.success(Message);
        setDisabledToolbar(true);
        onDisabled?.(false);

        // 如果是模态框或抽屉，触发重新加载
        if (openType === EditOpenType.Modal || openType === EditOpenType.Drawer) {
          options.onReload?.();
        }

        // 保存并新增
        if (type === SaveTypeEnum.SaveAdd) {
          // 根据保存类型处理后续操作
          if (onClose) {
            onClose();
          } else {
            setViewId(null);
            setDisabled(false);  // 保存并新增后，表单应该可编辑
            form.resetFields();
          }
        }
        // 保存后设置 ID
        else if (!id) {
          setFormPageId?.(Data);
          setViewId(Data);
          setModifyType(ModifyType.Edit);
          setAuditStatus("Add");
          // 如果是模态框或抽屉，保存后关闭
          if (onClose && (openType === EditOpenType.Modal || openType === EditOpenType.Drawer)) {
            onClose();
          }
        }
        // 编辑模式保存成功后，如果是模态框或抽屉，关闭
        else if (onClose && (openType === EditOpenType.Modal || openType === EditOpenType.Drawer)) {
          onClose();
        }
      }
    },
    [
      id,
      moduleCode,
      url,
      isDetail,
      masterColumn,
      masterId,
      beforeSave,
      form,
      onDisabled,
      openType,
      options,
      onClose,
      setFormPageId
    ]
  );

  /**
   * 保存
   */
  const onSave = useCallback(() => {
    return form.validateFields().then(onFinish);
  }, [form, onFinish]);

  /**
   * 保存并新增
   */
  const onSaveAdd = useCallback(() => {
    return form
      .validateFields()
      .then(values => onFinish(values, SaveTypeEnum.SaveAdd));
  }, [form, onFinish]);

  /**
   * 表单值变化处理
   */
  const onValuesChange = useCallback(() => {
    onDisabled?.(false);
    setDisabledToolbar(false);
    setDisabled(false);
  }, [onDisabled]);

  /**
   * 初始化模块信息
   */
  useEffect(() => {
    const fetchModuleInfo = async () => {
      const { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };

    if (!moduleInfo) {
      fetchModuleInfo();
    }
  }, [moduleCode, moduleInfo, dispatch]);

  /**
   * 初始化
   */
  useEffect(() => {
    if (!moduleInfo) return;

    if (Id) {
      setModifyType(ModifyType.Edit);
      setViewId(Id);
      querySingleData();
    }

    setIsLoading(false);
    setDisabled(false);
  }, [Id, querySingleData, moduleInfo]);

  /**
   * 暴露方法给父组件
   */
  useImperativeHandle(formPageRef, () => ({
    onSave,
    onSaveAdd,
    reload: querySingleData
  }));

  /**
   * 计算最终的 modifyType
   * 如果提供了 computeModifyType 回调，则使用它计算
   */
  const finalModifyType = useMemo(() => {
    if (computeModifyType) {
      return computeModifyType({ modifyType, auditStatus, orderStatus });
    }
    return modifyType;
  }, [computeModifyType, modifyType, auditStatus, orderStatus]);

  return {
    // State
    form,
    tableRef,
    isLoading,
    disabled,
    id,
    modifyType: finalModifyType,
    disabledToolbar,
    auditStatus,
    moduleInfo,
    actionAuthButton,

    // 方法
    querySingleData,
    onFinish,
    onSave,
    onSaveAdd,
    onValuesChange,

    // Setters（用于自定义扩展）
    setViewId,
    setModifyType,
    setDisabled,
    setDisabledToolbar,
    setAuditStatus
  };
};
