import React, { useState, useRef, useMemo } from "react";
import { TableList, FormPage, Attachment } from "@/components";
import { ViewType, ChildrenItem, ActionType } from "@/typings";

/**
 * 导入模板组件属性定义
 */
interface ImportTemplateProps {}

/**
 * 导入模板管理组件
 *
 * 该组件用于管理导入模板，包括模板列表展示、模板详情查看/编辑、模板文件管理等功能
 * 通过viewType状态控制显示列表视图或表单视图
 *
 * @returns 导入模板管理界面
 */
const ImportTemplate: React.FC<ImportTemplateProps> = () => {
  // 视图类型状态：FormIndex(列表) 或 FormPage(表单)
  const [viewType, setViewType] = useState<ViewType>(ViewType.INDEX);
  // 当前操作的表单ID
  const [formPageId, setFormPageId] = useState<string>("");
  // 表单是否为查看模式
  const [formPageIsView, setFormPageIsView] = useState(false);

  const tableRef = useRef<ActionType>();
  const moduleCode = "SM_IMPORT_TEMPLATE_MNG";

  /**
   * 切换页面视图
   *
   * @param value - 目标视图类型
   * @param id - 表单ID
   * @param isView - 是否为查看模式
   */
  const changePage = (value: ViewType, id: string, isView?: boolean): void => {
    setViewType(value);
    if (value === ViewType.PAGE) {
      setFormPageId(id);
      setFormPageIsView(isView ?? false);
    } else if (value === ViewType.INDEX) {
      setFormPageId("");
      setFormPageIsView(false);
    }
  };

  /**
   * 更新表单ID
   *
   * @param id - 新的表单ID
   */
  const handleFormPageIdChange = (id: string): void => setFormPageId(id);

  const onReload = () => tableRef.current?.reload();

  /**
   * 生成子项配置（使用 useMemo 优化性能）
   * 仅在 viewType、formPageId 或 formPageIsView 变化时重新计算
   */
  const childrenItems = useMemo<ChildrenItem[]>(() => {
    if (viewType !== ViewType.PAGE) return [];

    return [
      {
        key: 1,
        label: "主表信息",
        children: <TableList moduleCode="SM_IMPORT_TEMPLATE_MASTER_DETAIL_MNG" masterId={formPageId} IsView={formPageIsView} />
      },
      {
        key: 2,
        label: "模板明细",
        children: <TableList moduleCode="SM_IMPORT_TEMPLATE_DETAIL_MNG" masterId={formPageId} IsView={formPageIsView} />
      },
      {
        key: 3,
        label: "模板文件",
        children: <Attachment Id={formPageId} accept=".xlsx,.xls" filePath="ImportTemplate" isUnique={true} />
      }
    ];
  }, [viewType, formPageId, formPageIsView]);

  return (
    <>
      <div style={{ display: viewType == ViewType.INDEX ? "block" : "none" }}>
        <TableList moduleCode={moduleCode} changePage={changePage} tableActionRef={tableRef} />
      </div>

      {viewType === ViewType.PAGE && (
        <FormPage
          moduleCode={moduleCode}
          Id={formPageId}
          IsView={formPageIsView}
          changePage={changePage}
          setFormPageId={handleFormPageIdChange}
          childrenItems={childrenItems}
          onReload={onReload}
        />
      )}
    </>
  );
};

export default ImportTemplate;
