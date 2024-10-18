import { Alert, Card, Descriptions, Typography } from "antd";
import SvgIcon from "@/components/SvgIcon";
import "./index.less";

const { Link } = Typography;

const SvgIconPage = () => {
  const message = (
    <span>
      SvgIcon 使用 vite-plugin-svg-icons 插件完成，官方文档请查看 ：
      <Link href="https://github.com/vbenjs/vite-plugin-svg-icons" target="_blank">
        https://github.com/vbenjs/vite-plugin-svg-icons
      </Link>
    </span>
  );

  return (
    <Card className="svg-content">
      <Alert message={message} type="success" showIcon />
      <div className="icon-list">
        <SvgIcon name="1" iconStyle={{ width: "180px" }} />
        <SvgIcon name="2" iconStyle={{ width: "180px" }} />
        <SvgIcon name="3" iconStyle={{ width: "180px" }} />
        <SvgIcon name="4" iconStyle={{ width: "180px" }} />
        <SvgIcon name="5" iconStyle={{ width: "180px" }} />
        <SvgIcon name="6" iconStyle={{ width: "180px" }} />
        <SvgIcon name="7" iconStyle={{ width: "180px" }} />
        <SvgIcon name="8" iconStyle={{ width: "180px" }} />
        <SvgIcon name="9" iconStyle={{ width: "180px" }} />
        <SvgIcon name="10" iconStyle={{ width: "180px" }} />
        <SvgIcon name="11" iconStyle={{ width: "180px" }} />
        <SvgIcon name="12" iconStyle={{ width: "180px" }} />
        <SvgIcon name="13" iconStyle={{ width: "180px" }} />
        <SvgIcon name="14" iconStyle={{ width: "180px" }} />
      </div>
      <Descriptions title="配置项 📚" bordered column={1} labelStyle={{ width: "200px" }}>
        <Descriptions.Item label="name">图标的名称，svg 图标必须存储在 src/assets/icons 目录下</Descriptions.Item>
        <Descriptions.Item label="prefix">图标的前缀，默认为 icon</Descriptions.Item>
        <Descriptions.Item label="iconStyle"> 图标的样式，默认样式为 {"{ width: 100px, height: 100px }"} </Descriptions.Item>
      </Descriptions>
    </Card>
  );
};

export default SvgIconPage;
