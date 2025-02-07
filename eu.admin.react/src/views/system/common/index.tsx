import React, { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import FormIndex from "./components/FormIndex";
import { Loading } from "@/components";

// const RouterGuard: React.FC<RouterGuardProps> = props => {
const Index: React.FC = () => {
  const dispatch = useDispatch();
  const { pathname } = useLocation();
  let arr = pathname.split("/");

  const moduleCode = arr[arr.length - 1];
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
