import { useEffect } from "react";
import { Card, Typography } from "antd";
import { useDispatch } from "@/redux";
import { setTabTitle } from "@/redux/modules/tabs";
import { useLocation, useParams } from "react-router-dom";

const { Title } = Typography;

const TabsDetail: React.FC = () => {
  const { id } = useParams();
  const { search } = useLocation();
  const query = new URLSearchParams(search).get("params");

  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(setTabTitle(`No.${id} - Tab 详情`));
  }, []);

  return (
    <Card>
      <Title level={4}> 我是 Tab 详情页</Title>
      <Title level={5}>params ：{id}</Title>
      {query && <Title level={5}>query ：{query}</Title>}
    </Card>
  );
};

export default TabsDetail;
