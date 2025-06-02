import React, { useEffect, useState, useCallback, useMemo } from "react";
import { Tabs, Card, Button, Checkbox, Collapse } from "antd";
import http from "@/api";
import type { CollapseProps, CheckboxProps } from "antd";
import { PageLoader } from "@/components";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";
import { some } from "@/utils";

const { TabPane } = Tabs;
const CheckboxGroup = Checkbox.Group;

// API 路径常量
const API_URL = "/api/SmRoleModule";

/**
 * 模块项类型定义
 */
interface ModuleItem {
  key: string;
  title: string;
  isLeaf?: boolean;
  children?: ModuleItem[];
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
 * 该组件用于设置角色对应的功能操作和后台管理权限，包括：
 * 1. 加载所有模块列表
 * 2. 获取角色已有权限
 * 3. 通过树形结构展示权限项
 * 4. 支持权限的选择与取消
 * 5. 保存权限设置
 *
 * @param props 组件属性
 */
const PermissionSet: React.FC<PermissionSetProps> = ({ id }) => {
  // 状态定义
  const [tabKey, setTabKey] = useState<string>("1");
  const [loading, setLoading] = useState<boolean>(true);
  const [modules, setModules] = useState<ModuleItem[]>([]);
  const [checkedModuleKeys, setCheckedModuleKeys] = useState<string[]>([]);

  /**
   * 获取角色模块权限
   */
  const fetchRoleModule = async (): Promise<void> => {
    const { Data, Success } = await http.get<any>(`${API_URL}/GetRoleModule/${id}`);
    if (Success) setCheckedModuleKeys(Data);
    setLoading(false);
  };

  /**
   * 获取所有模块列表
   */
  const fetchAllModuleList = async (): Promise<void> => {
    const { Data, Success } = await http.get<any>(`${API_URL}/GetAllModuleList`);
    if (Success) setModules(Data.children);
  };

  /**
   * 初始化数据
   */
  useEffect(() => {
    // 获取所有模块列表和角色模块权限
    fetchAllModuleList();
    fetchRoleModule();
  }, []);

  /**
   * 保存权限设置
   */
  const handleSave = async (): Promise<void> => {
    message.loading("数据提交中...", 0);
    setLoading(true);
    NProgress.start();

    try {
      const { Message, Success } = await http.post<{ Message: string; Success: boolean }>(
        `${API_URL}/UpdateRoleModule/${id}`,
        checkedModuleKeys
      );

      if (Success) message.success(Message);
    } finally {
      message.destroy();
      setLoading(false);
      NProgress.done();
    }
  };

  /**
   * 阻止Checkbox点击事件冒泡
   */
  const handleCheckboxClick: CheckboxProps["onClick"] = e => e.stopPropagation();

  /**
   * 切换标签页
   */
  const handleTabClick = (key: string): void => setTabKey(key);

  /**
   * 检查项目是否被选中
   * @param item 模块项
   * @returns 是否选中
   */
  const isItemChecked = useCallback(
    (item: ModuleItem): boolean => {
      if (checkedModuleKeys.length === 0) return false;
      return some(checkedModuleKeys, item.key);
    },
    [checkedModuleKeys]
  );

  /**
   * 获取组内选中的项
   * @param items 模块项数组
   * @returns 选中的键数组
   */
  const getGroupCheckedItems = useCallback(
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
   * 处理组内选中状态变化
   * @param checkedList 选中的键列表
   * @param parent 父模块项
   */
  const handleGroupChange = (checkedList: string[], parent: ModuleItem): void => {
    const newCheckedKeys = [...checkedModuleKeys];

    // 移除所有子项
    parent.children?.forEach((item: ModuleItem) => {
      const index = newCheckedKeys.findIndex(x => x === item.key);
      if (index !== -1) newCheckedKeys.splice(index, 1);
    });

    // 如果所有子项都选中，则添加父项
    if (checkedList.length === parent.children?.length) {
      newCheckedKeys.push(parent.key);
    } else {
      // 否则移除父项
      const parentIndex = newCheckedKeys.findIndex(x => x === parent.key);
      if (parentIndex !== -1) newCheckedKeys.splice(parentIndex, 1);
    }

    // 添加选中的子项
    setCheckedModuleKeys([...newCheckedKeys, ...checkedList]);
  };

  /**
   * 计算模块项的半选状态
   * @param item 模块项
   * @returns 是否为半选状态
   */
  const calculateIndeterminate = useCallback(
    (item: ModuleItem): boolean => {
      if (checkedModuleKeys.length === 0 || !item.children) return false;

      // 计算选中的子项数量
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

      // 如果部分选中（不是全部也不是零），则为半选状态
      return checkedCount !== 0 && checkedCount !== totalCount;
    },
    [checkedModuleKeys]
  );

  /**
   * 处理Checkbox选中状态变化
   * @param e 事件对象
   * @param item 模块项
   */
  const handleCheckChange = (e: React.ChangeEvent<HTMLInputElement>, item: ModuleItem): void => {
    const newCheckedKeys = [...checkedModuleKeys];

    // 移除当前项及其所有子项
    const removeCheckedKeys = (keys: string[], parent: ModuleItem): void => {
      const index = keys.findIndex(x => x === parent.key);
      if (index !== -1) keys.splice(index, 1);

      if (parent.children)
        parent.children.forEach((child: ModuleItem) => {
          removeCheckedKeys(keys, child);
        });
    };

    // 添加当前项及其所有子项
    const addCheckedKeys = (keys: string[], parent: ModuleItem): void => {
      keys.push(parent.key);

      if (parent.children)
        parent.children.forEach((child: ModuleItem) => {
          addCheckedKeys(keys, child);
        });
    };

    // 先移除所有相关项
    removeCheckedKeys(newCheckedKeys, item);

    // 如果选中，则添加所有相关项
    if (e.target.checked) addCheckedKeys(newCheckedKeys, item);

    setCheckedModuleKeys(newCheckedKeys);
  };

  /**
   * 渲染模块树
   * @param items 模块项数组
   * @param level 缩进级别
   * @returns 渲染的模块树
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
                  indeterminate={calculateIndeterminate(item)}
                  checked={isItemChecked(item)}
                  name={item.key}
                  onChange={(e: any) => handleCheckChange(e, item)}
                >
                  {item.title}
                </Checkbox>
              </div>

              {/* 渲染子项 */}
              {item.children &&
                !item.isLeaf &&
                item.children.length > 0 &&
                (item.children.some((child: ModuleItem) => child.isLeaf === false) ? (
                  renderModuleTree(item.children, level + 2)
                ) : (
                  <CheckboxGroup
                    style={{ marginLeft: (level + 2) * 20, marginTop: 5 }}
                    value={getGroupCheckedItems(item.children)}
                    options={item.children.map((child: ModuleItem) => ({
                      label: child.title,
                      value: child.key
                    }))}
                    onChange={(list: string[]) => handleGroupChange(list, item)}
                  />
                ))}
            </div>
          ))}
        </>
      );
    },
    [checkedModuleKeys, calculateIndeterminate, isItemChecked, getGroupCheckedItems, handleGroupChange, handleCheckChange]
  );

  /**
   * 生成折叠面板项
   */
  const collapseItems: CollapseProps["items"] = useMemo(() => {
    if (modules.length === 0) return [];

    return modules.map((module: ModuleItem) => ({
      key: module.key,
      label: (
        <Checkbox
          indeterminate={calculateIndeterminate(module)}
          onClick={handleCheckboxClick}
          checked={isItemChecked(module)}
          onChange={(e: any) => handleCheckChange(e, module)}
          name={module.key}
        >
          {module.title}
        </Checkbox>
      ),
      children: renderModuleTree(module.children || [], 2)
    }));
  }, [modules, checkedModuleKeys, renderModuleTree, calculateIndeterminate, isItemChecked, handleCheckChange]);

  return (
    <>
      {modules.length > 0 ? (
        <Card
          title="设置角色对应的功能操作、后台管理权限"
          className="card-small card-head"
          extra={
            <Button type="primary" onClick={handleSave} loading={loading}>
              保存
            </Button>
          }
          size="small"
          bordered={false}
          style={{ boxShadow: "initial" }}
        >
          <Tabs activeKey={tabKey} onTabClick={handleTabClick}>
            <TabPane tab="角色-功能权限" key="1">
              <Collapse
                bordered={false}
                ghost
                defaultActiveKey={modules.length > 0 ? [modules[0].key] : []}
                size="small"
                items={collapseItems}
              />
            </TabPane>
          </Tabs>
        </Card>
      ) : (
        <PageLoader />
      )}
    </>
  );
};

export default PermissionSet;
