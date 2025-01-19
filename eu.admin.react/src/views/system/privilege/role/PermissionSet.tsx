import React, { useEffect, useState } from "react";
import { Tabs, Card, Button, Checkbox, Collapse } from "antd";
import { useDispatch, RootState, useSelector } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";
import http from "@/api";
import type { CollapseProps, CheckboxProps } from "antd";
import { PageLoader } from "@/components/Loading/index";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";
import { some } from "@/utils";

const { TabPane } = Tabs;
const CheckboxGroup = Checkbox.Group;
let moduleCode = "BD_MATERIAL_TYPE_MNG";
let url = "/api/SmRoleModule";

const PermissionSet: React.FC<any> = props => {
  const dispatch = useDispatch();
  const [tabKey, setTabKey] = useState<any>();
  const [loading, setLoading] = useState<boolean>(true);
  const [modules, setModules] = useState<any>([]);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  const [checkedModuleKeys, setCheckedModuleKeys] = useState<any>([]);
  const { id } = props;

  let i = 0;
  let total = 0;
  useEffect(() => {
    if (!moduleInfo) getModuleInfo1();

    getAllModuleList();
    getRoleModule();
  }, []);
  const getModuleInfo1 = async () => {
    let { Data } = await getModuleInfo(moduleCode);
    dispatch(setModuleInfo(Data));
  };
  const getRoleModule = async () => {
    let { Data, Success } = await http.get<any>(url + "/GetRoleModule/" + id);
    if (Success) setCheckedModuleKeys(Data);
    setLoading(false);
  };
  const onClick: CheckboxProps["onClick"] = e => {
    e.stopPropagation();
  };
  const onTabClick = async (key: any) => setTabKey(key);
  const getAllModuleList = async () => {
    let { Data, Success } = await http.get<any>(url + "/GetAllModuleList");
    if (Success) setModules(Data.children);
  };
  const save = async () => {
    message.loading("数据提交中...", 0);
    setLoading(true);
    NProgress.start();

    let { Message, Success } = await http.post<any>(url + "/UpdateRoleModule/" + id, checkedModuleKeys);
    message.destroy();
    setLoading(false);
    NProgress.done();
    if (Success) message.success(Message);
  };

  const getChecked = (item: any) => {
    // let checked: boolean = false;
    if (checkedModuleKeys.length == 0) return false;
    return some(checkedModuleKeys, item.key);
  };
  const getGroupChecked = (items: any[]) => {
    let list: string[] = [];
    if (checkedModuleKeys.length == 0) return list;
    items.map((item: any) => {
      if (some(checkedModuleKeys, item.key)) list.push(item.key);
    });
    return list;
  };
  const onGroupChange = (list: string[], parent: any) => {
    let keys = [...checkedModuleKeys];
    parent.children.map((item: any) => {
      let index = keys.findIndex(x => x === item.key);
      if (index !== -1) keys.splice(index, 1);
    });
    if (list.length == parent.children.length) keys.push(parent.key);
    else {
      let index = keys.findIndex(x => x === parent.key);
      if (index !== -1) keys.splice(index, 1);
    }
    keys = [...keys, ...list];
    setCheckedModuleKeys(keys);
  };
  const getIndeterminate = (item: any) => {
    if (checkedModuleKeys.length == 0) return false;
    let result = setIndeterminateCount(0, 0, item);
    if (result.i != result.total && result.i != 0) return true;
    else false;
  };
  const setIndeterminateCount = (i: number, total: number, parent: any) => {
    if (parent.children) {
      total = total + parent.children.length;
      parent.children.map((item: any) => {
        if (some(checkedModuleKeys, item.key)) i++;
        if (item.children) return setIndeterminateCount(i, total, item);
      });
    }
    return { i, total };
  };
  const getIndeterminate1 = (item: any) => {
    i = 0;
    total = 0;
    if (checkedModuleKeys.length == 0) return false;
    setIndeterminateCount1(item);
    if (i != total && i != 0) return true;
    else false;
  };
  const setIndeterminateCount1 = (parent: any) => {
    if (parent.children) {
      total = total + parent.children.length;
      parent.children.map((item: any) => {
        if (some(checkedModuleKeys, item.key)) i++;
        if (item.children) return setIndeterminateCount1(item);
      });
    }
  };
  const onCheckChange = (e: any, parent: any) => {
    let keys = [...checkedModuleKeys];
    removeCheckedKeys(keys, parent);

    if (e.target.checked) addCheckedKeys(keys, parent);
    setCheckedModuleKeys(keys);
  };

  const removeCheckedKeys = (keys: string[], parent: any) => {
    let index = keys.findIndex(x => x === parent.key);
    if (index !== -1) keys.splice(index, 1);

    if (parent.children)
      parent.children.map((item: any) => {
        removeCheckedKeys(keys, item);
      });
  };

  const addCheckedKeys = (keys: string[], item: any) => {
    keys.push(item.key);
    if (item.children)
      item.children.map((item: any) => {
        addCheckedKeys(keys, item);
      });
  };

  const getItems: () => CollapseProps["items"] = () => {
    if (modules.length > 0)
      return modules?.map((child: any) => {
        return {
          key: child.key,
          label: (
            <Checkbox
              indeterminate={getIndeterminate1(child)}
              onClick={onClick}
              checked={getChecked(child)}
              onChange={(e: any) => onCheckChange(e, child)}
              name={child.key}
            >
              {child.title}
            </Checkbox>
          ),
          children: component(child.children, 2)
          // style: panelStyle
        };
      });

    return [];
  };
  const component = (list: any[], level = 0) => {
    return (
      <>
        {list.map((item: any, index: any) => {
          return (
            <div key={index}>
              <div style={{ borderBottom: "1px solid #f0f0f0", marginTop: index > 0 ? 10 : 0, paddingBottom: 2 }}>
                <Checkbox
                  style={{ marginLeft: level * 20 }}
                  indeterminate={getIndeterminate(item)}
                  checked={getChecked(item)}
                  name={item.key}
                  onChange={(e: any) => onCheckChange(e, item)}
                >
                  {item.title}
                </Checkbox>
              </div>
              {item.children != null && item.isLeaf === false && item.children.length > 0 ? (
                item.children.some((x: { isLeaf: boolean }) => x.isLeaf === false) ? (
                  component(item.children, level + 2)
                ) : (
                  <CheckboxGroup
                    style={{ marginLeft: (level + 2) * 20, marginTop: 5 }}
                    value={getGroupChecked(item.children)}
                    options={item.children.map((child: any) => {
                      return { label: child.title, value: child.key };
                    })}
                    onChange={(list: string[]) => onGroupChange(list, item)}
                  />
                )
              ) : null}
              {/* {item.children != null &&
              item.isLeaf === false &&
              item.children.length > 0 &&
              item.children.some((x: { isLeaf: boolean }) => x.isLeaf === false)
                ? component(item.children, level + 1)
                : null} */}
            </div>
          );
        })}
      </>
    );
  };
  return (
    <>
      {modules.length > 0 ? (
        <Card
          title="设置角色对应的功能操作、后台管理权限"
          className="card-small card-head"
          extra={
            <Button type="primary" onClick={save} loading={loading}>
              保存
            </Button>
          }
          size="small"
          bordered={false}
          style={{ boxShadow: "initial" }}
        >
          <Tabs activeKey={tabKey} onTabClick={onTabClick}>
            <TabPane tab="角色-功能权限" key="1">
              <Collapse
                bordered={false}
                ghost
                defaultActiveKey={modules.length > 0 ? [modules[0].key] : []}
                size="small"
                // style={{ backgroundColor: "transparent" }}
                // expandIcon={({ isActive }) => <CaretRightOutlined rotate={isActive ? 90 : 0} />}
                items={getItems()}
              />

              {/* <>{component(modules, 0)}</> */}
            </TabPane>
            {/* <TabPane tab="数据浏览" key="2"></TabPane> */}
          </Tabs>
        </Card>
      ) : (
        <PageLoader />
      )}
    </>
  );
};

export default PermissionSet;
