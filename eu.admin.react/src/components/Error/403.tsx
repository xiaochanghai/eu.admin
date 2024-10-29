import { Button, Result } from "antd";
import { useNavigate } from "react-router-dom";
import "./index.less";

const NotAuth = () => {
  const navigate = useNavigate();

  return (
    <Result
      className="error-page"
      status="403"
      title="403"
      subTitle="Sorry, you are not authorized to access this page."
      extra={
        <Button type="primary" onClick={() => navigate(-1)}>
          Go Back
        </Button>
      }
    />
  );
};

export default NotAuth;
