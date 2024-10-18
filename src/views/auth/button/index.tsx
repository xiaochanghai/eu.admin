import useAuthButton from "@/hooks/useAuthButton";
import AuthButton from "@/components/AuthButton";
import { Alert, Button, Card, Divider, Space } from "antd";
import {
  DeleteOutlined,
  EditOutlined,
  PlusCircleOutlined,
  VerticalAlignBottomOutlined,
  VerticalAlignTopOutlined
} from "@ant-design/icons";

const AuthButtonPage: React.FC = () => {
  const { BUTTONS } = useAuthButton();

  return (
    <Card>
      <Alert message={`当前用户按钮权限：${JSON.stringify(Object.keys(BUTTONS))}`} type="success" showIcon />
      <Divider orientation="left" plain className="mt30">
        使用 Hooks 绑定按钮权限
      </Divider>
      <Space>
        {BUTTONS.add && (
          <Button type="primary" icon={<PlusCircleOutlined />}>
            新增
          </Button>
        )}
        {BUTTONS.edit && (
          <Button type="primary" icon={<EditOutlined />}>
            编辑
          </Button>
        )}
        {BUTTONS.delete && (
          <Button type="primary" icon={<DeleteOutlined />}>
            删除
          </Button>
        )}
        {BUTTONS.import && (
          <Button type="primary" icon={<VerticalAlignTopOutlined />}>
            导入数据
          </Button>
        )}
        {BUTTONS.export && (
          <Button type="primary" icon={<VerticalAlignBottomOutlined />}>
            导出数据
          </Button>
        )}
      </Space>

      <Divider orientation="left" plain className="mt30">
        使用 HOC 绑定按钮权限
      </Divider>
      <Space>
        <AuthButton authority="add">
          <Button type="primary" icon={<PlusCircleOutlined />}>
            新增
          </Button>
        </AuthButton>
        <AuthButton authority="edit">
          <Button type="primary" icon={<EditOutlined />}>
            编辑
          </Button>
        </AuthButton>
        <AuthButton authority="delete">
          <Button type="primary" icon={<DeleteOutlined />}>
            删除
          </Button>
        </AuthButton>
        <AuthButton authority="import">
          <Button type="primary" icon={<VerticalAlignTopOutlined />}>
            导入数据
          </Button>
        </AuthButton>
        <AuthButton authority="export">
          <Button type="primary" icon={<VerticalAlignBottomOutlined />}>
            导出数据
          </Button>
        </AuthButton>
      </Space>

      <Divider orientation="left" plain className="mt30">
        使用 HOC 绑定多个按钮权限
      </Divider>
      <Space>
        <AuthButton authority={["add", "edit", "delete", "import", "export"]}>
          <Button type="primary" icon={<PlusCircleOutlined />}>
            新增
          </Button>
        </AuthButton>
        <AuthButton authority={["add", "edit", "delete", "import", "export"]}>
          <Button type="primary" icon={<EditOutlined />}>
            编辑
          </Button>
        </AuthButton>
        <AuthButton authority={["add", "edit", "delete", "import", "export"]}>
          <Button type="primary" icon={<DeleteOutlined />}>
            删除
          </Button>
        </AuthButton>
        <AuthButton authority={["add", "edit", "delete", "import", "export"]}>
          <Button type="primary" icon={<VerticalAlignTopOutlined />}>
            导入数据
          </Button>
        </AuthButton>
        <AuthButton authority={["add", "edit", "delete", "import", "export"]}>
          <Button type="primary" icon={<VerticalAlignBottomOutlined />}>
            导出数据
          </Button>
        </AuthButton>
      </Space>
    </Card>
  );
};

export default AuthButtonPage;
