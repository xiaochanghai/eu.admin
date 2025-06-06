import React, { useEffect, useState, useCallback } from "react";
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
import usePermissions from "@/hooks/usePermissions";
import NProgress from "@/config/nprogress";
import { Icon } from "@/components";

// 应用标题常量
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;

// 消息提示的唯一标识
const LOADING_MESSAGE_KEY = "loading";

/**
 * 登录表单组件接口
 */
interface LoginFormProps {}

/**
 * 登录表单组件
 *
 * @description 处理用户登录逻辑，包括表单验证、登录请求、权限初始化和导航
 * @returns {JSX.Element} 登录表单组件
 */
const LoginForm: React.FC<LoginFormProps> = React.memo(() => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { initPermissions } = usePermissions();

  // 表单引用
  const formRef = React.useRef<FormInstance>(null);

  // 加载状态
  const [loading, setLoading] = useState<boolean>(false);

  /**
   * 表单提交成功处理函数
   *
   * @param {ReqLogin} values - 表单提交的值
   */
  const onFinish = useCallback(
    async (values: ReqLogin) => {
      try {
        // 设置加载状态
        setLoading(true);
        NProgress.start();
        message.open({ key: LOADING_MESSAGE_KEY, type: "loading", content: "登录中..." });

        // 发送登录请求
        const { Data } = await loginApi(values);

        // 存储token
        dispatch(setToken(Data.Token));

        // 清除上一个账户的标签页
        dispatch(setTabsList([]));

        // 初始化权限
        await initPermissions(Data.Token);
        NProgress.done();

        // 登录成功提示
        notification.success({
          message: getTimeState(),
          description: `欢迎登录 ${APP_TITLE}`,
          icon: <Icon name="CheckCircleFilled" style={{ color: "#73d13d" }} />
        });

        // 导航到首页
        navigate(HOME_URL);
      } catch (error) {
        setLoading(false);
        // 登录失败处理
        console.error("登录失败:", error);
      } finally {
        // 清理加载状态
        setLoading(false);
        NProgress.done();

        message.destroy(LOADING_MESSAGE_KEY);
      }
    },
    [dispatch, navigate, initPermissions]
  );

  /**
   * 表单提交失败处理函数
   *
   * @param {any} errorInfo - 表单验证错误信息
   */
  const onFinishFailed: FormProps["onFinishFailed"] = useCallback((errorInfo: any) => {
    console.log("表单验证失败:", errorInfo);
  }, []);

  /**
   * 重置表单处理函数
   */
  const onReset = useCallback(() => {
    formRef.current?.resetFields();
  }, []);

  /**
   * 设置回车键提交表单
   */
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.code === "Enter") {
        event.preventDefault();
        formRef.current?.submit();
      }
    };

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, []);

  return (
    <div className="login-form-content">
      <Form name="login" size="large" autoComplete="off" ref={formRef} onFinish={onFinish} onFinishFailed={onFinishFailed}>
        {/* 用户名输入框 */}
        <Form.Item name="UserAccount" rules={[{ required: true, message: "请输入用户名!" }]}>
          <Input prefix={<Icon name="UserOutlined" />} placeholder="用户名" />
        </Form.Item>

        {/* 密码输入框 */}
        <Form.Item name="PassWord" rules={[{ required: true, message: "请输入密码!" }]}>
          <Input.Password prefix={<Icon name="LockOutlined" />} placeholder="密码" />
        </Form.Item>

        {/* 按钮区域 */}
        <Form.Item className="login-form-button">
          <Button shape="round" icon={<Icon name="CloseCircleOutlined" />} onClick={onReset}>
            重置
          </Button>
          <Button type="primary" shape="round" icon={<Icon name="UserOutlined" />} loading={loading} htmlType="submit">
            登录
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
});

export default LoginForm;
