import React from "react";
import { useDispatch } from "@/redux";
import { RootState, useSelector } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";

const Maximize: React.FC = () => {
  const dispatch = useDispatch();
  const maximize = useSelector((state: RootState) => state.global.maximize);

  return (
    <React.Fragment>
      {maximize && (
        <div className="maximize-icon" onClick={() => dispatch(setGlobalState({ key: "maximize", value: false }))}>
          <i className="iconfont icon-tuichu"></i>;
        </div>
      )}
    </React.Fragment>
  );
};

export default Maximize;
