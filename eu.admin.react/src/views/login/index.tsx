import LoginForm from "./components/LoginForm";
import SwitchDark from "@/components/SwitchDark";
import loginIllustration from "@/assets/images/login_illustration.svg";
import logo from "@/assets/images/logo.png";
import "./index.less";
import { useTranslation } from "react-i18next";

const Login: React.FC = () => {
  const { t } = useTranslation();

  return (
    <div className="login-container">
      <div className="login-content">
        <SwitchDark />
        <div className="login-illustration">
          <img src={loginIllustration} alt="illustration" />
        </div>
        <div className="login-form">
          <div className="login-form-title">
            <img className="login-title-logo" src={logo} alt="logo" />
            <span className="login-title-text">{t("title")}</span>
          </div>
          <LoginForm />
        </div>
      </div>
    </div>
  );
};

export default Login;
