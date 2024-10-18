import { logoutApi } from "@/api/modules/login";
import { TeamOutlined } from "@ant-design/icons";
import { Alert, Button, Card, Divider } from "antd";
import { useNavigate } from "react-router-dom";
import { useDispatch } from "@/redux";
import { setToken } from "@/redux/modules/user";
import { setAuthMenuList } from "@/redux/modules/auth";
import { LOGIN_URL } from "@/config";
import { message } from "@/hooks/useMessage";

const AuthPage: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();

  const handleToLogin = async () => {
    // Execute the logout interface
    await logoutApi();

    // Set token to empty
    dispatch(setToken(""));

    // Set menu list empty
    dispatch(setAuthMenuList([]));

    // Jump to login page
    navigate(LOGIN_URL);

    message.success("退出登录成功！");
  };

  return (
    <Card>
      <Alert
        message="页面权限采用 React-Router 动态路由实现，根据不同用户角色返回对应路由菜单。登录不同账号观察左侧菜单变化（admin 账号可查看所有菜单、user 账号只可查看部分菜单）"
        type="success"
        showIcon
      />

      <Divider />

      <Button type="primary" icon={<TeamOutlined />} onClick={handleToLogin}>
        登录其他账号
      </Button>
    </Card>
  );
};

export default AuthPage;
