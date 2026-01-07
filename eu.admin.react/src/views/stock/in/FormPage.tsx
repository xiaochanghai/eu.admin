/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useState, useEffect } from "react";
import { Flex, Popconfirm } from "antd";
import { getModuleInfo } from "@/api/modules/module";
import { useSelector, useDispatch, RootState } from "@/redux";
import { setModuleInfo } from "@/redux/modules/module";
import http from "@/api";
import { message } from "@/hooks/useMessage";
import { createUuid } from "@/utils";
import { BaseFormPage, Loading, EditableProTable, ComboGrid, Element } from "@/components";
import { ModifyType } from "@/typings";

const FormPage: React.FC<any> = props => {
  const dispatch = useDispatch();
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);

  // 自定义状态
  const [stockId, setStockId] = useState<string | null>(null);
  const [stockId1, setStockId1] = useState<string | null>(null);
  const [goodsLocationId, setGoodsLocationId] = useState<string | null>(null);
  const [masterStockId, setMasterStockId] = useState<string | null>(null);
  const [masterGoodsLocationId, setMasterGoodsLocationId] = useState<string | null>(null);
  const [dataSource, setDataSource] = useState<any>([]);

  // 明细表模块
  const moduleCode1 = "IV_IN_DETAIL_MNG";
  const moduleInfo1 = moduleInfos[moduleCode1];

  // 加载明细表模块信息
  useEffect(() => {
    const loadDetailModuleInfo = async () => {
      if (!moduleInfo1) {
        const { Data } = await getModuleInfo(moduleCode1);
        dispatch(setModuleInfo(Data));
      }
    };
    loadDetailModuleInfo();
  }, [moduleInfo1, dispatch]);

  /**
   * 自定义表单字段渲染
   */
  const renderFormFields = ({ formColumns, form, disabled }: any) => {
    return (
      <Flex wrap="wrap">
        {formColumns
          ?.filter((f: any) => f.HideInForm === false)
          ?.map((item: any, index: number) => {
            const width = (item.GridSpan ?? 50) + "%";

            // StockId 特殊处理：onChange 时清空 GoodsLocationId
            if (item.DataIndex === "StockId") {
              return (
                <div style={{ width }} key={index}>
                  <Element
                    field={item}
                    disabled={disabled ?? props.IsView}
                    onChange={(value: any) => {
                      setStockId(value);
                      form.setFieldsValue({ GoodsLocationId: null });
                    }}
                  />
                </div>
              );
            }

            // GoodsLocationId 特殊处理：依赖 StockId
            if (item.DataIndex === "GoodsLocationId") {
              return (
                <div style={{ width }} key={index}>
                  <Element
                    field={item}
                    disabled={stockId ? (disabled ?? props.IsView) : true}
                    parentColumn="StockId"
                    parentId={stockId}
                  />
                </div>
              );
            }

            // 默认渲染
            return (
              <div style={{ width }} key={index}>
                <Element field={item} disabled={disabled ?? props.IsView} />
              </div>
            );
          })}
      </Flex>
    );
  };

  /**
   * 自定义明细表渲染
   */
  const renderDetailTable = ({ tableRef, masterId, modifyType }: any) => {
    // 自定义操作列
    const actionColumn = {
      title: "操作",
      dataIndex: "option",
      fixed: "right",
      valueType: "option",
      width: 150,
      render: (_text: any, record: any, _: any, action: any) => [
        <a
          key="editable"
          onClick={() => {
            setStockId1(null);
            setGoodsLocationId(record.GoodsLocationId);
            setStockId1(record.StockId);
            action?.startEditable?.(record.ID);
          }}
        >
          编辑
        </a>,
        <Popconfirm
          key="delete"
          title="提醒"
          description="是否确定删除记录?"
          onConfirm={async () => {
            const { Success, Message } = await http.delete<any>("/api/IvInDetail/" + record.ID);
            if (Success) message.success(Message);
            if (tableRef.current) tableRef.current.reload();
          }}
          okType="danger"
          okText="确定"
          cancelText="取消"
        >
          <a>删除</a>
        </Popconfirm>
      ]
    };

    // 构建列配置
    let columns: any = [];
    if (modifyType !== ModifyType.View) {
      if (moduleInfo1?.columns) columns = [...moduleInfo1.columns, actionColumn];
    } else if (moduleInfo1?.columns) {
      columns = [...moduleInfo1.columns];
    }

    // 自定义列渲染
    columns?.forEach((item: any, index: number) => {
      let hasChange = false;
      let column = columns[index];

      const formItemProps = () => ({
        rules: [{ required: true, message: "此项为必填项" }]
      });

      // MaterialId 自定义渲染
      if (item.dataIndex === "MaterialId") {
        column = {
          ...column,
          renderFormItem: (_item: any, { isEditable }: any) =>
            isEditable ? <ComboGrid code="BdMaterial" /> : null,
          render: (_text: any, record: any) => <>{record.MaterialName}</>
        };
        hasChange = true;
      }

      // StockId 自定义渲染
      else if (item.dataIndex === "StockId") {
        column = {
          ...column,
          renderFormItem: (item: any, { isEditable }: any, _form: any) =>
            isEditable ? (
              <ComboGrid
                code="BdStock"
                onChange={(value: any) => {
                  if (value) {
                    item.entity = { ...item.entity, StockId: value, GoodsLocationId: null };
                    setStockId1(value);
                    setGoodsLocationId(null);
                    _form.setFieldsValue({
                      [item.entity.ID]: { GoodsLocationId: null }
                    });
                  }
                }}
              />
            ) : null,
          render: (_text: any, record: any) => <>{record.StockName || "-"}</>
        };
        hasChange = true;
      }

      // GoodsLocationId 自定义渲染
      else if (item.dataIndex === "GoodsLocationId") {
        column = {
          ...column,
          renderFormItem: (item: any, { isEditable }: any, _form: any) => {
            _form.setFieldsValue({ GoodsLocationId: goodsLocationId });
            return isEditable ? (
              <ComboGrid
                code="BdGoodsLocation"
                value={goodsLocationId}
                onChange={(value: any) => {
                  if (value) item.entity = { ...item.entity, GoodsLocationId: value };
                }}
                disabled={!stockId1}
                parentColumn="StockId"
                parentId={stockId1 || null}
              />
            ) : (
              <>{item.entity.MaterialName}-111</>
            );
          },
          render: (_text: any, record: any) => <>{record.GoodsLocationName || "-"}</>
        };
        hasChange = true;
      }

      // 必填项处理
      if (item.required === true) {
        column = { ...column, formItemProps };
        hasChange = true;
      }

      if (hasChange) columns[index] = column;
    });

    return moduleInfo1 && columns ? (
      <EditableProTable
        moduleCode={moduleCode1}
        tableRef={tableRef}
        modifyType={modifyType}
        masterId={masterId}
        moduleInfo={moduleInfo1}
        columns={columns}
        recordCreatorProps={
          masterId
            ? {
                position: "end",
                record: () => ({
                  ID: createUuid(),
                  StockId: masterStockId,
                  GoodsLocationId: masterGoodsLocationId
                })
              }
            : false
        }
        value={dataSource}
        onChange={setDataSource}
        successCallBack={(originData: any, data: any) => {
          originData.MaterialName = data.MaterialName;
          originData.Specifications = data.Specifications;
          originData.UnitName = data.UnitName;
          originData.GoodsLocationName = data.GoodsLocationName;
          originData.StockName = data.StockName;
          originData.QTY = data.QTY;
          originData.Amount = data.Amount;
          return originData;
        }}
        failCallBack={() => {
          tableRef.current?.reload();
        }}
      />
    ) : (
      <Loading />
    );
  };

  return (
    <BaseFormPage
      {...props}
      moduleCode={props.moduleCode}
      changePage={props.changePage}
      showAttachment={false}
      detailModuleCode="IV_IN_DETAIL_MNG"
      detailTableTitle="物料信息"
      renderFormFields={renderFormFields}
      renderDetailTable={renderDetailTable}
      computeModifyType={({ modifyType, orderStatus }) =>
        orderStatus === "WaitShip" ? modifyType : ModifyType.View
      }
      onQuerySuccess={(data) => {
        // 查询成功后设置状态
        setStockId(data.StockId);
        setStockId1(data.StockId);
        setMasterStockId(data.StockId);
        setMasterGoodsLocationId(data.GoodsLocationId);
      }}
      beforeSave={(formData) => {
        // 保存成功后更新主表状态
        if (!props.Id) {
          setMasterStockId(formData.StockId);
          setMasterGoodsLocationId(formData.GoodsLocationId);
        }
        return formData;
      }}
    />
  );
};

export default FormPage;
