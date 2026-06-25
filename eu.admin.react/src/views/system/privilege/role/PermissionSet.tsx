import React, { useEffect, useState, useCallback, useMemo } from "react";
import { Tabs, Card, Button, Checkbox, Collapse } from "antd";
import http from "@/api";
import type { CollapseProps, CheckboxProps, TabsProps } from "@/typings";
import { PageLoader } from "@/components";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";
import { some } from "@/utils";

const CheckboxGroup = Checkbox.Group;

// API 路径常量
const MODULE_API_URL = "/api/SmRoleModule";
const DATA_SCOPE_API_URL = "/api/SmRoleDataScope";

/**
 * 模块项类型定义（功能权限）
 */
interface ModuleItem {
  key: string;
  title: string;
  isLeaf?: boolean;
  children?: ModuleItem[];
}

/**
 * 数据权限项类型定义
 */
interface DataScopeItem {
  key: string;
  title: string;
  isLeaf?: boolean;
  children?: DataScopeItem[];
}

/**
 * 权限设置组件属性
 */
interface PermissionSetProps {
  /** 角色ID */
  id: string | null;
}

/**
 * 权限设置组件
 *
 * 该组件用于设置角色对应的权限，包括：
 * 1. 功能权限：模块和功能操作的权限
 * 2. 数据权限：集团和公司的数据访问权限
 *
 * @param props 组件属性
 */
const PermissionSet: React.FC<PermissionSetProps> = ({ id }) => {
  // ========== 功能权限状态 ==========
  const [loading, setLoading] = useState<boolean>(true);
  const [modules, setModules] = useState<ModuleItem[]>([]);
  const [checkedModuleKeys, setCheckedModuleKeys] = useState<string[]>([]);

  // ========== 数据权限状态 ==========
  const [dataScopes, setDataScopes] = useState<DataScopeItem[]>([]);
  const [checkedDataScopeKeys, setCheckedDataScopeKeys] = useState<string[]>([]);
  const [dataScopeLoading, setDataScopeLoading] = useState<boolean>(false);

  // ========== 功能权限方法 ==========

  /**
   * 获取角色模块权限
   */
  const fetchRoleModule = useCallback(async (): Promise<void> => {
    if (!id) return;
    const { Data, Success } = await http.get<any>(`${MODULE_API_URL}/GetRoleModule/${id}`);
    if (Success) setCheckedModuleKeys(Data);
    setLoading(false);
  }, [id]);

  /**
   * 获取所有模块列表
   */
  const fetchAllModuleList = useCallback(async (): Promise<void> => {
    const { Data, Success } = await http.get<any>(`${MODULE_API_URL}/GetAllModuleList`);
    if (Success) setModules(Data.children);
  }, []);

  /**
   * 保存功能权限
   */
  const handleSaveModulePermission = useCallback(async (): Promise<void> => {
    message.loading("功能权限提交中...", 0);
    setLoading(true);
    NProgress.start();

    try {
      const { Message, Success } = await http.post<any>(`${MODULE_API_URL}/UpdateRoleModule/${id}`, checkedModuleKeys);
      message.destroy();
      if (Success) message.success(Message || "功能权限保存成功");
    } catch (error) {
      message.destroy();
      message.error("功能权限保存失败");
    } finally {
      setLoading(false);
      NProgress.done();
    }
  }, [id, checkedModuleKeys]);

  // ========== 数据权限方法 ==========

  /**
   * 获取角色数据权限
   */
  const fetchRoleDataScope = useCallback(async (): Promise<void> => {
    if (!id) return;
    const { Data, Success } = await http.get<any>(`${DATA_SCOPE_API_URL}/GetRoleDataScope/${id}`);
    if (Success) setCheckedDataScopeKeys(Data || []);
  }, [id]);

  /**
   * 获取所有数据权限树
   */
  const fetchAllDataScopeTree = useCallback(async (): Promise<void> => {
    const { Data, Success } = await http.get<any>(`${DATA_SCOPE_API_URL}/GetAllDataScopeTree`);
    // 后端直接返回集团列表
    if (Success) setDataScopes(Data || []);
  }, []);

  /**
   * 保存数据权限
   */
  const handleSaveDataScopePermission = useCallback(async (): Promise<void> => {
    message.loading("数据权限提交中...", 0);
    setDataScopeLoading(true);
    NProgress.start();

    try {
      const { Message, Success } = await http.post<any>(`${DATA_SCOPE_API_URL}/UpdateDataScope/${id}`, checkedDataScopeKeys);
      message.destroy();
      if (Success) message.success(Message || "数据权限保存成功");
    } catch (error) {
      message.destroy();
      message.error("数据权限保存失败");
    } finally {
      setDataScopeLoading(false);
      NProgress.done();
    }
  }, [id, checkedDataScopeKeys]);

  // ========== 初始化 ==========

  /**
   * 初始化数据
   */
  useEffect(() => {
    fetchAllModuleList();
    fetchRoleModule();
    fetchAllDataScopeTree();
    fetchRoleDataScope();
  }, [fetchAllModuleList, fetchRoleModule, fetchAllDataScopeTree, fetchRoleDataScope]);

  // ========== 功能权限辅助方法 ==========

  /**
   * 阻止Checkbox点击事件冒泡
   */
  const handleCheckboxClick: CheckboxProps["onClick"] = e => e.stopPropagation();

  /**
   * 检查功能权限项是否被选中
   * 如果所有子项都被选中，也视为选中
   */
  const isModuleItemChecked = useCallback(
    (item: ModuleItem): boolean => {
      if (checkedModuleKeys.length === 0) return false;

      // 检查自身是否被选中
      if (some(checkedModuleKeys, item.key)) return true;

      // 检查是否所有子项都被选中
      if (item.children && item.children.length > 0) {
        const allChildrenChecked = item.children.every((child: ModuleItem) => {
          return some(checkedModuleKeys, child.key);
        });
        if (allChildrenChecked) return true;
      }

      return false;
    },
    [checkedModuleKeys]
  );

  /**
   * 获取功能权限组内选中的项
   */
  const getModuleGroupCheckedItems = useCallback(
    (items: ModuleItem[]): string[] => {
      if (checkedModuleKeys.length === 0) return [];

      const checkedItems: string[] = [];
      items.forEach((item: ModuleItem) => {
        if (some(checkedModuleKeys, item.key)) {
          checkedItems.push(item.key);
        }
      });

      return checkedItems;
    },
    [checkedModuleKeys]
  );

  /**
   * 处理功能权限组内选中状态变化
   * 当所有子项都被选中时，自动勾选父项
   */
  const handleModuleGroupChange = useCallback(
    (checkedList: string[], parent: ModuleItem): void => {
      // 使用 Set 避免重复
      const newCheckedKeys = new Set(checkedModuleKeys);

      // 移除所有子项和父项
      parent.children?.forEach((item: ModuleItem) => {
        newCheckedKeys.delete(item.key);
      });
      newCheckedKeys.delete(parent.key);

      // 如果所有子项都被选中（且至少有一个子项），则添加父项
      if (parent.children && checkedList.length === parent.children.length && checkedList.length > 0) {
        newCheckedKeys.add(parent.key);
      }

      // 添加选中的子项
      checkedList.forEach(key => newCheckedKeys.add(key));

      setCheckedModuleKeys(Array.from(newCheckedKeys));
    },
    [checkedModuleKeys]
  );

  /**
   * 计算功能权限项的半选状态
   */
  const calculateModuleIndeterminate = useCallback(
    (item: ModuleItem): boolean => {
      if (checkedModuleKeys.length === 0 || !item.children) return false;

      let checkedCount = 0;
      let totalCount = 0;

      const countCheckedItems = (parent: ModuleItem): void => {
        if (parent.children) {
          totalCount += parent.children.length;
          parent.children.forEach((child: ModuleItem) => {
            if (some(checkedModuleKeys, child.key)) checkedCount++;
            if (child.children) countCheckedItems(child);
          });
        }
      };

      countCheckedItems(item);

      return checkedCount !== 0 && checkedCount !== totalCount;
    },
    [checkedModuleKeys]
  );

  /**
   * 处理功能权限Checkbox选中状态变化
   */
  const handleModuleCheckChange = useCallback(
    (e: any, item: ModuleItem): void => {
      const newCheckedKeys = [...checkedModuleKeys];

      const removeCheckedKeys = (keys: string[], parent: ModuleItem): void => {
        const index = keys.findIndex(x => x === parent.key);
        if (index !== -1) keys.splice(index, 1);

        if (parent.children)
          parent.children.forEach((child: ModuleItem) => {
            removeCheckedKeys(keys, child);
          });
      };

      const addCheckedKeys = (keys: string[], parent: ModuleItem): void => {
        keys.push(parent.key);

        if (parent.children)
          parent.children.forEach((child: ModuleItem) => {
            addCheckedKeys(keys, child);
          });
      };

      removeCheckedKeys(newCheckedKeys, item);

      if (e.target.checked) addCheckedKeys(newCheckedKeys, item);

      setCheckedModuleKeys(newCheckedKeys);
    },
    [checkedModuleKeys]
  );

  /**
   * 渲染功能权限模块树
   */
  const renderModuleTree = useCallback(
    (items: ModuleItem[], level = 0) => {
      return (
        <>
          {items.map((item: ModuleItem, index: number) => (
            <div key={item.key}>
              <div
                style={{
                  borderBottom: "1px solid #f0f0f0",
                  marginTop: index > 0 ? 10 : 0,
                  paddingBottom: 2
                }}
              >
                <Checkbox
                  style={{ marginLeft: level * 20 }}
                  indeterminate={calculateModuleIndeterminate(item)}
                  checked={isModuleItemChecked(item)}
                  name={item.key}
                  onChange={(e: any) => handleModuleCheckChange(e, item)}
                >
                  {item.title}
                </Checkbox>
              </div>

              {item.children &&
                !item.isLeaf &&
                item.children.length > 0 &&
                (item.children.some((child: ModuleItem) => child.isLeaf === false) ? (
                  renderModuleTree(item.children, level + 2)
                ) : (
                  <CheckboxGroup
                    style={{ marginLeft: (level + 2) * 20, marginTop: 5 }}
                    value={getModuleGroupCheckedItems(item.children)}
                    options={item.children.map((child: ModuleItem) => ({
                      label: child.title,
                      value: child.key
                    }))}
                    onChange={(list: string[]) => handleModuleGroupChange(list, item)}
                  />
                ))}
            </div>
          ))}
        </>
      );
    },
    [checkedModuleKeys, calculateModuleIndeterminate, isModuleItemChecked, getModuleGroupCheckedItems, handleModuleGroupChange, handleModuleCheckChange]
  );

  // ========== 数据权限辅助方法 ==========

  /**
   * 检查数据权限项是否被选中
   * 如果所有子项都被选中，也视为选中
   */
  const isDataScopeItemChecked = useCallback(
    (item: DataScopeItem): boolean => {
      if (checkedDataScopeKeys.length === 0) return false;

      // 检查是否所有子项都被选中
      if (item.children && item.children.length > 0) {
        const allChildrenChecked = item.children.every((child: DataScopeItem) => {
          return some(checkedDataScopeKeys, child.key);
        });
        if (allChildrenChecked) return true;
      }

      // 叶子节点直接判断自身
      if (!item.children || item.children.length === 0) {
        return some(checkedDataScopeKeys, item.key);
      }

      return false;
    },
    [checkedDataScopeKeys]
  );

  /**
   * 获取数据权限组内选中的项
   */
  const getDataScopeGroupCheckedItems = useCallback(
    (items: DataScopeItem[]): string[] => {
      if (checkedDataScopeKeys.length === 0) return [];

      const checkedItems: string[] = [];
      items.forEach((item: DataScopeItem) => {
        if (item.isLeaf && some(checkedDataScopeKeys, item.key)) {
          checkedItems.push(item.key);
        }
      });

      return checkedItems;
    },
    [checkedDataScopeKeys]
  );

  /**
   * 处理数据权限组内选中状态变化
   * 当所有子项（公司）都被选中时，自动勾选父项（集团）
   * 注意：只存储公司 ID，不存储集团 ID
   */
  const handleDataScopeGroupChange = useCallback(
    (checkedList: string[], parent: DataScopeItem): void => {
      // 使用 Set 避免重复
      const newCheckedKeys = new Set(checkedDataScopeKeys);

      // 只移除子项，父项不参与存储
      parent.children?.forEach((item: DataScopeItem) => {
        newCheckedKeys.delete(item.key);
      });

      // 添加选中的子项
      checkedList.forEach(key => newCheckedKeys.add(key));

      setCheckedDataScopeKeys(Array.from(newCheckedKeys));
    },
    [checkedDataScopeKeys]
  );

  /**
   * 计算数据权限项的半选状态
   */
  const calculateDataScopeIndeterminate = useCallback(
    (item: DataScopeItem): boolean => {
      if (checkedDataScopeKeys.length === 0 || !item.children) return false;

      let checkedCount = 0;
      let totalCount = 0;

      const countCheckedItems = (parent: DataScopeItem): void => {
        if (parent.children) {
          totalCount += parent.children.length;
          parent.children.forEach((child: DataScopeItem) => {
            if (some(checkedDataScopeKeys, child.key)) checkedCount++;
            if (child.children) countCheckedItems(child);
          });
        }
      };

      countCheckedItems(item);

      return checkedCount !== 0 && checkedCount !== totalCount;
    },
    [checkedDataScopeKeys]
  );

  /**
   * 处理数据权限Checkbox选中状态变化
   */
  const handleDataScopeCheckChange = useCallback(
    (e: any, item: DataScopeItem): void => {
      const newCheckedKeys = [...checkedDataScopeKeys];

      const removeCheckedKeys = (keys: string[], parent: DataScopeItem): void => {
        // 只移除公司 ID（叶子节点）
        if (parent.isLeaf) {
          const index = keys.findIndex(x => x === parent.key);
          if (index !== -1) keys.splice(index, 1);
        }

        if (parent.children)
          parent.children.forEach((child: DataScopeItem) => {
            removeCheckedKeys(keys, child);
          });
      };

      const addCheckedKeys = (keys: string[], parent: DataScopeItem): void => {
        // 只添加公司 ID（叶子节点）
        if (parent.isLeaf) {
          keys.push(parent.key);
        }

        if (parent.children)
          parent.children.forEach((child: DataScopeItem) => {
            addCheckedKeys(keys, child);
          });
      };

      removeCheckedKeys(newCheckedKeys, item);

      if (e.target.checked) addCheckedKeys(newCheckedKeys, item);

      setCheckedDataScopeKeys(newCheckedKeys);
    },
    [checkedDataScopeKeys]
  );

  /**
   * 渲染数据权限树
   */
  const renderDataScopeTree = useCallback(
    (items: DataScopeItem[], level = 0) => {
      return (
        <>
          {items.map((item: DataScopeItem, index: number) => (
            <div key={item.key}>
              <div
                style={{
                  borderBottom: "1px solid #f0f0f0",
                  marginTop: index > 0 ? 10 : 0,
                  paddingBottom: 2
                }}
              >
                <Checkbox
                  style={{ marginLeft: level * 20 }}
                  indeterminate={calculateDataScopeIndeterminate(item)}
                  checked={isDataScopeItemChecked(item)}
                  name={item.key}
                  onChange={(e: any) => handleDataScopeCheckChange(e, item)}
                >
                  {item.title}
                </Checkbox>
              </div>

              {item.children &&
                !item.isLeaf &&
                item.children.length > 0 &&
                (item.children.some((child: DataScopeItem) => child.isLeaf === false) ? (
                  renderDataScopeTree(item.children, level + 2)
                ) : (
                  <CheckboxGroup
                    style={{ marginLeft: (level + 2) * 20, marginTop: 5 }}
                    value={getDataScopeGroupCheckedItems(item.children)}
                    options={item.children.map((child: DataScopeItem) => ({
                      label: child.title,
                      value: child.key
                    }))}
                    onChange={(list: string[]) => handleDataScopeGroupChange(list, item)}
                  />
                ))}
            </div>
          ))}
        </>
      );
    },
    [checkedDataScopeKeys, calculateDataScopeIndeterminate, isDataScopeItemChecked, getDataScopeGroupCheckedItems, handleDataScopeGroupChange, handleDataScopeCheckChange]
  );

  // ========== 生成标签页 ==========

  /**
   * 生成功能权限折叠面板项
   */
  const moduleCollapseItems: CollapseProps["items"] = useMemo(() => {
    if (modules.length === 0) return [];

    return modules.map((module: ModuleItem) => ({
      key: module.key,
      label: (
        <Checkbox
          indeterminate={calculateModuleIndeterminate(module)}
          onClick={handleCheckboxClick}
          checked={isModuleItemChecked(module)}
          onChange={e => handleModuleCheckChange(e, module)}
          name={module.key}
        >
          {module.title}
        </Checkbox>
      ),
      children: renderModuleTree(module.children || [], 2)
    }));
  }, [modules, calculateModuleIndeterminate, isModuleItemChecked, handleCheckboxClick, handleModuleCheckChange, renderModuleTree]);

  /**
   * 生成数据权限折叠面板项
   */
  const dataScopeCollapseItems: CollapseProps["items"] = useMemo(() => {
    if (dataScopes.length === 0) return [];

    return dataScopes.map((scope: DataScopeItem) => ({
      key: scope.key,
      label: (
        <Checkbox
          indeterminate={calculateDataScopeIndeterminate(scope)}
          onClick={handleCheckboxClick}
          checked={isDataScopeItemChecked(scope)}
          onChange={e => handleDataScopeCheckChange(e, scope)}
          name={scope.key}
        >
          {scope.title}
        </Checkbox>
      ),
      children: renderDataScopeTree(scope.children || [], 2)
    }));
  }, [dataScopes, calculateDataScopeIndeterminate, isDataScopeItemChecked, handleCheckboxClick, handleDataScopeCheckChange, renderDataScopeTree]);

  /**
   * 生成标签页项
   */
  const tabItems: TabsProps["items"] = useMemo(
    () => [
      {
        key: "1",
        label: "功能权限",
        children: (
          <Card
            title="设置角色对应的功能操作、后台管理权限"
            className="card-small card-head"
            extra={
              <Button type="primary" onClick={handleSaveModulePermission} loading={loading}>
                保存
              </Button>
            }
            size="small"
            variant="borderless"
            style={{ boxShadow: "initial" }}
          >
            {modules.length > 0 ? (
              <Collapse
                bordered={false}
                ghost
                defaultActiveKey={modules.length > 0 ? [modules[0].key] : []}
                size="small"
                items={moduleCollapseItems}
              />
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
              <Button type="primary" onClick={handleSaveDataScopePermission} loading={dataScopeLoading}>
                保存
              </Button>
            }
            size="small"
            variant="borderless"
            style={{ boxShadow: "initial" }}
          >
            {dataScopes.length > 0 ? (
              <Collapse
                bordered={false}
                ghost
                defaultActiveKey={dataScopes.length > 0 ? [dataScopes[0].key] : []}
                size="small"
                items={dataScopeCollapseItems}
              />
            ) : (
              <PageLoader />
            )}
          </Card>
        )
      }
    ],
    [
      modules,
      dataScopes,
      loading,
      dataScopeLoading,
      moduleCollapseItems,
      dataScopeCollapseItems,
      handleSaveModulePermission,
      handleSaveDataScopePermission
    ]
  );

  return <Tabs defaultActiveKey="1" items={tabItems} />;
};

export default PermissionSet;
