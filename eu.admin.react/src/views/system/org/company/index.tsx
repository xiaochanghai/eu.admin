import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
// import { AppstoreOutlined, BarsOutlined } from "@ant-design/icons";
import { Card, Empty, List, Space, Spin, Typography } from "antd";
import { TableList } from "@/components";
import { message } from "@/hooks/useMessage";
import { queryByFilter } from "@/api/modules/module";
import { ActionType } from "@/typings";

interface GroupRecord {
  ID: string;
  GroupCode?: string;
  GroupName?: string;
  Remark?: string | null;
}

const GROUP_MODULE_CODE = "SM_GROUP_MNG";
const COMPANY_MODULE_CODE = "SM_COMPANY_MNG";

const Index: React.FC = () => {
  const tableRef = useRef<ActionType>();
  const [groups, setGroups] = useState<GroupRecord[]>([]);
  const [groupLoading, setGroupLoading] = useState(false);
  const [selectedGroupId, setSelectedGroupId] = useState<string>("");
  // const [activeState, setActiveState] = useState<string>("1");

  const loadGroups = useCallback(async () => {
    setGroupLoading(true);
    try {
      const res = await queryByFilter(GROUP_MODULE_CODE, {}, null);

      const list: GroupRecord[] = (res as any)?.Data ?? (res as any)?.data ?? [];
      setGroups(list);
      setSelectedGroupId(current => current || list[0]?.ID || "");
    } catch (error) {
      message.error("集团数据加载失败");
    } finally {
      setGroupLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadGroups();
  }, [loadGroups]);

  const customConditions = useMemo(() => {
    const conditions: string[] = [];

    // if (activeState === "0" || activeState === "1") {
    //   conditions.push(`A.IsActive = '${activeState}'`);
    // }

    if (selectedGroupId) {
      const escapedGroupId = selectedGroupId.replace(/'/g, "''");
      conditions.push(`A.GroupId = '${escapedGroupId}'`);
    }

    return conditions.join(" AND ");
  }, [selectedGroupId]);

  useEffect(() => {
    tableRef.current?.reload(true);
  }, [customConditions]);

  const groupList = (
    <Card
      title="集团列表"
      size="small"
      style={{ height: "100%" }}
      styles={{ body: { padding: 12, height: "calc(100% - 57px)", overflow: "auto" } }}
    >
      <Spin spinning={groupLoading}>
        {groups.length === 0 ? (
          <Empty description="暂无集团数据" style={{ marginTop: 48 }} />
        ) : (
          <List
            dataSource={groups}
            renderItem={item => {
              const selected = item.ID === selectedGroupId;
              return (
                <List.Item
                  onClick={() => setSelectedGroupId(item.ID)}
                  style={{
                    cursor: "pointer",
                    padding: 12,
                    marginBottom: 8,
                    borderRadius: 8,
                    border: selected ? "1px solid #1677ff" : "1px solid #f0f0f0",
                    background: selected ? "#e6f4ff" : "#fff"
                  }}
                >
                  <Space direction="vertical" size={2} style={{ width: "100%" }}>
                    <Typography.Text strong>{item.GroupName || "Unnamed Group"}</Typography.Text>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      {item.GroupCode || "-"}
                    </Typography.Text>
                    {item.Remark ? (
                      <Typography.Text type="secondary" style={{ fontSize: 12 }} ellipsis>
                        {item.Remark}
                      </Typography.Text>
                    ) : null}
                  </Space>
                </List.Item>
              );
            }}
          />
        )}
      </Spin>
    </Card>
  );

  return (
    <div style={{ display: "flex", gap: 16, alignItems: "stretch" }}>
      <div style={{ width: 280, flexShrink: 0 }}>{groupList}</div>

      <div style={{ flex: 1, minWidth: 0 }}>
        <TableList
          moduleCode={COMPANY_MODULE_CODE}
          tableActionRef={tableRef}
          customConditions={customConditions}
        // expendAction={() => (
        //   <Segmented<string>
        //     options={[
        //       {
        //         label: "启用",
        //         value: "1",
        //         icon: <BarsOutlined />
        //       },
        //       {
        //         label: "停用",
        //         value: "0",
        //         icon: <AppstoreOutlined />
        //       }
        //     ]}
        //     value={activeState}
        //     onChange={value => setActiveState(value)}
        //   />
        // )}
        />
      </div>
    </div>
  );
};

export default Index;
