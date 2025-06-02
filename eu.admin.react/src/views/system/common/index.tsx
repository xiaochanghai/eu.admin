import React, { useEffect, useCallback } from "react";
import { useLocation } from "react-router-dom";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import FormIndex from "./components/FormIndex";
import { Loading } from "@/components";

/**
 * 通用模块页面组件
 *
 * 该组件通过URL路径获取模块代码，加载对应的模块信息，
 * 并渲染相应的表单组件。如果模块信息不存在，会先从服务器获取。
 */
const Index: React.FC = React.memo(() => {
  const dispatch = useDispatch();
  const { pathname } = useLocation();

  // 从URL路径中提取模块代码
  const pathSegments = pathname.split("/");
  const moduleCode = pathSegments[pathSegments.length - 1];

  // 从Redux获取模块信息
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const moduleInfo = moduleInfos[moduleCode];

  /**
   * 获取模块信息并存储到Redux
   */
  const fetchModuleInfo = useCallback(async () => {
    try {
      const { Data } = await getModuleInfo(moduleCode);
      if (Data) dispatch(setModuleInfo(Data));
    } catch (error) {
      console.error(`获取模块[${moduleCode}]信息失败:`, error);
      // 这里可以添加错误处理，如显示错误提示等
    }
  }, [moduleCode, dispatch]);

  // 组件挂载或moduleCode变化时，检查并获取模块信息
  useEffect(() => {
    if (!moduleInfo && moduleCode) fetchModuleInfo();
  }, [moduleInfo, moduleCode, fetchModuleInfo]);

  return <React.Fragment>{moduleInfo && moduleCode ? <FormIndex moduleCode={moduleCode} /> : <Loading />}</React.Fragment>;
});

export default Index;
