import React, { useEffect, useImperativeHandle, useState, useCallback } from "react";
import { Form, Flex } from "antd";
import { Loading, Layout } from "@/components";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { message } from "@/hooks/useMessage";
import { RootState, useSelector, useDispatch } from "@/redux";
import { EditOpenType } from "@/typings";

/**
 * 表单列定义接口
 */
interface FormColumn {
  HideInForm: boolean;
  GridSpan?: number;
  [key: string]: any;
}

/**
 * 表单页面属性接口
 */
interface FormPageProps {
  /** 记录ID，编辑模式下必须提供 */
  Id?: string | number | null;
  /** 模块代码 */
  moduleCode: string;
  /** 表单页面引用，用于父组件调用表单方法 */
  formPageRef: React.RefObject<FormPageRefType>;
  /** 关闭回调函数 */
  onClose?: () => void;
  /** 是否为查看模式 */
  IsView?: boolean;
  /** 禁用状态变更回调 */
  onDisabled?: (disabled: boolean) => void;
  /** 主表ID，用于明细表关联 */
  masterId?: string | number;
  /** 重新加载数据回调 */
  onReload?: () => void;
  /** 设置表单页面ID回调 */
  setFormPageId?: (id: string | number) => void;
}

/**
 * 表单页面引用类型接口
 */
interface FormPageRefType {
  /** 保存表单数据 */
  onSave: () => void;
  /** 保存并新增表单数据 */
  onSaveAdd: () => void;
}

/**
 * 表单页面组件
 *
 * 用于动态渲染模块表单，支持新增、编辑和查看模式
 * 集成了与后端API的交互，包括数据获取、提交和验证
 *
 * @param props 组件属性
 */
const FormPage: React.FC<FormPageProps> = React.memo(props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState<string | number | null>(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [form] = Form.useForm();
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);

  const { Id, moduleCode, formPageRef, onClose, IsView, onDisabled, masterId, onReload, setFormPageId } = props;

  // 获取当前模块信息
  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  const { formColumns, openType, url, isDetail, masterColumn } = moduleInfo;

  /**
   * 获取记录数据
   * 当组件加载且有ID参数时，从服务器获取记录数据
   */
  useEffect(() => {
    const fetchRecordData = async () => {
      if (dispatch && Id) {
        setViewId(Id);
        setModifyType(ModifyType.Edit);
        try {
          setIsLoading(true);
          const { Data, Success } = await querySingle({ Id, moduleCode, url });
          if (Success) {
            dispatch(setId({ moduleCode, id: Id }));
            form.setFieldsValue(Data);
          }
        } catch (error) {
          message.error("获取数据失败");
          console.error("获取数据失败:", error);
        } finally {
          setIsLoading(false);
        }

        setDisabled(IsView === true);
      } else {
        setDisabled(false);
        setIsLoading(false);
      }
    };

    fetchRecordData();
  }, [Id, dispatch, moduleCode, url, IsView, form]);

  /**
   * 渲染表单内容
   * 根据模块配置的表单列动态生成表单项
   */
  const renderFormContent = useCallback(() => {
    return (
      <Flex wrap="wrap">
        {formColumns.filter((f: FormColumn) => f.HideInForm === false)?.length === 0 ? (
          <div className="main-tooltip">请选择进行系统表单配置</div>
        ) : (
          formColumns
            .filter((f: FormColumn) => f.HideInForm === false)
            .map((item: FormColumn, index: number) => {
              return (
                <div
                  style={{
                    width: (item.GridSpan != null ? item.GridSpan : 50) + "%"
                  }}
                  key={index}
                >
                  <Layout field={item} disabled={disabled} modifyType={modifyType} />
                </div>
              );
            })
        )}
      </Flex>
    );
  }, [formColumns, disabled, modifyType]);

  /**
   * 处理表单提交
   * @param data 表单数据
   * @param type 提交类型："Save"保存，"SaveAdd"保存并新增
   */
  const handleFormSubmit = useCallback(
    async (data: any, type = "Save") => {
      if (onDisabled) onDisabled(true);
      message.loading("数据提交中...", 0);

      try {
        // 准备提交数据
        let submitData = { ...data, url };
        if (id) submitData.Id = id;
        if (isDetail) submitData[masterColumn] = masterId;
        if (moduleCode !== "SM_MODULE_MNG") submitData.ModuleCode = moduleCode;

        // 处理空值
        for (let key in submitData) {
          submitData[key] = submitData[key] ?? null;
        }

        // 提交数据到服务器
        const { Data, Success, Message } = id ? await update(submitData) : await add(submitData);
        message.destroy();

        if (Success) {
          message.success(Message);
          if (openType === EditOpenType.Modal || openType === EditOpenType.Drawer) onReload?.();

          if (type !== "SaveAdd" && onClose) onClose();

          if (type === "SaveAdd") {
            // 保存并新增：重置表单
            setViewId(null);
            setDisabled(false);
            form.resetFields();
          } else if (!id) {
            // 新增成功：更新ID
            if (setFormPageId) setFormPageId(Data);
            setViewId(Data);
          }
        }
      } catch (error) {
        message.error("提交数据失败");
        console.error("提交数据失败:", error);
      } finally {
        message.destroy();
        if (onDisabled) onDisabled(false);
      }
    },
    [id, url, isDetail, masterColumn, moduleCode, onDisabled, onReload, onClose, setFormPageId, form, openType]
  );

  /**
   * 保存表单数据
   */
  const onSave = useCallback(() => {
    form.validateFields().then(values => handleFormSubmit(values));
  }, [form, handleFormSubmit]);

  /**
   * 保存并新增表单数据
   */
  const onSaveAdd = useCallback(() => {
    form.validateFields().then(values => handleFormSubmit(values, "SaveAdd"));
  }, [form, handleFormSubmit]);

  /**
   * 表单值变更处理
   */
  const onValuesChange = useCallback(() => {
    if (onDisabled) onDisabled(false);
    setDisabled(false);
  }, [onDisabled]);

  // 暴露方法给父组件
  useImperativeHandle(formPageRef, () => ({
    onSave,
    onSaveAdd
  }));

  // 仅在Modal或Drawer模式下渲染表单
  if (openType !== EditOpenType.Modal && openType !== EditOpenType.Drawer) {
    return null;
  }

  return (
    <div style={{ marginTop: 20, marginBottom: 20 }}>
      <Form
        labelCol={{ span: 6, xl: 6, md: 8, sm: 8 }}
        labelWrap
        wrapperCol={{ span: 16 }}
        onFinish={handleFormSubmit}
        onValuesChange={onValuesChange}
        form={form}
      >
        {isLoading ? <Loading /> : renderFormContent()}
      </Form>
    </div>
  );
});

// 添加组件显示名称，便于调试
// FormPage.displayName = "FormPage";

export default FormPage;
