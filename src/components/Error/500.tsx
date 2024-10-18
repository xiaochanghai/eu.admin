import { Button, Result } from "antd";
import { useNavigate } from "react-router-dom";
import "./index.less";

const NotNetwork = () => {
  const navigate = useNavigate();

  return (
    <Result
      className="error-page"
      status="500"
      title="500"
      subTitle="Sorry, something went wrong."
      extra={
        <Button type="primary" onClick={() => navigate(-1)}>
          Go Back
        </Button>
      }
    />
  );
};

export default NotNetwork;
