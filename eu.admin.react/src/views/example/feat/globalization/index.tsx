import React from "react";
import type { RadioChangeEvent } from "antd";
import { Card, DatePicker, Divider, Pagination, Radio, Space, Table, TimePicker, Transfer, Typography } from "antd";
import { RootState, useDispatch, useSelector } from "@/redux";
import { setGlobalState } from "@/redux/modules/global";
import { useTranslation } from "react-i18next";

const { Text } = Typography;
const { RangePicker } = DatePicker;

const columns = [
  { title: "Name", dataIndex: "name" },
  { title: "Age", dataIndex: "age" }
];

const Globalization: React.FC = () => {
  const { t } = useTranslation();

  const dispatch = useDispatch();

  const language = useSelector((state: RootState) => state.global.language);

  const changeLocale = (e: RadioChangeEvent) => {
    dispatch(setGlobalState({ key: "language", value: e.target.value }));
  };

  return (
    <Card>
      <span className="mr10">Change locale of components :</span>
      <Radio.Group value={language} onChange={changeLocale} buttonStyle="solid">
        <Radio.Button key="en" value={"en"}>
          English
        </Radio.Button>
        <Radio.Button key="zh" value={"zh"}>
          中文
        </Radio.Button>
      </Radio.Group>

      <Divider />

      <Text>{t("home.welcome")}</Text>

      <Divider />

      <Space direction="vertical" size={[0, 16]} style={{ width: "100%" }}>
        <Pagination defaultCurrent={1} total={50} showSizeChanger />
        <Space wrap>
          <DatePicker />
          <TimePicker />
          <RangePicker />
        </Space>
        <Transfer dataSource={[]} showSearch targetKeys={[]} />
        <Table dataSource={[]} columns={columns} />
      </Space>
    </Card>
  );
};

export default Globalization;
