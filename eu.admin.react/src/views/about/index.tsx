import React from "react";
import { Card, Descriptions, Tag, Typography } from "antd";
import "./index.less";

const { Link, Title } = Typography;
const style = { width: "280px" };

const About: React.FC = () => {
  const { pkg, lastBuildTime } = __APP_INFO__;
  const { dependencies, devDependencies, version } = pkg;

  return (
    <div className="about-content">
      <Card className="mb10">
        <Title level={4} className="mb15">
          关于
        </Title>
        <span className="text">
          <Link href="https://github.com/xiaochanghai/eu-admin" target="_blank">
            EU-Admin
          </Link>
          一款一款基于.NET 8、React18、React-RouterV6、React-Hooks、Redux-Toolkit、Zustand、TypeScript、Vite5、Ant-Design5
          的实现的通用管理平台框架，开箱即用。集成SqlSugar、缓存、 通讯、远程请求、任务调度等 开源的后台管理框架。
        </span>
      </Card>

      <Card className="mb10">
        <Title level={4} className="mb15">
          项目信息
        </Title>
        <Descriptions column={2} bordered size="middle" labelStyle={style}>
          <Descriptions.Item label="版本号">
            <Tag color="processing">{version}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="发布时间">
            <Tag color="processing">{lastBuildTime}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Gitee">
            <Link href="https://gitee.com/xiaochanghai520/eu-admin" target="_blank">
              Gitee
            </Link>
          </Descriptions.Item>
          <Descriptions.Item label="Github">
            <Link href="https://github.com/xiaochanghai/eu-admin" target="_blank">
              Github
            </Link>
          </Descriptions.Item>
          <Descriptions.Item label="Issues">
            <Link href="https://github.com/xiaochanghai/eu-admin/issues" target="_blank">
              Issues
            </Link>
          </Descriptions.Item>
          <Descriptions.Item label="预览地址">
            <Link href="https://gitee.com/link?target=http%3A%2F%2Feu-keji.com%2F" target="_blank">
              预览地址
            </Link>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card className="mb10">
        <Title level={4} className="mb15">
          生产环境依赖
        </Title>
        <Descriptions column={3} bordered size="middle" labelStyle={style}>
          {Object.keys(dependencies).map(key => {
            return (
              <React.Fragment key={key}>
                <Descriptions.Item label={key}>
                  <Tag color="default">{dependencies[key]} </Tag>
                </Descriptions.Item>
              </React.Fragment>
            );
          })}
        </Descriptions>
      </Card>

      <Card>
        <Title level={4} className="mb15">
          开发环境依赖
        </Title>
        <Descriptions column={3} bordered size="middle" labelStyle={style}>
          {Object.keys(devDependencies).map(key => {
            return (
              <React.Fragment key={key}>
                <Descriptions.Item label={key}>
                  <Tag color="default">{devDependencies[key]} </Tag>
                </Descriptions.Item>
              </React.Fragment>
            );
          })}
        </Descriptions>
      </Card>
    </div>
  );
};

export default About;
