import React from "react";
import { Avatar } from "antd";
import { RootState, useSelector } from "@/redux";
import avatar from "@/assets/images/avatar.png";

let baseURL = import.meta.env.VITE_API_URL as string;
let VITE_USER_NODE_ENV = import.meta.env.VITE_USER_NODE_ENV as string;

interface AvatarIconProps {
  height?: number;
}

export const AvatarIcon: React.FC<AvatarIconProps> = React.memo(({ height = 42 }) => {
  const userInfo = useSelector((state: RootState) => state.user.userInfo);

  return (
    <React.Fragment>
      <Avatar
        className="avatar"
        size={height}
        src={
          userInfo.AvatarFileId
            ? (VITE_USER_NODE_ENV == "development" ? baseURL : "") + `/api/File/Img/${userInfo.AvatarFileId}`
            : avatar
        }
      />
    </React.Fragment>
  );
});
