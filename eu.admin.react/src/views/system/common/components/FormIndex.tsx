import React, { useEffect, useState } from "react";
import TableList from "./TableList";
import FormPage from "./FormPage";
// import { useDispatch } from "@/redux";

const FormIndex: React.FC<any> = props => {
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");
  const { moduleCode } = props;
  // const dispatch = useDispatch();
  // dispatch({
  //   type: 'smcommon/setTableStatus',
  //   payload: { moduleCode }
  // });
  // let current = <TableList moduleCode={moduleCode} />;
  // dispatch({
  //   type: 'smcommon/setCurrent',
  //   payload: { current, moduleCode }
  // })
  // dispatch({
  //   type: 'smcommon/setModalVisible',
  //   payload: { moduleCode, visible: false }
  // });
  // dispatch({
  //   type: 'smcommon/setId',
  //   payload: { moduleCode, id: null }
  // });

  useEffect(() => {
    // dispatch(setAuthMenuList(Data));
  }, []);

  const changePage = (value: any, id: string, isVIew: any) => {
    if (value == "FormPage") {
      setViewType(value);
      setFormPageId(id);
      setFormPageIsView(isVIew);
    } else if (value == "FormIndex") {
      setViewType(value);
      setFormPageId("");
      setFormPageIsView("");
    }
  };
  return (
    <>
      {viewType == "FormIndex" ? <TableList moduleCode={moduleCode} changePage={changePage} /> : null}
      {viewType == "FormPage" ? (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}
    </>
  );
};

export default FormIndex;
