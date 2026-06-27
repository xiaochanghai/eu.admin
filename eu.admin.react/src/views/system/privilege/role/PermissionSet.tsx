import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Button, Card, Checkbox, Collapse, Empty, Tabs } from "antd";
import http from "@/api";
import type { CollapseProps, CheckboxProps, TabsProps } from "@/typings";
import { PageLoader } from "@/components";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";

const CheckboxGroup = Checkbox.Group;

const MODULE_API_URL = "/api/SmRoleModule";
const DATA_SCOPE_API_URL = "/api/SmRoleDataScope";
const TREE_INDENT = 20;

interface PermissionTreeItem {
  key: string;
  title: string;
  isLeaf?: boolean;
  children?: PermissionTreeItem[];
}

interface PermissionSetProps {
  id: string | null;
}

type CheckboxChangeEvent = Parameters<NonNullable<CheckboxProps["onChange"]>>[0];

const TREE_ROW_STYLE: React.CSSProperties = {
  borderBottom: "1px solid #f0f0f0",
  paddingBottom: 2
};

const hasChildren = (item: PermissionTreeItem): boolean => (item.children?.length ?? 0) > 0;

const hasNestedChildren = (items: PermissionTreeItem[]): boolean =>
  items.some(item => hasChildren(item));

const collectSelectedKeys = (item: PermissionTreeItem, leafOnly = false): string[] => {
  const keys: string[] = [];

  const visit = (node: PermissionTreeItem): void => {
    const selectable = !leafOnly || node.isLeaf === true;
    if (selectable) keys.push(node.key);
    node.children?.forEach(visit);
  };

  visit(item);
  return keys;
};

const isNodeChecked = (item: PermissionTreeItem, checkedSet: Set<string>): boolean => {
  if (checkedSet.has(item.key)) return true;

  const children = item.children ?? [];
  return children.length > 0 && children.every(child => isNodeChecked(child, checkedSet));
};

const isNodeIndeterminate = (item: PermissionTreeItem, checkedSet: Set<string>): boolean => {
  const children = item.children ?? [];
  if (children.length === 0) return false;

  const childChecked = children.some(
    child => isNodeChecked(child, checkedSet) || isNodeIndeterminate(child, checkedSet)
  );

  return childChecked && !isNodeChecked(item, checkedSet);
};

const toggleTreeSelection = (
  currentKeys: string[],
  item: PermissionTreeItem,
  checked: boolean,
  leafOnly = false
): string[] => {
  const nextKeys = new Set(currentKeys);
  const selectionKeys = collectSelectedKeys(item, leafOnly);

  selectionKeys.forEach(key => nextKeys.delete(key));

  if (checked) {
    selectionKeys.forEach(key => nextKeys.add(key));
  }

  return Array.from(nextKeys);
};

const getCheckedChildKeys = (items: PermissionTreeItem[], checkedSet: Set<string>): string[] =>
  items.filter(item => checkedSet.has(item.key)).map(item => item.key);

interface RenderTreeConfig {
  checkedSet: Set<string>;
  onNodeChange: (event: CheckboxChangeEvent, item: PermissionTreeItem) => void;
  onGroupChange: (checkedList: string[], parent: PermissionTreeItem) => void;
}

const renderTree = (
  items: PermissionTreeItem[],
  config: RenderTreeConfig,
  level = 0
): React.ReactNode => {
  return (
    <>
      {items.map((item, index) => {
        const children = item.children ?? [];

        return (
          <div key={item.key}>
            <div
              style={{
                ...TREE_ROW_STYLE,
                marginTop: index > 0 ? 10 : 0
              }}
            >
              <Checkbox
                style={{ marginLeft: level * TREE_INDENT }}
                indeterminate={isNodeIndeterminate(item, config.checkedSet)}
                checked={isNodeChecked(item, config.checkedSet)}
                name={item.key}
                onClick={event => event.stopPropagation()}
                onChange={event => config.onNodeChange(event, item)}
              >
                {item.title}
              </Checkbox>
            </div>

            {children.length > 0 &&
              !item.isLeaf &&
              (hasNestedChildren(children) ? (
                renderTree(children, config, level + 2)
              ) : (
                <CheckboxGroup
                  style={{ marginLeft: (level + 2) * TREE_INDENT, marginTop: 5 }}
                  value={getCheckedChildKeys(children, config.checkedSet)}
                  options={children.map(child => ({
                    label: child.title,
                    value: child.key
                  }))}
                  onChange={checkedList => config.onGroupChange(checkedList as string[], item)}
                />
              ))}
          </div>
        );
      })}
    </>
  );
};

async function savePermissions(
  url: string,
  payload: string[],
  loadingText: string,
  successText: string,
  failureText: string,
  setSaving: React.Dispatch<React.SetStateAction<boolean>>
): Promise<void> {
  message.loading(loadingText, 0);
  setSaving(true);
  NProgress.start();

  try {
    const { Message, Success } = await http.post<any>(url, payload);
    message.destroy();

    if (Success) {
      message.success(Message || successText);
    } else {
      message.error(Message || failureText);
    }
  } catch {
    message.destroy();
    message.error(failureText);
  } finally {
    setSaving(false);
    NProgress.done();
  }
}

const PermissionSet: React.FC<PermissionSetProps> = ({ id }) => {
  const [modules, setModules] = useState<PermissionTreeItem[]>([]);
  const [checkedModuleKeys, setCheckedModuleKeys] = useState<string[]>([]);
  const [moduleTreeLoaded, setModuleTreeLoaded] = useState(false);
  const [moduleSaving, setModuleSaving] = useState(false);

  const [dataScopes, setDataScopes] = useState<PermissionTreeItem[]>([]);
  const [checkedDataScopeKeys, setCheckedDataScopeKeys] = useState<string[]>([]);
  const [dataScopeTreeLoaded, setDataScopeTreeLoaded] = useState(false);
  const [dataScopeSaving, setDataScopeSaving] = useState(false);

  const moduleCheckedSet = useMemo(() => new Set(checkedModuleKeys), [checkedModuleKeys]);
  const dataScopeCheckedSet = useMemo(() => new Set(checkedDataScopeKeys), [checkedDataScopeKeys]);

  useEffect(() => {
    let active = true;

    const loadModulePermissionData = async (): Promise<void> => {
      setModuleTreeLoaded(false);

      if (!id) {
        if (active) {
          setModules([]);
          setCheckedModuleKeys([]);
          setModuleTreeLoaded(true);
        }
        return;
      }

      try {
        const [moduleTreeResult, roleModuleResult] = await Promise.all([
          http.get<any>(`${MODULE_API_URL}/GetAllModuleList`),
          http.get<any>(`${MODULE_API_URL}/GetRoleModule/${id}`)
        ]);

        if (!active) return;

        if (moduleTreeResult.Success) {
          setModules(moduleTreeResult.Data?.children ?? []);
        }

        if (roleModuleResult.Success) {
          setCheckedModuleKeys(roleModuleResult.Data ?? []);
        }
      } catch {
        if (active) {
          message.error("功能权限数据加载失败");
        }
      } finally {
        if (active) {
          setModuleTreeLoaded(true);
        }
      }
    };

    const loadDataScopePermissionData = async (): Promise<void> => {
      setDataScopeTreeLoaded(false);

      if (!id) {
        if (active) {
          setDataScopes([]);
          setCheckedDataScopeKeys([]);
          setDataScopeTreeLoaded(true);
        }
        return;
      }

      try {
        const [dataScopeTreeResult, roleDataScopeResult] = await Promise.all([
          http.get<any>(`${DATA_SCOPE_API_URL}/GetAllDataScopeTree`),
          http.get<any>(`${DATA_SCOPE_API_URL}/QueryRole/${id}`)
        ]);

        if (!active) return;

        if (dataScopeTreeResult.Success) {
          setDataScopes(dataScopeTreeResult.Data ?? []);
        }

        if (roleDataScopeResult.Success) {
          setCheckedDataScopeKeys(roleDataScopeResult.Data ?? []);
        }
      } catch {
        if (active) {
          message.error("数据权限加载失败");
        }
      } finally {
        if (active) {
          setDataScopeTreeLoaded(true);
        }
      }
    };

    void loadModulePermissionData();
    void loadDataScopePermissionData();

    return () => {
      active = false;
    };
  }, [id]);

  const handleModuleCheckChange = useCallback((event: CheckboxChangeEvent, item: PermissionTreeItem): void => {
    setCheckedModuleKeys(current =>
      toggleTreeSelection(current, item, event.target.checked)
    );
  }, []);

  const handleModuleGroupChange = useCallback((checkedList: string[], parent: PermissionTreeItem): void => {
    setCheckedModuleKeys(current => {
      const nextKeys = new Set(current);

      parent.children?.forEach(child => {
        nextKeys.delete(child.key);
      });

      checkedList.forEach(key => nextKeys.add(key));

      if (parent.children && checkedList.length === parent.children.length && checkedList.length > 0) {
        nextKeys.add(parent.key);
      } else {
        nextKeys.delete(parent.key);
      }

      return Array.from(nextKeys);
    });
  }, []);

  const handleDataScopeCheckChange = useCallback((event: CheckboxChangeEvent, item: PermissionTreeItem): void => {
    setCheckedDataScopeKeys(current =>
      toggleTreeSelection(current, item, event.target.checked, true)
    );
  }, []);

  const handleDataScopeGroupChange = useCallback((checkedList: string[], parent: PermissionTreeItem): void => {
    setCheckedDataScopeKeys(current => {
      const nextKeys = new Set(current);

      parent.children?.forEach(child => {
        nextKeys.delete(child.key);
      });

      checkedList.forEach(key => nextKeys.add(key));

      return Array.from(nextKeys);
    });
  }, []);

  const handleSaveModulePermission = useCallback(() => {
    if (!id) return;

    void savePermissions(
      `${MODULE_API_URL}/UpdateRoleModule/${id}`,
      checkedModuleKeys,
      "功能权限提交中...",
      "功能权限保存成功",
      "功能权限保存失败",
      setModuleSaving
    );
  }, [id, checkedModuleKeys]);

  const handleSaveDataScopePermission = useCallback(() => {
    if (!id) return;

    void savePermissions(
      `${DATA_SCOPE_API_URL}/UpdateDataScope/${id}`,
      checkedDataScopeKeys,
      "数据权限提交中...",
      "数据权限保存成功",
      "数据权限保存失败",
      setDataScopeSaving
    );
  }, [id, checkedDataScopeKeys]);

  const moduleCollapseItems: CollapseProps["items"] = useMemo(() => {
    if (modules.length === 0) return [];

    return modules.map(module => ({
      key: module.key,
      label: (
        <Checkbox
          indeterminate={isNodeIndeterminate(module, moduleCheckedSet)}
          checked={isNodeChecked(module, moduleCheckedSet)}
          name={module.key}
          onClick={event => event.stopPropagation()}
          onChange={event => handleModuleCheckChange(event, module)}
        >
          {module.title}
        </Checkbox>
      ),
      children: renderTree(module.children ?? [], {
        checkedSet: moduleCheckedSet,
        onNodeChange: handleModuleCheckChange,
        onGroupChange: handleModuleGroupChange
      }, 2)
    }));
  }, [modules, moduleCheckedSet, handleModuleCheckChange, handleModuleGroupChange]);

  const dataScopeCollapseItems: CollapseProps["items"] = useMemo(() => {
    if (dataScopes.length === 0) return [];

    return dataScopes.map(scope => ({
      key: scope.key,
      label: (
        <Checkbox
          indeterminate={isNodeIndeterminate(scope, dataScopeCheckedSet)}
          checked={isNodeChecked(scope, dataScopeCheckedSet)}
          name={scope.key}
          onClick={event => event.stopPropagation()}
          onChange={event => handleDataScopeCheckChange(event, scope)}
        >
          {scope.title}
        </Checkbox>
      ),
      children: renderTree(scope.children ?? [], {
        checkedSet: dataScopeCheckedSet,
        onNodeChange: handleDataScopeCheckChange,
        onGroupChange: handleDataScopeGroupChange
      }, 2)
    }));
  }, [dataScopes, dataScopeCheckedSet, handleDataScopeCheckChange, handleDataScopeGroupChange]);

  const tabItems: TabsProps["items"] = useMemo(
    () => [
      {
        key: "1",
        label: "功能权限",
        children: (
          <Card
            title="设置角色对应的功能操作和后台管理权限"
            className="card-small card-head"
            extra={
              <Button type="primary" onClick={handleSaveModulePermission} loading={moduleSaving}>
                保存
              </Button>
            }
            size="small"
            variant="borderless"
            style={{ boxShadow: "initial" }}
          >
            {moduleTreeLoaded ? (
              modules.length > 0 ? (
                <Collapse
                  bordered={false}
                  ghost
                  defaultActiveKey={modules.length > 0 ? [modules[0].key] : []}
                  size="small"
                  items={moduleCollapseItems}
                />
              ) : (
                <Empty description="暂无功能权限数据" />
              )
            ) : (
              <PageLoader />
            )}
          </Card>
        )
      },
      {
        key: "2",
        label: "数据权限",
        children: (
          <Card
            title="设置角色对应的数据权限（集团和公司）"
            className="card-small card-head"
            extra={
              <Button type="primary" onClick={handleSaveDataScopePermission} loading={dataScopeSaving}>
                保存
              </Button>
            }
            size="small"
            variant="borderless"
            style={{ boxShadow: "initial" }}
          >
            {dataScopeTreeLoaded ? (
              dataScopes.length > 0 ? (
                <Collapse
                  bordered={false}
                  ghost
                  defaultActiveKey={dataScopes.length > 0 ? [dataScopes[0].key] : []}
                  size="small"
                  items={dataScopeCollapseItems}
                />
              ) : (
                <Empty description="暂无数据权限数据" />
              )
            ) : (
              <PageLoader />
            )}
          </Card>
        )
      }
    ],
    [
      moduleTreeLoaded,
      dataScopeTreeLoaded,
      modules,
      dataScopes,
      moduleCollapseItems,
      dataScopeCollapseItems,
      handleSaveModulePermission,
      handleSaveDataScopePermission,
      moduleSaving,
      dataScopeSaving
    ]
  );

  return <Tabs defaultActiveKey="1" items={tabItems} />;
};

export default PermissionSet;
