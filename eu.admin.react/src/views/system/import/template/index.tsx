import React, { useState } from "react";
import TableList from "../../common/components/TableList";
import FormPage from "../../common/components/FormPage";
import { Attachment } from "@/components";
import { ViewType } from "@/typings";

/**
 * 子项类型定义
 */
interface ChildrenItem {
  key: number;
  label: string;
  children: React.ReactNode;
}

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
  const [formPageIsView, setFormPageIsView] = useState<boolean | null | undefined>(false);

  /**
   * 切换页面视图
   *
   * @param value - 目标视图类型
   * @param id - 表单ID
   * @param isView - 是否为查看模式
   */
  const changePage = (value: ViewType, id: string, isView?: boolean): void => {
    if (value === "FormPage") {
      setViewType(value);
      setFormPageId(id);
      setFormPageIsView(isView);
    } else if (value === "FormIndex") {
      setViewType(value);
      setFormPageId("");
      setFormPageIsView(false);
    }
  };

  /**
   * 更新表单ID
   *
   * @param id - 新的表单ID
   */
  const handleFormPageIdChange = (id: string): void => {
    setFormPageId(id);
  };

  /**
   * 生成子项配置
   *
   * @returns 子项配置数组
   */
  const generateChildrenItems = (): ChildrenItem[] => {
    if (viewType !== "FormPage") return [];

    return [
      {
        key: 1,
        label: "模板明细",
        children: <TableList moduleCode="SM_IMPORT_TEMPLATE_DETAIL_MNG" masterId={formPageId} IsView={formPageIsView} />
      },
      {
        key: 2,
        label: "模板文件",
        children: <Attachment Id={formPageId} accept=".xlsx,.xls" filePath="ImportTemplate" isUnique={true} />
      }
    ];
  };

  // 生成子项配置
  const childrenItems = generateChildrenItems();

  return (
    <>
      {viewType === "FormIndex" && <TableList moduleCode="SM_IMPORT_TEMPLATE_MNG" changePage={changePage} />}

      {viewType === "FormPage" && (
        <FormPage
          moduleCode="SM_IMPORT_TEMPLATE_MNG"
          Id={formPageId}
          IsView={formPageIsView}
          changePage={changePage}
          setFormPageId={handleFormPageIdChange}
          childrenItems={childrenItems}
        />
      )}
    </>
  );
};

export default ImportTemplate;
