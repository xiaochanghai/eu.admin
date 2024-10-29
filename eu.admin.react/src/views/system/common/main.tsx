import React, { useEffect } from "react";
import { useDispatch } from "@/redux";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";
import { RootState, useSelector } from "@/redux";
import { Loading } from "@/components/Loading/index";
import FormIndex from "./components/FormIndex";

// const RouterGuard: React.FC<RouterGuardProps> = props => {
const Index: React.FC<any> = props => {
  const dispatch = useDispatch();
  const { moduleCode } = props;

  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode];
  useEffect(() => {
    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) getModuleInfo1();
  }, []);

  return (
    <React.Fragment>
      {/* <h1>路由参数：{moduleCode}</h1> */}
      {moduleInfo && moduleCode ? <FormIndex moduleCode={moduleCode} /> : <Loading />}
    </React.Fragment>
  );
};

export default Index;
