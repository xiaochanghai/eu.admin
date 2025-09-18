import React, { useRef } from "react";
import { type MenuProps, Dropdown, Avatar } from "antd";
import { Icon } from "@/components";
import { HOME_URL, LOGIN_URL } from "@/config";
import { RootState, useSelector, useDispatch } from "@/redux";
import { useNavigate } from "react-router-dom";
import { logoutApi } from "@/api/modules/login";
import { setToken, clearUserInfo } from "@/redux/modules/user";
import { setAuthMenuList } from "@/redux/modules/auth";
import { modal, message } from "@/hooks/useMessage";
import InfoModal, { InfoModalRef } from "./InfoModal";
import PasswordModal, { PasswordModalRef } from "./PasswordModal";
import avatar from "@/assets/images/avatar.png";

let baseURL = import.meta.env.VITE_API_URL as string;
let VITE_USER_NODE_ENV = import.meta.env.VITE_USER_NODE_ENV as string;

interface AvatarIconProps {
  height?: number;
}

const AvatarIcon: React.FC<AvatarIconProps> = React.memo(({ height = 42 }) => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const userInfo = useSelector((state: RootState) => state.user.userInfo);

  const passRef = useRef<PasswordModalRef>(null);
  const infoRef = useRef<InfoModalRef>(null);

  const logout = () => {
    modal.confirm({
      title: "温馨提示 🧡",
      icon: <Icon name="ExclamationCircleOutlined" />,
      content: "是否确认退出登录？",
      okText: "确认",
      cancelText: "取消",
      maskClosable: true,
      onOk: async () => {
        // Execute the logout interface
        await logoutApi();
        dispatch(clearUserInfo());

        // Set token to empty
        dispatch(setToken(""));

        // Set menu list empty
        dispatch(setAuthMenuList([]));

        // Jump to login page
        navigate(LOGIN_URL, { replace: true });

        message.success("退出登录成功！");
      }
    });
  };

  const getIcon = (type: string) => {
    return <Icon name={type} className="font-size14" />;
  };
  const items: MenuProps["items"] = [
    {
      key: "1",
      label: <span className="dropdown-item">首页</span>,
      icon: getIcon("HomeOutlined"),
      onClick: () => navigate(HOME_URL)
    },
    {
      key: "2",
      label: <span className="dropdown-item">个人信息</span>,
      icon: getIcon("UserOutlined"),
      // onClick: () => infoRef.current?.showModal({ name: "hooks" })
      onClick: () => navigate("/account/settings/index")
    },
    {
      key: "3",
      label: <span className="dropdown-item">修改密码</span>,
      icon: getIcon("FormOutlined"),
      onClick: () => passRef.current?.showModal({ name: "hooks" })
    },
    {
      type: "divider"
    },
    {
      key: "4",
      label: <span className="dropdown-item">关于系统</span>,
      icon: getIcon("InfoCircleOutlined"),
      onClick: () => navigate("/about")
    },
    {
      key: "5",
      label: <span className="dropdown-item">退出登录</span>,
      icon: getIcon("LoginOutlined"),
      onClick: logout
    }
  ];

  return (
    <React.Fragment>
      <Dropdown menu={{ items }} trigger={["click"]} placement="bottom" arrow>
        <Avatar
          className="avatar"
          size={height}
          src={
            userInfo.AvatarFileId
              ? (VITE_USER_NODE_ENV == "development" ? baseURL : "") + `/api/File/Img/${userInfo.AvatarFileId}`
              : avatar
          }
        />
      </Dropdown>
      <InfoModal ref={infoRef} />
      <PasswordModal ref={passRef} />
    </React.Fragment>
  );
});
export default React.memo(AvatarIcon);
