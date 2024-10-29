import React, { useEffect, useState } from "react";
import { Button, Form, Input } from "antd";
import { HOME_URL } from "@/config";
import { getTimeState } from "@/utils";
import { useDispatch } from "@/redux";
import { setToken } from "@/redux/modules/user";
import { setTabsList } from "@/redux/modules/tabs";
import { loginApi } from "@/api/modules/login";
import { ReqLogin } from "@/api/interface";
import { useNavigate } from "react-router-dom";
import { message, notification } from "@/hooks/useMessage";
import type { FormInstance, FormProps } from "antd/es/form";
import { LockOutlined, UserOutlined, CloseCircleOutlined, CheckCircleFilled } from "@ant-design/icons";
import usePermissions from "@/hooks/usePermissions";
// import md5 from "md5";
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;

const LoginForm: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();

  const { initPermissions } = usePermissions();

  const formRef = React.useRef<FormInstance>(null);
  const [loading, setLoading] = useState(false);

  const key = "loading";

  const onFinish = async (values: ReqLogin) => {
    try {
      // loading
      setLoading(true);
      message.open({ key, type: "loading", content: "登录中..." });

      // user login
      // const { data } = await loginApi({ ...values, password: md5(values.password) });
      const { Data } = await loginApi(values);
      // if (Success) {
      dispatch(setToken(Data.Token));

      // clear last account tabs
      dispatch(setTabsList([]));

      // init permissions
      await initPermissions(Data.Token);

      // prompt for successful login and redirect
      notification.success({
        message: getTimeState(),
        description: "欢迎登录 " + APP_TITLE,
        icon: <CheckCircleFilled style={{ color: "#73d13d" }} />
      });

      // navigate to home
      navigate(HOME_URL);
      // } else message.error(Message);
    } finally {
      setLoading(false);
      message.destroy(key);
    }
  };

  const onFinishFailed: FormProps["onFinishFailed"] = errorInfo => {
    console.log("Failed:", errorInfo);
  };

  const onReset = () => {
    formRef.current?.resetFields();
  };

  useEffect(() => {
    document.onkeydown = event => {
      if (event.code === "Enter") {
        event.preventDefault();
        formRef.current?.submit();
      }
    };
    return () => {
      document.onkeydown = () => {};
    };
  }, []);
  return (
    <div className="login-form-content">
      <Form name="login" size="large" autoComplete="off" ref={formRef} onFinish={onFinish} onFinishFailed={onFinishFailed}>
        <Form.Item name="UserAccount" rules={[{ required: true, message: "请输入用户名!" }]}>
          <Input prefix={<UserOutlined />} placeholder="用户名" />
        </Form.Item>
        <Form.Item name="PassWord" rules={[{ required: true, message: "请输入密码!" }]}>
          <Input.Password prefix={<LockOutlined />} placeholder="密码" />
        </Form.Item>
        <Form.Item className="login-form-button">
          <Button shape="round" icon={<CloseCircleOutlined />} onClick={onReset}>
            重置
          </Button>
          <Button type="primary" shape="round" icon={<UserOutlined />} loading={loading} htmlType="submit">
            登录
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
};

export default LoginForm;
