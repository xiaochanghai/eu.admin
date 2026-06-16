import React from "react";
import { Card, Descriptions, Tag, Typography } from "antd";
import { useTranslation } from "react-i18next";
import "./index.less";

const { Link, Title } = Typography;
const style = { width: "280px" };

const About: React.FC = () => {
  const { t } = useTranslation();
  const { pkg, lastBuildTime } = __APP_INFO__;
  const { dependencies, devDependencies, version } = pkg;

  return (
    <div className="about-content">
      <Card className="mb10">
        <Title level={4} className="mb15">
          {t("about.title")}
        </Title>
        <span className="text">
          <Link href="https://github.com/xiaochanghai/eu-admin" target="_blank">
            EU-Admin
          </Link>
          {t("about.description")}
        </span>
      </Card>

      <Card className="mb10">
        <Title level={4} className="mb15">
          {t("about.projectInfo")}
        </Title>
        <Descriptions column={2} bordered size="middle" styles={{ label: style }}>
          <Descriptions.Item label={t("about.version")}>
            <Tag color="processing">{version}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label={t("about.releaseTime")}>
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
          <Descriptions.Item label={t("about.previewUrl")}>
            <Link href="http://116.204.98.209:9527/" target="_blank">
              {t("about.previewUrl")}
            </Link>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card className="mb10">
        <Title level={4} className="mb15">
          {t("about.prodDependencies")}
        </Title>
        <Descriptions column={3} bordered size="middle" styles={{ label: style }}>
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
          {t("about.devDependencies")}
        </Title>
        <Descriptions column={3} bordered size="middle" styles={{ label: style }}>
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
