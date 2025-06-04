import React, { useEffect, useImperativeHandle, useState } from "react";
import { Form, Flex } from "antd";
import { Loading, Element } from "@/components";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { message } from "@/hooks/useMessage";
import { EditOpenType } from "@/typings";

// 定义组件props类型
interface FormPageProps {
  Id?: string | null;
  moduleCode: string;
  formPageRef?: React.Ref<any>;
  onClose?: () => void;
  IsView?: boolean;
  onDisabled?: (disabled: boolean) => void;
  masterId?: string;
  onReload?: () => void;
  setFormPageId?: (id: string) => void;
}

/**
 * 表单页面组件
 * 功能：处理表单的展示、编辑和提交
 */
const FormPage: React.FC<FormPageProps> = props => {
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState<string | null>(null);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [form] = Form.useForm();

  // 从redux获取模块信息
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const moduleInfo = moduleInfos[props.moduleCode] as ModuleInfo;
  const { formColumns, openType, url, isDetail, masterColumn } = moduleInfo;

  /**
   * 初始化表单数据
   */
  useEffect(() => {
    const initFormData = async () => {
      if (props.Id) {
        setViewId(props.Id);
        setModifyType(ModifyType.Edit);

        // 查询单条数据
        const { Data, Success } = await querySingle({
          Id: props.Id,
          moduleCode: props.moduleCode,
          url
        });

        if (Success) {
          dispatch(setId({ moduleCode: props.moduleCode, id: props.Id }));
          form.setFieldsValue(Data);
        }
      }

      setIsLoading(false);
      setDisabled(props.IsView || false);
    };

    initFormData();
  }, [props.Id, props.moduleCode, props.IsView]);

  /**
   * 渲染表单字段组件
   */
  const renderFormFields = () => {
    const visibleColumns = formColumns?.filter((f: any) => f.HideInForm === false);

    if (!visibleColumns?.length) {
      return <div className="main-tooltip">请选择进行系统表单配置</div>;
    }

    return (
      <Flex wrap="wrap">
        {visibleColumns.map((item: any, index: number) => (
          <div style={{ width: `${item.GridSpan ?? 50}%` }} key={`${item.DataIndex}_${index}`}>
            <Element field={item} disabled={disabled} modifyType={modifyType} />
          </div>
        ))}
      </Flex>
    );
  };

  /**
   * 表单提交处理
   * @param data 表单数据
   * @param type 提交类型 (Save/SaveAdd)
   */
  const handleSubmit = async (data: any, type = "Save") => {
    const payload = {
      ...data,
      url,
      ...(id && { Id: id }),
      //  ...(isDetail && { [masterColumn]: props.masterId }),
      ...(props.moduleCode !== "SM_MODULE_MNG" && { ModuleCode: props.moduleCode })
    };
    if (isDetail) payload[masterColumn] = props.masterId;

    // 处理空值
    Object.keys(payload).forEach(key => {
      payload[key] = payload[key] ?? null;
    });

    const { Data, Success, Message } = id ? await update(payload) : await add(payload);

    if (Success) {
      message.success(Message);

      // 处理不同提交类型的回调
      if (props.onDisabled) props.onDisabled(true);
      if (openType === EditOpenType.Modal || openType === EditOpenType.Drawer) props.onReload?.();

      if (type !== "SaveAdd") {
        props.onClose?.();
      } else {
        setViewId(null);
        setDisabled(true);
        form.resetFields();
      }

      if (!id && props.setFormPageId) {
        props.setFormPageId(Data);
        setViewId(Data);
      }
    }
  };

  // 暴露方法给父组件
  useImperativeHandle(props.formPageRef, () => ({
    onSave: () => form.validateFields().then(handleSubmit),
    onSaveAdd: () => form.validateFields().then(values => handleSubmit(values, "SaveAdd"))
  }));

  /**
   * 表单值变化处理
   */
  const handleValuesChange = () => {
    props.onDisabled?.(false);
    setDisabled(false);
  };

  // 仅当打开类型为Modal或Drawer时渲染表单
  if (openType !== EditOpenType.Modal && openType !== EditOpenType.Drawer) return null;

  return (
    <div style={{ margin: "20px 0" }}>
      <Form
        labelCol={{ span: 6, xl: 6, md: 8, sm: 8 }}
        labelWrap
        wrapperCol={{ span: 16 }}
        onFinish={handleSubmit}
        onValuesChange={handleValuesChange}
        form={form}
      >
        {isLoading ? <Loading /> : renderFormFields()}
      </Form>
    </div>
  );
};

export default FormPage;
