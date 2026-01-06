import { useDispatch, useSelector, RootState } from "@/redux";
import { setTableParam, setSearchVisible } from "@/redux/modules/module";
import { queryByFilter } from "@/api/modules/module";
import { ModuleInfo } from "@/api/interface/index";

/**
 * ProTable 数据管理 Hook
 * 负责数据请求、分页、排序、筛选和状态管理
 *
 * @param moduleCode 模块代码
 * @param moduleInfo 模块信息
 * @param masterId 主表ID（用于明细表过滤）
 * @returns 数据请求相关的状态和函数
 */
export const useProTableData = (moduleCode: string, moduleInfo: ModuleInfo, masterId?: string | null) => {
  const dispatch = useDispatch();
  const searchVisibles = useSelector((state: RootState) => state.module.searchVisibles);
  const tableParams = useSelector((state: RootState) => state.module.tableParams);

  const tableParam = tableParams[moduleCode] as any;
  const searchVisible = searchVisibles[moduleCode] ?? false;

  /**
   * 处理表格数据请求
   */
  const handleRequest = async (params: any, sorter: any, _filterCondition: any) => {
    // 合并表格参数
    if (tableParam?.params && !params._timestamp) {
      params = { ...tableParam.params, ...params };
    }
    if (tableParam?.sorter) {
      sorter = { ...tableParam.sorter, ...sorter };
    }

    // 构建过滤条件
    const filter = {
      PageIndex: params.current,
      PageSize: params.pageSize,
      sorter,
      params,
      Conditions:
        moduleInfo.isDetail && moduleInfo.masterColumn && masterId
          ? `A.${moduleInfo.masterColumn} = '${masterId}'`
          : moduleInfo.isDetail
          ? "1 != 1"
          : ""
    };

    // 保存参数到 Redux
    dispatch(setTableParam({ params, sorter, moduleCode, filter }));

    // 处理明细表且无主表 ID 的情况
    if (moduleInfo.isDetail && !masterId) {
      return { data: [], success: true, total: 0 };
    }

    // 加载数据
    return await queryByFilter(moduleCode, {}, filter);
  };

  /**
   * 重置表格参数
   */
  const handleReset = () => {
    dispatch(setTableParam({ moduleCode }));
  };

  /**
   * 切换搜索栏显示/隐藏
   */
  const onSearchToggle = () => {
    dispatch(setSearchVisible({ value: !searchVisible, moduleCode }));
  };

  return {
    searchVisible,
    tableParam,
    handleRequest,
    handleReset,
    onSearchToggle
  };
};
