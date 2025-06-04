import React, { useEffect, useImperativeHandle, useState } from "react";
import { Card, Form, Flex, Tabs } from "antd";
import TableList from "./TableList";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import { useDispatch, RootState, useSelector } from "@/redux";
import { ModuleInfo, ModifyType } from "@/api/interface/index";
import { message } from "@/hooks/useMessage";
import { Loading, FormToolbar, Element } from "@/components";

/**
 * 表单页面组件
 * 用于处理模块的表单数据展示、编辑、保存等操作
 */
const FormPage: React.FC<any> = props => {
  // Redux相关
  const dispatch = useDispatch();
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);

  // 状态定义
  const [isLoading, setIsLoading] = useState(true); // 加载状态
  const [disabled, setDisabled] = useState(true); // 表单禁用状态
  const [disabledToolbar, setDisabledToolbar] = useState(true); // 工具栏禁用状态
  const [id, setViewId] = useState<string | null>(null); // 当前记录ID
  const [modifyType, setModifyType] = useState(ModifyType.Add); // 修改类型（新增/编辑）
  const [form] = Form.useForm(); // 表单实例

  // 从props中获取参数
  const {
    Id, // 记录ID
    changePage, // 页面切换函数
    moduleCode, // 模块代码
    formPageRef, // 表单页面引用
    onClose, // 关闭回调
    IsView, // 是否为查看模式
    onDisabled, // 禁用回调
    masterId, // 主表ID
    onReload, // 重新加载回调
    childrenItems, // 子项目
    setFormPageId // 设置表单页面ID
  } = props;

  // 获取当前模块信息
  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  const { formColumns, openType, children, url, isDetail, masterColumn } = moduleInfo || {};

  /**
   * 查询单条数据
   * 根据ID获取记录详情并填充表单
   */
  const querySingleData = async () => {
    const { Data, Success } = await querySingle({ Id, moduleCode, url });
    if (Success) {
      // 更新Redux中的ID
      dispatch(setId({ moduleCode, id: Id }));
      // 更新状态并填充表单
      setIsLoading(false);
      form.setFieldsValue(Data);
    }
  };

  /**
   * 初始化表单
   * 根据是否有ID决定是新增还是编辑模式
   */
  useEffect(() => {
    if (Id) {
      // 编辑模式
      setViewId(Id);
      setModifyType(ModifyType.Edit);
      querySingleData();
      setDisabled(false);

      // 如果是查看模式，则禁用表单
      if (IsView) setDisabled(true);
    } else {
      // 新增模式
      setDisabled(false);
      setIsLoading(false);
    }
  }, []);

  /**
   * 渲染表单组件
   * 根据模块配置的表单列生成对应的表单项
   */
  const renderFormComponent = () => {
    return (
      <Flex wrap="wrap">
        {!formColumns || formColumns.filter((f: { HideInForm: boolean }) => f.HideInForm === false)?.length === 0 ? (
          <div className="main-tooltip">请选择进行系统表单配置</div>
        ) : (
          formColumns
            .filter((f: any) => f.HideInForm === false)
            .map((item: any, index: number) => {
              return (
                <div
                  style={{
                    width: (item.GridSpan != null ? item?.GridSpan : 50) + "%"
                  }}
                  key={index}
                >
                  <Element field={item} disabled={disabled} modifyType={modifyType} />
                </div>
              );
            })
        )}
      </Flex>
    );
  };

  /**
   * 表单提交处理
   * @param data 表单数据
   * @param type 保存类型（Save/SaveAdd）
   */
  const onFinish = async (data: any, type = "Save") => {
    // 如果有禁用回调，则先禁用
    if (onDisabled) onDisabled(true);

    // 显示加载提示
    message.loading("数据提交中...", 0);

    // 准备提交数据
    let submitData = { ...data, url };
    if (id) submitData.Id = id;

    // 如果是明细表，添加主表关联
    if (isDetail && masterId) submitData[masterColumn] = masterId;

    // 除了模块管理外，其他模块都需要添加ModuleCode
    if (moduleCode !== "SM_MODULE_MNG") submitData.ModuleCode = moduleCode;

    // 将undefined值转为null
    for (let key in submitData) {
      submitData[key] = submitData[key] ?? null;
    }

    // 根据是否有ID决定是更新还是新增
    const { Data, Success, Message } = id ? await update(submitData) : await add(submitData);

    // 清除加载提示
    message.destroy();

    if (Success) {
      // 恢复禁用状态
      if (onDisabled) onDisabled(false);

      // 显示成功消息
      message.success(Message);

      // 如果是模态框或抽屉，则重新加载数据
      if (openType === "Modal" || openType === "Drawer") onReload?.();

      // 根据保存类型处理后续操作
      if (type !== "SaveAdd" && onClose) {
        onClose();
      } else if (type === "SaveAdd") {
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
  };

  /**
   * 保存表单
   */
  const onSave = () => form.validateFields().then(onFinish);

  /**
   * 保存并新增
   */
  const onSaveAdd = () => form.validateFields().then(values => onFinish(values, "SaveAdd"));

  /**
   * 表单值变化处理
   * 启用工具栏和表单
   */
  const onValuesChange = () => {
    if (onDisabled) onDisabled(false);
    setDisabledToolbar(false);
    setDisabled(false);
  };

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
  let tabItems: any[] = [];

  // 如果没有自定义子项且有子表配置，则根据子表配置生成选项卡
  if (!childrenItems && moduleInfo && children?.length > 0) {
    children.forEach((childModule: any, index: number) => {
      tabItems.push({
        key: index,
        label: childModule.ModuleName,
        children: <TableList moduleCode={childModule.ModuleCode} masterId={id} IsView={IsView} modifyType={modifyType} />
      });
    });
  } else if (childrenItems) {
    // 使用自定义子项
    tabItems = [...tabItems, ...childrenItems];
  }

  return (
    <>
      {/* 标准页面表单 */}
      {openType !== "Drawer" && openType !== "Modal" ? (
        <Form
          labelCol={{
            xs: { span: 8 },
            sm: { span: 8 },
            md: { span: 8 }
          }}
          wrapperCol={{
            xs: { span: 16 },
            sm: { span: 16 },
            md: { span: 16 }
          }}
          labelWrap
          onFinish={onFinish}
          onValuesChange={onValuesChange}
          form={form}
        >
          {/* 表单工具栏 */}
          <FormToolbar
            moduleInfo={moduleInfo}
            disabled={IsView === true || disabled === true || disabledToolbar}
            onFinishAdd={onSaveAdd}
            modifyType={modifyType}
            onBack={() => changePage("FormIndex")}
          />

          {/* 表单内容 */}
          {isLoading ? (
            <Loading />
          ) : (
            <Card size="small" bordered={false}>
              {renderFormComponent()}
            </Card>
          )}
        </Form>
      ) : null}

      {/* 模态框/抽屉表单 */}
      {(openType === "Modal" || openType === "Drawer") && (
        <div style={{ marginTop: 20, marginBottom: 20 }}>
          <Form
            labelCol={{ span: 6, xl: 6, md: 8, sm: 8 }}
            labelWrap
            wrapperCol={{ span: 16 }}
            onFinish={onFinish}
            onValuesChange={onValuesChange}
            form={form}
          >
            {isLoading ? <Loading /> : renderFormComponent()}
          </Form>
        </div>
      )}

      {/* 子表选项卡 */}
      {moduleInfo && tabItems.length > 0 && (
        <>
          <div style={{ height: 10 }}></div>
          <Card size="small" bordered={false}>
            <Tabs items={tabItems} />
          </Card>
        </>
      )}
    </>
  );
};

export default FormPage;
