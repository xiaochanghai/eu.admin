import ThemeSettingsPanel from "./ThemeSettingsPanel";
import { defaultSystemTheme, getThemeConfigChanges, readThemeConfig, SystemThemeSettings } from "./themeConfig";
import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Card, Menu, Switch, Input, Select, InputNumber, Space, Button, Spin, Empty, Tag, List, Alert } from "antd";
import { SaveOutlined, ReloadOutlined, SearchOutlined } from "@ant-design/icons";
import { createConfig, getConfigListByGroup, updateConfig, SmConfigGroupView, SmConfigItem } from "@/api/modules/smConfig";
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

const THEME_GROUP = "system-theme";
const SystemSetup: React.FC = () => {
  const [themeDrafts, setThemeDrafts] = useState<Record<string, SystemThemeSettings>>({});
  const busyRef = useRef(false);
  const mountedRef = useRef(true);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [groups, setGroups] = useState<SmConfigGroupView[]>([]);
  const treeData = useMemo(() => groups.map(group => ({ key: group.ID, title: group.Name })), [groups]);
  const [selectedGroupId, setSelectedGroupId] = useState<string>("");
  const selectedGroup = groups.find(group => group.ID === selectedGroupId);
  const isThemeGroup = selectedGroup?.Type === THEME_GROUP;
  const themeDraft = themeDrafts[selectedGroupId] ?? defaultSystemTheme;
  const themeChanges = useMemo(
    () => (isThemeGroup && selectedGroup ? getThemeConfigChanges(selectedGroup, themeDraft) : []),
    [isThemeGroup, selectedGroup, themeDraft]
  );
  const changeTheme = useCallback(
    (patch: Partial<SystemThemeSettings>) => {
      if (busyRef.current) return;
      setThemeDrafts(prev => {
        const current = prev[selectedGroupId] ?? defaultSystemTheme;
        if (current.allowCustom && patch.allowCustom === undefined) return prev;
        return { ...prev, [selectedGroupId]: { ...current, ...patch } };
      });
    },
    [selectedGroupId]
  );
  const [formValues, setFormValues] = useState<Record<string, string>>({});
  const [dirtyKeys, setDirtyKeys] = useState<Set<string>>(new Set());
  const [searchText, setSearchText] = useState("");

  /** 加载数据 */
  const loadData = useCallback(async () => {
    if (busyRef.current) return;
    busyRef.current = true;
    setLoading(true);
    try {
      const res = await getConfigListByGroup();
      if (!mountedRef.current) return;
      if (!res.Success || !Array.isArray(res.Data)) throw new Error("读取系统配置失败");
      if (res.Success && res.Data) {
        setGroups(res.Data);
        setThemeDrafts(
          Object.fromEntries(
            res.Data.filter(group => group.Type === THEME_GROUP).map(group => [group.ID, readThemeConfig(group.detail)])
          )
        );
        setSelectedGroupId(res.Data[0]?.ID ?? "");
        setSearchText("");
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
      }
    } catch (error) {
      console.error("加载系统参数失败:", error);
      if (mountedRef.current) message.error("加载系统参数失败，请重试");
    } finally {
      busyRef.current = false;
      if (mountedRef.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    mountedRef.current = true;
    loadData();
    return () => {
      mountedRef.current = false;
    };
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
    if (busyRef.current || !selectedGroup || (isThemeGroup ? themeChanges.length === 0 : dirtyKeys.size === 0)) return;
    busyRef.current = true;
    setSaving(true);
    try {
      if (isThemeGroup) {
        // Reconcile IDs before creating: a previous POST may have succeeded despite a lost response.
        const latest = await getConfigListByGroup();
        if (!mountedRef.current) return;
        if (!latest.Success || !Array.isArray(latest.Data)) throw new Error("读取最新主题配置失败");
        const latestGroup = latest.Data.find(group => group.ID === selectedGroup.ID && group.Type === THEME_GROUP);
        if (!latestGroup) throw new Error("主题分组已不存在，请刷新页面");
        setGroups(prev => prev.map(group => (group.ID === latestGroup.ID ? latestGroup : group)));
        let failed = 0;
        for (const item of getThemeConfigChanges(latestGroup, themeDraft)) {
          if (!mountedRef.current) return;
          try {
            const result = item.ID ? await updateConfig(item) : await createConfig(item);
            if (!mountedRef.current) return;
            if (!result.Success) throw new Error(result.Message || "主题参数保存失败");
            const id = item.ID ?? result.Data;
            if (typeof id !== "string" || !id) throw new Error("新增参数未返回 ID，请刷新后重试");
            const saved: SmConfigItem = { ...item, ID: id };
            // Retain successful IDs immediately so retries update rather than insert duplicates.
            setGroups(prev =>
              prev.map(group =>
                group.ID === selectedGroup.ID
                  ? {
                      ...group,
                      detail: [...(group.detail ?? []).filter(config => config.ConfigCode !== item.ConfigCode), saved]
                    }
                  : group
              )
            );
          } catch {
            failed++;
          }
        }
        if (failed) message.error(`${failed} 项主题参数保存失败，请重试`);
        else message.success("主题配置保存成功");
        return;
      }
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
    } catch {
      if (mountedRef.current) message.error("保存失败，请刷新后重试");
    } finally {
      busyRef.current = false;
      if (mountedRef.current) setSaving(false);
    }
  }, [dirtyKeys, findConfigByCode, formValues, isThemeGroup, selectedGroup, themeChanges, themeDraft]);

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
  const currentGroupName = selectedGroup?.Name ?? "";

  return (
    <div style={{ padding: 16 }}>
      {/* 操作栏 */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <h3 style={{ margin: 0, fontSize: 16, fontWeight: 600 }}>系统设置</h3>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={loadData} disabled={loading || saving}>
            刷新
          </Button>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            disabled={loading || (isThemeGroup ? themeChanges.length === 0 : dirtyKeys.size === 0)}
            loading={saving}
            onClick={handleSave}
          >
            {`保存${(isThemeGroup ? themeChanges.length : dirtyKeys.size) > 0 ? ` (${isThemeGroup ? themeChanges.length : dirtyKeys.size})` : ""}`}
          </Button>
        </Space>
      </div>

      <Spin spinning={loading}>
        {treeData.length === 0 && !loading ? (
          <Card>
            <Empty description="暂无配置分组" />
          </Card>
        ) : (
          <div style={{ display: "flex", gap: 16 }}>
            {/* 左侧：分组菜单 */}
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
              <Menu
                mode="inline"
                items={treeData.map(({ key, title }) => ({ key, label: title }))}
                selectedKeys={selectedGroupId ? [selectedGroupId] : []}
                onClick={({ key }) => {
                  if (busyRef.current) return;
                  setSelectedGroupId(key);
                  setSearchText("");
                }}
                style={{ minHeight: 400, borderInlineEnd: 0 }}
              />
            </Card>

            {/* 右侧：参数列表 */}
            <Card style={{ flex: 1 }} title={currentGroupName || "请选择分组"}>
              {isThemeGroup ? (
                <div style={{ maxWidth: 620 }}>
                  <Alert
                    type="info"
                    showIcon
                    title="系统主题配置"
                    description="修改后点击保存。此处维护系统主题参数，不会即时改变当前个人主题。"
                  />
                  <div style={{ display: "flex", justifyContent: "space-between", margin: "24px 0" }}>
                    <span>允许用户自定义主题</span>
                    <Switch
                      checked={themeDraft.allowCustom}
                      disabled={saving || loading}
                      onChange={allowCustom => changeTheme({ allowCustom })}
                    />
                  </div>
                  <ThemeSettingsPanel
                    value={themeDraft}
                    onChange={changeTheme}
                    disabled={saving || loading || themeDraft.allowCustom}
                  />
                </div>
              ) : currentItems.length === 0 ? (
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
                            <div style={{ color: "#717171", fontSize: 12, lineHeight: "18px", marginTop: 2 }}>{item.Remark}</div>
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
