import React, { useState, useCallback } from "react";
import TableList from "../../common/components/TableList";
import SqlEdit from "./SqlEdit";
import FormDesign from "../../config/form/components/FormDesign";
import FormPage from "./FormPage";
import { ViewType } from "@/typings";

/**
 * 模块管理组件属性接口
 */
interface SystemModuleProps {
  // 可以在此添加未来可能需要的属性
}

/**
 * 系统模块管理组件
 *
 * 该组件负责系统模块的管理，包括模块列表展示、SQL编辑和表单配置功能。
 * 通过视图类型(viewType)控制显示不同的子组件。
 *
 * @returns React 组件
 */
const SystemModule: React.FC<SystemModuleProps> = () => {
  // 当前视图类型状态
  const [viewType, setViewType] = useState<ViewType>(ViewType.INDEX);
  // 表单页面ID状态
  const [formPageId, setFormPageId] = useState<string>("");
  // 表单页面是否为查看模式状态
  const [formPageIsView, setFormPageIsView] = useState<boolean>(false);

  /**
   * 页面切换处理函数
   *
   * @param value - 目标视图类型
   * @param id - 表单页面ID
   * @param isView - 是否为查看模式
   */
  const changePage = useCallback((value: ViewType, id?: string | null, isView: boolean = false) => {
    setViewType(value);

    if (value === ViewType.INDEX) setFormPageId("");
    else setFormPageId(id ?? "");

    setFormPageIsView(isView);
  }, []);

  /**
   * 根据当前视图类型渲染对应组件
   */
  const renderContent = () => {
    switch (viewType) {
      case ViewType.INDEX:
        return <TableList moduleCode="SM_MODULE_MNG" changePage={changePage} DynamicFormPage={FormPage} />;
      case ViewType.SQL_EDIT:
        return <SqlEdit ModuleId={formPageId} IsView={formPageIsView} changePage={changePage} />;
      case ViewType.FORM_COLLOCATE:
        return (
          <FormDesign moduleCode="SD_SALES_ORDER_MNG" ModuleId={formPageId} IsView={formPageIsView} changePage={changePage} />
        );
      default:
        return null;
    }
  };

  return <>{renderContent()}</>;
};

// 使用 React.memo 优化组件性能，避免不必要的重渲染
export default React.memo(SystemModule);
