import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Card, Tree, Switch, Input, Select, InputNumber, Space, Button, Spin, Empty, Tag, List } from "antd";
import { SaveOutlined, ReloadOutlined, SearchOutlined } from "@ant-design/icons";
import { getConfigListByGroup, updateConfig, SmConfigGroupView, SmConfigItem } from "@/api/modules/smConfig";
import { message } from "@/hooks/useMessage";

const { TextArea } = Input;

/** 解析 AvailableValue: "标签1:值1;标签2:值2" => [{label, value}] */
const parseAvailableValue = (availableValue?: string): { label: string; value: string }[] => {
  if (!availableValue) return [];
  return availableValue
    .split(";")
    .filter(s => s.trim())
    .map(item => {
      const [label, value] = item.split(":");
      return { label: label?.trim() ?? "", value: value?.trim() ?? "" };
    });
};

const SystemSetup: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [groups, setGroups] = useState<SmConfigGroupView[]>([]);
  const [treeData, setTreeData] = useState<{ key: string; title: string }[]>([]);
  const [selectedGroupId, setSelectedGroupId] = useState<string>("");
  const [formValues, setFormValues] = useState<Record<string, string>>({});
  const [dirtyKeys, setDirtyKeys] = useState<Set<string>>(new Set());
  const [searchText, setSearchText] = useState("");

  /** 加载数据 */
  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getConfigListByGroup();
      if (res.Success && res.Data) {
        setGroups(res.Data);
        setTreeData(res.Data.map(g => ({ key: g.ID, title: g.Name })));
        // 初始化表单值
        const values: Record<string, string> = {};
        res.Data.forEach(g => {
          g.detail?.forEach(item => {
            if (item.ConfigCode) {
              values[item.ConfigCode] = item.ConfigValue ?? "";
            }
          });
        });
        setFormValues(values);
        setDirtyKeys(new Set());
        // 默认选中第一个分组
        if (res.Data.length > 0 && !selectedGroupId) {
          setSelectedGroupId(res.Data[0].ID);
        }
      }
    } catch (error) {
      console.error("加载系统参数失败:", error);
    } finally {
      setLoading(false);
    }
  }, [selectedGroupId]);

  useEffect(() => {
    loadData();
  }, []);

  /** 当前选中分组的配置项（支持搜索过滤） */
  const currentItems = useMemo(() => {
    const group = groups.find(g => g.ID === selectedGroupId);
    const items = group?.detail ?? [];
    if (!searchText) return items;
    const keyword = searchText.toLowerCase();
    return items.filter(
      item =>
        item.ConfigName?.toLowerCase().includes(keyword) ||
        item.ConfigCode?.toLowerCase().includes(keyword) ||
        item.Remark?.toLowerCase().includes(keyword)
    );
  }, [groups, selectedGroupId, searchText]);

  /** 值变更 */
  const handleValueChange = useCallback((configCode: string, value: string) => {
    setFormValues(prev => ({ ...prev, [configCode]: value }));
    setDirtyKeys(prev => {
      const next = new Set(prev);
      next.add(configCode);
      return next;
    });
  }, []);

  /** 根据 ConfigCode 查找原始配置项 */
  const findConfigByCode = useCallback(
    (code: string): SmConfigItem | undefined => {
      for (const g of groups) {
        const item = g.detail?.find(d => d.ConfigCode === code);
        if (item) return item;
      }
      return undefined;
    },
    [groups]
  );

  /** 保存所有修改 */
  const handleSave = useCallback(async () => {
    if (dirtyKeys.size === 0) return;
    setSaving(true);
    try {
      const toUpdate: Partial<SmConfigItem>[] = [];
      dirtyKeys.forEach(code => {
        const original = findConfigByCode(code);
        if (original) {
          toUpdate.push({ ...original, ConfigValue: formValues[code] ?? "" });
        }
      });

      let hasError = false;
      for (const item of toUpdate) {
        try {
          await updateConfig(item);
        } catch {
          hasError = true;
        }
      }

      if (hasError) {
        message.error("部分参数保存失败");
      } else {
        message.success("保存成功");
        setDirtyKeys(new Set());
      }
    } finally {
      setSaving(false);
    }
  }, [dirtyKeys, findConfigByCode, formValues]);

  /** 渲染单个配置项的控件 */
  const renderControl = useCallback(
    (item: SmConfigItem) => {
      const value = formValues[item.ConfigCode] ?? "";

      switch (item.InputType) {
        case "SWITCH":
          return (
            <Switch
              checked={value === "Y" || value === "true" || value === "1"}
              checkedChildren="是"
              unCheckedChildren="否"
              onChange={checked => handleValueChange(item.ConfigCode, checked ? "Y" : "N")}
            />
          );

        case "SELECT": {
          const options = parseAvailableValue(item.AvailableValue);
          return (
            <Select
              value={value || undefined}
              placeholder={`请选择${item.ConfigName}`}
              allowClear
              style={{ width: 260 }}
              options={options}
              onChange={val => handleValueChange(item.ConfigCode, val ?? "")}
            />
          );
        }

        case "NUMBER":
          return (
            <InputNumber
              value={value ? Number(value) : undefined}
              style={{ width: 260 }}
              onChange={val => handleValueChange(item.ConfigCode, val != null ? String(val) : "")}
            />
          );

        case "TEXTAREA":
          return (
            <TextArea
              value={value}
              rows={2}
              placeholder={`请输入${item.ConfigName}`}
              style={{ width: 260 }}
              onChange={e => handleValueChange(item.ConfigCode, e.target.value)}
            />
          );

        case "INPUT":
        default:
          return (
            <Input
              value={value}
              placeholder={`请输入${item.ConfigName}`}
              style={{ width: 260 }}
              onChange={e => handleValueChange(item.ConfigCode, e.target.value)}
            />
          );
      }
    },
    [formValues, handleValueChange]
  );

  /** 当前选中的分组名称 */
  const currentGroupName = groups.find(g => g.ID === selectedGroupId)?.Name ?? "";

  return (
    <div style={{ padding: 16 }}>
      {/* 操作栏 */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>系统参数设置</h3>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={loadData}>
            刷新
          </Button>
          <Button type="primary" icon={<SaveOutlined />} disabled={dirtyKeys.size === 0} loading={saving} onClick={handleSave}>
            保存{dirtyKeys.size > 0 && ` (${dirtyKeys.size})`}
          </Button>
        </Space>
      </div>

      <Spin spinning={loading}>
        {groups.length === 0 && !loading ? (
          <Card>
            <Empty description="暂无配置分组" />
          </Card>
        ) : (
          <div style={{ display: "flex", gap: 16 }}>
            {/* 左侧：树形菜单 */}
            <Card style={{ width: 240, flexShrink: 0 }} styles={{ body: { padding: 12 } }}>
              <Input
                placeholder="搜索参数"
                prefix={<SearchOutlined style={{ color: "#bbb" }} />}
                allowClear
                size="small"
                style={{ marginBottom: 8 }}
                value={searchText}
                onChange={e => setSearchText(e.target.value)}
              />
              <Tree
                treeData={treeData}
                selectedKeys={selectedGroupId ? [selectedGroupId] : []}
                onSelect={(keys: React.Key[]) => {
                  if (keys.length > 0) {
                    setSelectedGroupId(String(keys[0]));
                    setSearchText("");
                  }
                }}
                style={{ minHeight: 400 }}
              />
            </Card>

            {/* 右侧：参数列表 */}
            <Card style={{ flex: 1 }} title={currentGroupName || "请选择分组"}>
              {currentItems.length === 0 ? (
                <Empty description={searchText ? "无匹配结果" : "暂无参数配置"} />
              ) : (
                <List
                  dataSource={currentItems}
                  renderItem={item => {
                    const isDirty = dirtyKeys.has(item.ConfigCode);
                    return (
                      <List.Item
                        style={{
                          display: "flex",
                          justifyContent: "space-between",
                          alignItems: "center",
                          padding: "12px 0",
                          borderBottom: "1px solid #f0f0f0"
                        }}
                      >
                        {/* 左侧：名称 + 备注 */}
                        <div style={{ flex: 1, paddingRight: 24, minWidth: 0 }}>
                          <div style={{ fontWeight: 600, color: "#262626", lineHeight: "22px" }}>
                            {item.ConfigName}
                            {isDirty && (
                              <Tag color="orange" style={{ marginLeft: 8 }}>
                                未保存
                              </Tag>
                            )}
                          </div>
                          {item.Remark && (
                            <div style={{ color: "#717171", fontSize: 12, lineHeight: "18px", marginTop: 2 }}>
                              {item.Remark}
                            </div>
                          )}
                        </div>

                        {/* 右侧：控件 */}
                        <div style={{ flexShrink: 0 }}>{renderControl(item)}</div>
                      </List.Item>
                    );
                  }}
                />
              )}
            </Card>
          </div>
        )}
      </Spin>
    </div>
  );
};

export default SystemSetup;
