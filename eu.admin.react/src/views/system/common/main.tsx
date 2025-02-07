import React, { useEffect } from "react";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import FormIndex from "./components/FormIndex";
import { Loading } from "@/components";

// const RouterGuard: React.FC<RouterGuardProps> = props => {
const Index: React.FC<any> = props => {
  const dispatch = useDispatch();
  const { moduleCode } = props;

  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode];
  const getModuleInfo1 = async () => {
    let { Data } = await getModuleInfo(moduleCode);
    dispatch(setModuleInfo(Data));
  };
  useEffect(() => {
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
