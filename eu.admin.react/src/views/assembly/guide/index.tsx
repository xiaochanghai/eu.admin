import React, { useRef, useState } from "react";
import { type TourProps, Button, Divider, Space, Tour, Alert, Typography, Card } from "antd";
import { EllipsisOutlined } from "@ant-design/icons";
import illustration01 from "@/assets/images/illustration01.svg";
import illustration02 from "@/assets/images/illustration02.svg";
import illustration03 from "@/assets/images/illustration03.svg";

const { Link } = Typography;

const GuidePage: React.FC = () => {
  const ref1 = useRef(null);
  const ref2 = useRef(null);
  const ref3 = useRef(null);

  const [open, setOpen] = useState<boolean>(false);

  const steps: TourProps["steps"] = [
    {
      title: "Center",
      description: "Displayed in the center of screen.",
      target: null
    },
    {
      title: "Upload File",
      description: "Put your files here.",
      target: () => ref1.current,
      cover: <img alt="tour.png" src={illustration01} />
    },
    {
      title: "Save",
      description: "Save your changes.",
      target: () => ref2.current,
      cover: <img alt="tour.png" src={illustration02} />
    },
    {
      title: "Other Actions",
      description: "Click to see other actions.",
      target: () => ref3.current,
      cover: <img alt="tour.png" src={illustration03} />
    }
  ];

  const message = (
    <span>
      通过 Ant Design Tour 漫游式引导插件完成，官方文档请查看 ：
      <Link href="https://ant.design/components/tour-cn" target="_blank">
        https://ant.design/components/tour-cn
      </Link>
    </span>
  );

  return (
    <Card>
      <Alert message={message} type="success" showIcon className="mb25" />

      <Button type="primary" onClick={() => setOpen(true)}>
        Begin Tour
      </Button>

      <Divider />

      <Space>
        <Button ref={ref1}> Upload</Button>
        <Button ref={ref2} type="primary">
          Save
        </Button>
        <Button ref={ref3} icon={<EllipsisOutlined />} />
      </Space>

      <Tour open={open} onClose={() => setOpen(false)} steps={steps} />
    </Card>
  );
};

export default GuidePage;
