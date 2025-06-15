import React, { useEffect, useCallback } from "react";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import FormIndex from "./components/FormIndex";
import { Loading } from "@/components";

/**
 * 模块主页面属性接口
 */
interface ModuleIndexProps {
  /** 模块代码，用于获取模块信息和渲染对应表单 */
  moduleCode: string;
  extendAction?: any;
}

/**
 * 模块主页面组件
 *
 * 该组件负责加载模块信息并渲染对应的表单组件。
 * 如果模块信息不存在，会先从服务器获取，然后存储到Redux中。
 *
 * @param props 组件属性
 */
const Main: React.FC<ModuleIndexProps> = React.memo(props => {
  const dispatch = useDispatch();
  const { moduleCode, extendAction } = props;

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
      console.error("获取模块信息失败:", error);
      // 这里可以添加错误处理，如显示错误提示等
    }
  }, [moduleCode, dispatch]);

  // 组件挂载或moduleCode变化时，检查并获取模块信息
  useEffect(() => {
    if (!moduleInfo) fetchModuleInfo();
  }, [moduleInfo, fetchModuleInfo]);

  return (
    <React.Fragment>
      {moduleInfo && moduleCode ? <FormIndex moduleCode={moduleCode} extendAction={extendAction} /> : <Loading />}
    </React.Fragment>
  );
});

export default Main;
