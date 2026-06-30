import React, { useCallback, useEffect, useImperativeHandle, useMemo, useState } from "react";
import { Flex, Tree, Card, Form, Tabs, message } from "antd";
import { querySingle, add, update } from "@/api/modules/module";
import { setId } from "@/redux/modules/module";
import { useDispatch, RootState, useSelector } from "@/redux";
import http from "@/api";
import { Loading } from "@/components";
import { ModuleInfo } from "@/api/interface";
import { SaveTypeEnum, EditOpenType, ModifyType } from "@/typings";
import { STANDARD_FORM_LAYOUT } from "@/config";
import { Element, Skeleton } from "@/components";

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [form] = Form.useForm();

  const [isLoading, setIsLoading] = useState(true);
  const [disabled, setDisabled] = useState(true);
  const [id, setViewId] = useState<any>(null);
  const [treeData, setTreeData] = useState<any[]>([]);
  const [checkedModuleKeys, setCheckedModuleKeys] = useState<any[]>([]);
  const [modifyType, setModifyType] = useState(ModifyType.Add);
  const [groupId, setGroupId] = useState<string | number | null>(null);

  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const { Id, moduleCode, formPageRef, onDisabled, masterId, onReload } = props;
  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  const { formColumns, openType, url, isDetail, masterColumn } = moduleInfo;

  useEffect(() => {
    const loadData = async () => {
      try {
        if (Id) {
          setViewId(Id);
          setModifyType(ModifyType.Edit);

          const [{ Data: formData, Success: formSuccess }, { Data: roleData, Success: roleSuccess }] = await Promise.all([
            querySingle({ Id, moduleCode, url }),
            http.get<any>(`/api/SmUserRole/QueryUserRole/${Id}`)
          ]);

          if (formSuccess) {
            setGroupId(formData.GroupId ?? null);
            dispatch(setId({ moduleCode, id: Id }));
            form.setFieldsValue(formData);
          }

          if (roleSuccess) {
            setCheckedModuleKeys(roleData.map((item: any) => item.SmRoleId));
          }
        } else {
          setViewId(null);
          setModifyType(ModifyType.Add);
          setGroupId(null);
        }

        const { Data, Success } = await http.get<any>("/api/SmUserRole/QueryRole");
        if (Success) setTreeData([Data]);
      } finally {
        setIsLoading(false);
        setDisabled(false);
      }
    };

    loadData();
  }, [Id, dispatch, form, moduleCode, url]);

  const visibleColumns = useMemo(() => {
    return formColumns?.filter((item: any) => {
      if (item.HideInForm !== false) return false;
      if (modifyType === ModifyType.Add && item.CreateHide === true) return false;
      if (modifyType === ModifyType.Edit && item.ModifyHide === true) return false;
      return true;
    });
  }, [formColumns, modifyType]);

  const onFinish = useCallback(
    async (values: any, type = SaveTypeEnum.Save) => {
      const data = {
        ...values,
        url,
        ...(id ? { Id: id } : {}),
        ...(isDetail ? { [masterColumn]: masterId } : {}),
        ...(moduleCode !== "SM_MODULE_MNG" ? { ModuleCode: moduleCode } : {})
      };

      Object.keys(data).forEach(key => {
        data[key] = data[key] ?? null;
      });

      const { Data, Success, Message } = id ? await update(data) : await add(data);

      if (Success) {
        message.success(Message);
        onDisabled?.(true);
        if (openType === EditOpenType.Modal || openType === EditOpenType.Drawer) onReload?.();

        if (type === SaveTypeEnum.SaveAdd) {
          setViewId(null);
          setGroupId(null);
          setDisabled(true);
          form.resetFields();
        } else if (!id) {
          setViewId(Data);
        }
      }
    },
    [form, id, isDetail, masterColumn, masterId, moduleCode, onDisabled, onReload, openType, url]
  );

  const onSave = useCallback(() => form.validateFields().then(values => onFinish(values)), [form, onFinish]);
  const onSaveAdd = useCallback(
    () => form.validateFields().then(values => onFinish(values, SaveTypeEnum.SaveAdd)),
    [form, onFinish]
  );

  const onValuesChange = useCallback(() => {
    onDisabled?.(false);
    setDisabled(false);
  }, [onDisabled]);

  const onModuleCheck = useCallback(
    async (keys: any) => {
      setCheckedModuleKeys(keys);
      await http.post<any>("/api/SmUserRole/BatchInsertUserRole", { roleList: keys, UserId: id });
    },
    [id]
  );

  useImperativeHandle(formPageRef, () => ({ onSave, onSaveAdd }), [onSave, onSaveAdd]);

  const items = useMemo(
    () => [
      {
        key: "role",
        label: "功能角色",
        children: (
          <Tree
            defaultExpandedKeys={["All"]}
            checkedKeys={checkedModuleKeys}
            onCheck={onModuleCheck}
            checkable
            treeData={treeData}
          />
        )
      }
    ],
    [checkedModuleKeys, onModuleCheck, treeData]
  );

  const onGroupChange = useCallback(
    (value: string | number | null) => {
      setGroupId(value);
      form.setFieldsValue({ CompanyId: null });
    },
    [form]
  );

  const renderElement = useCallback(
    (item: any, index: number) => {
      if (item.DataIndex === "CompanyId" && !groupId) return null;

      const width = { width: `${item?.GridSpan ?? 50}%` };
      const style = visibleColumns.length - 1 === index ? { marginBottom: 0 } : undefined;
      const cascadeProps = item.DataIndex === "CompanyId" ? { parentColumn: "GroupId", parentId: groupId } : {};
      const changeProps = item.DataIndex === "GroupId" ? { onChange: onGroupChange } : {};

      return (
        <div style={width} key={item.FieldName || item.DataIndex || index}>
          <Element field={item} disabled={disabled} modifyType={modifyType} style={style} {...cascadeProps} {...changeProps} />
        </div>
      );
    },
    [disabled, groupId, modifyType, onGroupChange, visibleColumns]
  );

  const renderFormComponent = () => {
    if (!visibleColumns || visibleColumns.length === 0) {
      return (
        <Flex wrap="wrap">
          <div className="main-tooltip">请选择进行系统表单配置</div>
        </Flex>
      );
    }

    return <Flex wrap="wrap">{visibleColumns.map(renderElement)}</Flex>;
  };

  return (
    <>
      {openType === EditOpenType.Modal || openType === EditOpenType.Drawer ? (
        <Form {...STANDARD_FORM_LAYOUT} labelWrap onFinish={onFinish} onValuesChange={onValuesChange} form={form}>
          {isLoading ? <Loading /> : renderFormComponent()}
        </Form>
      ) : null}

      {id ? (
        <>
          <div style={{ height: 20 }} />
          <Card>
            {treeData.length === 0 ? (
              <>
                <Skeleton active />
                <Skeleton active />
                <Skeleton active />
              </>
            ) : (
              <Tabs items={items} />
            )}
          </Card>
        </>
      ) : null}
    </>
  );
};

export default FormPage;
