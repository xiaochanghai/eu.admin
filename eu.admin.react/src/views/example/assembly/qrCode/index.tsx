import { Button, Card, Col, Input, Popover, QRCode, QRCodeProps, Row, Segmented, Space, theme } from "antd";
import React, { useState } from "react";
import "./index.less";

const { useToken } = theme;
const src = "https://gw.alipayobjects.com/zos/rmsportal/KDpgvguMpGfqaHPjicRK.svg";

const QRCodePage: React.FC = () => {
  const { token } = useToken();

  const [text, setText] = useState("https://ant.design/");
  const [level, setLevel] = useState<string | number>("L");

  const downloadQRCode = () => {
    const canvas = document.getElementById("myQRCode")?.querySelector<HTMLCanvasElement>("canvas");
    if (canvas) {
      const url = canvas.toDataURL();
      const a = document.createElement("a");
      a.download = "QRCode.png";
      a.href = url;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    }
  };

  return (
    <Row gutter={[12, 10]} className="qrCode-content">
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="基本使用">
          <QRCode value={text || "-"} />
          <Input placeholder="-" maxLength={60} value={text} onChange={e => setText(e.target.value)} />
        </Card>
      </Col>
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="不同的状态">
          <Space>
            <QRCode value="https://ant.design/" status="loading" />
            <QRCode value="https://ant.design/" status="expired" onRefresh={() => console.log("refresh")} />
          </Space>
        </Card>
      </Col>
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="自定义颜色">
          <Space>
            <QRCode value="https://ant.design/" color={token.colorSuccessText} />
            <QRCode value="https://ant.design/" color={token.colorInfoText} bgColor={token.colorBgLayout} />
          </Space>
        </Card>
      </Col>
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="自定义渲染类型（canvas、svg）">
          <Space>
            <QRCode type="canvas" value="https://ant.design/" />
            <QRCode type="svg" value="https://ant.design/" />
          </Space>
        </Card>
      </Col>
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="生成 Icon">
          <QRCode errorLevel="H" value="https://ant.design/" icon={src} />
        </Card>
      </Col>
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="纠错比例">
          <QRCode style={{ marginBottom: 16 }} errorLevel={level as QRCodeProps["errorLevel"]} value={src} />
          <Segmented options={["L", "M", "Q", "H"]} value={level} onChange={setLevel} />
        </Card>
      </Col>
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="下载二维码" id="myQRCode">
          <QRCode value="https://ant.design/" style={{ marginBottom: 16 }} />
          <Button type="primary" onClick={downloadQRCode}>
            Download
          </Button>
        </Card>
      </Col>
      <Col xl={6} lg={8} md={12} sm={24} xs={24}>
        <Card hoverable title="带气泡卡片">
          <Popover overlayInnerStyle={{ padding: 0 }} content={<QRCode value={src} bordered={false} />}>
            <img width={100} height={100} src={src} alt="icon" style={{ cursor: "pointer" }} />
          </Popover>
        </Card>
      </Col>
    </Row>
  );
};

export default QRCodePage;
