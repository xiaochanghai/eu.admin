import { useRef } from "react";
import { useDispatch, useSelector, RootState } from "@/redux";
import { setTableParam, setSearchVisible } from "@/redux/modules/module";
import { queryByFilter } from "@/api/modules/module";
import { ModuleInfo } from "@/api/interface/index";

type FormRefLike = {
  current?: {
    getFieldsValue?: (...args: any[]) => Record<string, any>;
    isFieldsTouched?: (...args: any[]) => boolean;
  } | null;
};

const getSearchParamKeys = (moduleInfo: ModuleInfo) => {
  return (moduleInfo.columns ?? [])
    .filter((column: any) => !column.hideInSearch)
    .map((column: any) => column.dataIndex)
    .filter((dataIndex: any) => dataIndex !== undefined && dataIndex !== null)
    .map((dataIndex: any) => (Array.isArray(dataIndex) ? dataIndex.join(".") : String(dataIndex)));
};

const isEmptySearchValue = (value: any) => value === undefined || value === null || value === "";

/**
 * ProTable 数据管理 Hook
 * 负责数据请求、分页、排序、筛选和状态管理
 *
 * @param moduleCode 模块代码
 * @param moduleInfo 模块信息
 * @param masterId 主表ID（用于明细表过滤）
 * @returns 数据请求相关的状态和函数
 */
export const useProTableData = (
  moduleCode: string,
  moduleInfo: ModuleInfo,
  masterId?: string | null,
  customConditions?: string,
  formRef?: FormRefLike
) => {
  const dispatch = useDispatch();
  const searchVisibles = useSelector((state: RootState) => state.module.searchVisibles);
  const tableParams = useSelector((state: RootState) => state.module.tableParams);

  // 记录最近一次请求实际使用的参数（含分页/排序/筛选），供 onLoad 同步回填表单。
  // 用 ref 而非 Redux tableParam，避免读取到慢一拍的 render 闭包导致输入框回退到上一次的值。
  const latestParamsRef = useRef<Record<string, any> | null>(null);

  const tableParam = tableParams[moduleCode] as any;
  const searchVisible = searchVisibles[moduleCode] ?? false;

  /**
   * 处理表格数据请求
   */
  const handleRequest = async (params: any, sorter: any, _filterCondition: any) => {
    const form = formRef?.current;
    const formValues = form?.getFieldsValue?.(true);
    const hasUserChangedSearchForm = form?.isFieldsTouched?.() ?? true;

    // 分页以 ProTable 本次请求传入的 params 为准（ProTable 内部 pageInfo 是页码的唯一来源）。
    // onLoad 回填表单时会把 current/pageSize 一起 setFieldsValue，导致下面 getFieldsValue(true)
    // 读到的 formValues 里残留着上一次的旧分页值；若让它参与合并（formValues 在最后 spread），
    // 会把真实页码覆盖掉，表现为翻页时请求始终发送旧页码、翻不动页。先捕获、最后强制写回。
    const requestedCurrent = params.current;
    const requestedPageSize = params.pageSize;

    // 合并表格参数
    if (tableParam?.params && !params._timestamp) {
      if (formValues && hasUserChangedSearchForm) {
        const nextParams = { ...tableParam.params };
        getSearchParamKeys(moduleInfo).forEach(key => delete nextParams[key]);
        params = { ...nextParams, ...params, ...formValues };
      } else {
        params = { ...tableParam.params, ...params };
      }
    }
    if (tableParam?.sorter) {
      sorter = { ...tableParam.sorter, ...sorter };
    }

    if (formValues && hasUserChangedSearchForm) {
      params = { ...params, ...formValues };
      Object.entries(formValues).forEach(([key, value]) => {
        if (isEmptySearchValue(value)) {
          delete params[key];
        }
      });
    }

    // 强制还原本次请求的真实分页值，避免被表单残留的旧 current/pageSize 覆盖导致翻页失效
    params.current = requestedCurrent;
    params.pageSize = requestedPageSize;

    // 构建基础过滤条件
    let baseConditions = "";
    if (moduleInfo.isDetail && moduleInfo.masterColumn && masterId) {
      baseConditions = `A.${moduleInfo.masterColumn} = '${masterId}'`;
    } else if (moduleInfo.isDetail) {
      baseConditions = "1 != 1";
    }

    // 拼接自定义条件
    const Conditions = customConditions
      ? baseConditions
        ? `${baseConditions} AND ${customConditions}`
        : customConditions
      : baseConditions;

    const filter = {
      PageIndex: params.current,
      PageSize: params.pageSize,
      sorter,
      params,
      Conditions
    };

    // 同步记录本次请求实际使用的参数（必须在 dispatch 之前/同时，且不依赖 render 闭包），
    // 供组件的 onLoad 回填表单使用，避免读取慢一拍的 Redux tableParam
    latestParamsRef.current = params;

    // 保存参数到 Redux
    dispatch(setTableParam({ params, sorter, moduleCode, filter }));

    // 处理明细表且无主表 ID 的情况
    if (moduleInfo.isDetail && !masterId) {
      return { data: [], success: true, total: 0 };
    }

    // 加载数据
    return await queryByFilter(moduleCode, {}, filter, moduleInfo.queryApiUrl);
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
    latestParamsRef,
    handleRequest,
    handleReset,
    onSearchToggle
  };
};
