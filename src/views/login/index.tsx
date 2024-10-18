import LoginForm from "./components/LoginForm";
import SwitchDark from "@/components/SwitchDark";
import loginIllustration from "@/assets/images/login_illustration.svg";
import logo from "@/assets/images/logo.png";
import "./index.less";
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;

const Login: React.FC = () => {
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
            <span className="login-title-text">{APP_TITLE}</span>
          </div>
          <LoginForm />
        </div>
      </div>
    </div>
  );
};

export default Login;
