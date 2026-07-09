import { useState } from "react";
import { Button, message } from "antd";
import { SaveOutlined, CheckOutlined } from "@ant-design/icons";
import { useSelector } from "@/redux";
import { RootState } from "@/redux";
import { updateMobilePage } from "@/api/modules/mobileConfig";

interface Props {
  configId?: string;
}

export default function SaveBtn({ configId }: Props) {
  const state = useSelector((s: RootState) => s.mobileEditor);
  const [loading, setLoading] = useState(false);
  const [saved, setSaved] = useState(false);

  const handleSave = async () => {
    if (!configId) {
      message.warning("缺少配置ID");
      return;
    }
    setLoading(true);
    try {
      const configJson = JSON.stringify({
        type: state.type,
        props: state.props,
        children: state.children
      });
      const res = await updateMobilePage(configId, { ConfigJson: configJson });
      if (res?.Success) {
        message.success("保存成功");
        setSaved(true);
        setTimeout(() => setSaved(false), 2000);
      } else {
        message.error(res?.Message || "保存失败");
      }
    } catch (err) {
      console.error("保存失败", err);
      message.error("保存失败");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button
      type="primary"
      icon={saved ? <CheckOutlined /> : <SaveOutlined />}
      loading={loading}
      onClick={handleSave}
      style={{
        borderRadius: 6,
        background: saved ? "#059669" : undefined,
        borderColor: saved ? "#059669" : undefined
      }}
    >
      {saved ? "已保存" : "保存"}
    </Button>
  );
}
