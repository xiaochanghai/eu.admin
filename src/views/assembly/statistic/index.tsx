import React from "react";
import CountUp from "react-countup";
import { Card, Col, CountdownProps, Row, Statistic } from "antd";
import { ArrowDownOutlined, ArrowUpOutlined, LikeOutlined } from "@ant-design/icons";

const { Countdown } = Statistic;

const formatter = (value: number | string) => <CountUp end={Number(value)} separator="," />;

const deadline = Date.now() + 1000 * 60 * 60 * 24 * 2 + 1000 * 30;

const onFinish: CountdownProps["onFinish"] = () => {
  console.log("finished!");
};

const onChange: CountdownProps["onChange"] = val => {
  if (typeof val === "number" && 4.95 * 1000 < val && val < 5 * 1000) {
    console.log("changed!");
  }
};

const StatisticPage: React.FC = () => {
  return (
    <Row gutter={[10, 10]}>
      <Col span={24}>
        <Card title="基础展示">
          <Row gutter={15}>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic title="Feedback" value={1128} prefix={<LikeOutlined />} />
            </Col>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic title="Unmerged" value={93} suffix="/ 100" />
            </Col>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic
                title="Active"
                value={11.28}
                precision={2}
                valueStyle={{ color: "#3f8600" }}
                prefix={<ArrowUpOutlined />}
                suffix="%"
              />
            </Col>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic
                title="Idle"
                value={9.3}
                precision={2}
                valueStyle={{ color: "#cf1322" }}
                prefix={<ArrowDownOutlined />}
                suffix="%"
              />
            </Col>
          </Row>
        </Card>
      </Col>
      <Col span={24}>
        <Card title="动画效果">
          <Row gutter={15}>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic title="New Customers" value={654932} precision={2} formatter={formatter} />
            </Col>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic title="Active Users" value={132893} formatter={formatter} />
            </Col>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic title="Total Profit" value={219456} precision={2} formatter={formatter} />
            </Col>
            <Col lg={6} md={12} sm={12} xs={12}>
              <Statistic title="Sales Volume" value={854972} precision={2} formatter={formatter} />
            </Col>
          </Row>
        </Card>
      </Col>
      <Col span={24}>
        <Card title="倒计时">
          <Row gutter={[15, 30]}>
            <Col sm={12} xs={24}>
              <Countdown title="Countdown" value={deadline} onFinish={onFinish} />
            </Col>
            <Col sm={12} xs={24}>
              <Countdown title="Million Seconds" value={deadline} format="HH:mm:ss:SSS" />
            </Col>
            <Col sm={12} xs={24}>
              <Countdown title="Day Level" value={deadline} format="D 天 H 时 m 分 s 秒" />
            </Col>
            <Col sm={12} xs={24}>
              <Countdown title="Countdown" value={Date.now() + 60 * 1000} onChange={onChange} />
            </Col>
          </Row>
        </Card>
      </Col>
    </Row>
  );
};

export default StatisticPage;
