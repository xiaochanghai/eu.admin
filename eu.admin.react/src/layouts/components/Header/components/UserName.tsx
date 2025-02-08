import React, { useEffect } from "react";
import { Spin } from "antd";
import { RootState, useSelector, useDispatch } from "@/redux";
import { setUserInfo } from "@/redux/modules/user";
import http from "@/api";

const UserName: React.FC = () => {
  const dispatch = useDispatch();
  const userInfo = useSelector((state: RootState) => state.user.userInfo);
  const querySingleData = async () => {
    let { Data, Success } = await http.get<any>("/api/Authorize/CurrentUser");
    if (Success) dispatch(setUserInfo(Data));
  };
  useEffect(() => {
    if (!userInfo.UserName) querySingleData();
  }, []);

  return userInfo && userInfo.UserName ? (
    <span className="username">{userInfo.UserName}</span>
  ) : (
    <span>
      <Spin
        size="small"
        style={{
          marginLeft: 8,
          marginRight: 8
        }}
      />
    </span>
  );
};

export default UserName;
