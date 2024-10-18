import React, { useEffect, useState } from "react";
import { Row, Col, Tree, Tabs, Space, Button, Card, Popconfirm } from "antd";
import { DownOutlined, PlusOutlined, DeleteOutlined } from "@ant-design/icons";
import { useDispatch } from "@/redux";
import { RootState, useSelector } from "@/redux";
// import TableList from "../../system/common/components/TableList";
import { ModuleInfo } from "@/api/interface/index";
import SmProTable from "@/components/ProTable";
import FormPage from "./FormPage";
import http from "@/api";
import { getModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";

const { TabPane } = Tabs;
let moduleCode = "BD_MATERIAL_TYPE_MNG";
const ImportTemplate: React.FC<any> = () => {
  const dispatch = useDispatch();
  const [treeData, setTreeData] = useState<any>([]);
  const [selectedKeys, setSelectedKeys] = useState<any>();
  const [parentTypeId, setParentTypeId] = useState<any>();
  const [Id, setId] = useState<any>();
  const [tabKey, setTabKey] = useState<any>();

  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  // const ids = useSelector((state: RootState) => state.module.ids);
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  useEffect(() => {
    getAllMaterialType();
    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) getModuleInfo1();
  }, []);

  const onSelect = async (value: any[]) => {
    if (value[0] != "All") {
      setSelectedKeys(value[0]);
      setId(value[0]);
      setTabKey("1");
    } else {
      setSelectedKeys("");

      setId("");
      setTabKey("1");
    }
    // console.log('selected', selectedKeys, info);
  };
  const getAllMaterialType = async () => {
    let { Data, Success } = await http.get<any>("/api/MaterialType/GetAllMaterialType");
    if (Success) {
      // let keys: any[] = [];
      // Data.map(
      //   (item: any) => keys.push(item.SmRoleId) //item表示数组中的每一个元素
      // );
      setTreeData([Data]);
    }
  };
  const onTabClick = async (key: any) => setTabKey(key);
  return (
    <Card size="small" bordered={false}>
      <Row gutter={[16, 16]} style={{ background: "#fff" }}>
        <Col span={6}>
          {treeData.length > 0 ? (
            <>
              <Space style={{ display: "flex", paddingTop: 12 }}>
                <Button
                  type="primary"
                  onClick={() => {
                    if (selectedKeys) {
                      setParentTypeId(selectedKeys);
                    }
                    setId("");
                    setTabKey("1");
                  }}
                >
                  <PlusOutlined /> 新建
                </Button>
                {Id ? (
                  <>
                    <Popconfirm
                      title="删除提醒"
                      description="是否确定删除该分类?"
                      onConfirm={async () => {
                        await http.delete<any>("/api/MaterialType/" + Id);
                        setSelectedKeys("");

                        setId("");
                        getAllMaterialType();

                        // dispatch({
                        //   type: "materialtype/delete",
                        //   payload: Id
                        // }).then(result => {
                        //   me.setState({ Id: null });
                        //   dispatch({
                        //     type: "materialtype/getAllMaterialType"
                        //   });
                        // });
                      }}
                      onCancel={() => {}}
                      okText="确定"
                      cancelText="取消"
                    >
                      <Button danger>
                        <DeleteOutlined /> 删除
                      </Button>
                    </Popconfirm>
                    {/* <Button
            onClick={() => {
              dispatch({
                type: 'materialtype/delete',
                payload: { Id },
              }).then((result) => {
                me.setState({ Id: null });
                dispatch({
                  type: 'materialtype/getAllMaterialType'
                });
              })
            }}
          ><DeleteOutlined /> 删除</Button> */}
                  </>
                ) : null}
              </Space>
              <div style={{ height: 10 }}></div>
              <fieldset className="x-fieldset">
                <legend style={{ width: "auto", fontSize: 14, border: 0, paddingLeft: 10, paddingRight: 10, color: "#333" }}>
                  物料类型树
                </legend>
                <Tree showLine switcherIcon={<DownOutlined />} defaultExpandAll={true} onSelect={onSelect} treeData={treeData} />
              </fieldset>
            </>
          ) : null}
        </Col>
        <Col span={18}>
          <Tabs activeKey={tabKey} onTabClick={onTabClick}>
            <TabPane tab="基本资料" key="1">
              <FormPage Id={Id} parentTypeId={parentTypeId} getAllMaterialType={getAllMaterialType} />
            </TabPane>
            <TabPane tab="数据浏览" key="2">
              {moduleInfo ? (
                <SmProTable
                  moduleCode={moduleCode}
                  moduleInfo={moduleInfo}
                  // actionRef={tableRef}
                  // formRef={formRef}
                  // IsView={IsView}
                  // masterId={masterId}
                  //  onEdit={(id: any, isVIew: any) => {
                  //    dispatch(setId({ moduleCode, id }));

                  //    if (moduleInfo.openType === "Modal") {
                  //      setModalVisible(true);
                  //      setIsView(isVIew);
                  //    } else if (moduleInfo.openType === "Drawer") {
                  //      setDrawerOpen(true);
                  //      setIsView(isVIew);
                  //    } else changePage("FormPage", id, isVIew);
                  //  }}
                />
              ) : null}
            </TabPane>
          </Tabs>
        </Col>
      </Row>
    </Card>
  );
};

export default ImportTemplate;
