import React from "react";
import { setGlobalState } from "@/redux/modules/global";
import { RootState, useSelector, useDispatch } from "@/redux";
import { MenuFoldOutlined, MenuUnfoldOutlined } from "@ant-design/icons";

const CollapseIcon: React.FC = () => {
  const dispatch = useDispatch();
  const isCollapse = useSelector((state: RootState) => state.global.isCollapse);

  return (
    <React.Fragment>
      {React.createElement(isCollapse ? MenuUnfoldOutlined : MenuFoldOutlined, {
        className: "collapsed",
        onClick: () => dispatch(setGlobalState({ key: "isCollapse", value: !isCollapse }))
      })}
    </React.Fragment>
  );
};

export default CollapseIcon;
