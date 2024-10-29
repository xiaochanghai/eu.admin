import React, { useState, useEffect } from "react";
import { Button, message, Tabs, Input, Card, Form, Row, Col, Space, Modal, Skeleton } from "antd";
import { SaveOutlined, RollbackOutlined, InfoCircleOutlined } from "@ant-design/icons";
import { getModuleFullSql, add, update, getModuleSqlInfo } from "@/api/modules/module";
import TableList from "../../common/components/TableList";

const { TextArea } = Input;
const FormItem = Form.Item;
const { TabPane } = Tabs;

const SqlEdit: React.FC<any> = props => {
  const [iShowFullSql, setIShowFullSql] = useState<boolean>(false);
  const [fullSql, setFullSql] = useState("");
  const [tabKey, setTabKey] = useState("");
  const [id, setId] = useState("");
  const { IsView, ModuleId, changePage } = props;
  let legendCss = { width: "auto", fontSize: 14, border: 0, paddingLeft: 10, paddingRight: 10, color: "#333" };
  const [form] = Form.useForm();

  useEffect(() => {
    if (ModuleId) {
      const querySingleData = async () => {
        let { Data, Success } = await getModuleSqlInfo(ModuleId);
        if (Success) {
          if (Data.module) {
            if (!Data.moduleSql)
              Data.moduleSql = {
                TableAliasNames: "A",
                SqlDefaultCondition: "A.IsActive = 'true' AND A.IsDeleted = 'false'",
                SqlRecycleCondition: "A.IsActive = 'true' AND A.IsDeleted = 'true'",
                SqlSelect: "SELECT A.*,A.ID AS DELETE_CONFIRM_MSG"
              };
            setId(Data.moduleSql.ID);
            Object.assign(Data.moduleSql, Data.module);
          }
          form.setFieldsValue(Data.moduleSql);
        }
      };
      querySingleData();
    }
  }, []);

  const showLogRecordCancel = () => {
    setIShowFullSql(false);
  };

  const onTabClick = (key: any) => {
    setTabKey(key);
  };

  const getFullSql = async () => {
    let { Data, Success } = await getModuleFullSql(ModuleId);
    if (Success) {
      setIShowFullSql(true);
      setFullSql(Data);
    }
    // let result = await request("/api/SmModuleSql/GetModuleFullSql/" + Id, {
    //   method: "POST"
    // });
    // if (result && result.Success) me.setState({ iShowFullSql: true, fullSql: result.Data });
  };
  const onFinish = async (data: any) => {
    if (id) data.Id = id;
    data.ModuleId = ModuleId;
    data.url = "/api/SmModuleSql";

    for (let key in data) data[key] = data[key] ?? null;
    let { Data, Success, Message } = id ? await update(data) : await add(data);
    if (Success) {
      message.success(Message);
      if (!id) setId(Data);
    }
  };
  return (
    <>
      <Modal title="完整SQL" open={iShowFullSql} width={800} footer={null} onCancel={showLogRecordCancel}>
        {iShowFullSql ? <TextArea rows={4} value={fullSql} disabled={true} /> : <Skeleton active />}
      </Modal>
      <div style={{ height: 10 }}></div>
      <Form labelCol={{ span: 4 }} wrapperCol={{ span: 16 }} onFinish={onFinish} form={form}>
        <Space style={{ display: "flex", justifyContent: "flex-end" }}>
          {/* <Button type="default" onClick={() => Index.changePage(<TableList />)}>读取XML</Button>
              <Button type="default" onClick={() => Index.changePage(<TableList />)}>生成当前XML</Button>
              <Button type="default" onClick={() => Index.changePage(<TableList />)}>生成全部XML</Button> */}
          <Button type="default" onClick={getFullSql}>
            <InfoCircleOutlined />
            查看完整SQL
          </Button>
          <Button type="default" onClick={() => changePage("FormIndex")}>
            <RollbackOutlined />
          </Button>
        </Space>
        <div style={{ height: 10 }}></div>
        <Card>
          <Row gutter={24} justify={"center"}>
            <Col span={12}>
              <FormItem name="ModuleCode" label="模块代码" rules={[{ required: true }]}>
                <Input placeholder="请输入" disabled={true} />
              </FormItem>
            </Col>
            <Col span={12}>
              <FormItem name="ModuleName" label="模块名称" rules={[{ required: true }]}>
                <Input placeholder="请输入" disabled={true} />
              </FormItem>
            </Col>
          </Row>
        </Card>
        <div style={{ height: 10 }}></div>
        <Card>
          <Tabs onTabClick={onTabClick}>
            <TabPane tab={<span>模块SQL</span>} key="1">
              <fieldset className="x-fieldset">
                <legend style={legendCss}>表信息</legend>
                <Row gutter={24} justify={"center"}>
                  <Col span={12}>
                    <FormItem name="PrimaryTableName" label="主表名" rules={[{ required: true }]}>
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                  <Col span={12}>
                    <FormItem name="TableAliasNames" label="全部表别名" rules={[{ required: true }]}>
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={12}>
                    <FormItem name="TableNames" label="全部表名" rules={[{ required: true }]}>
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                  <Col span={12}>
                    <FormItem name="PrimaryKey" label="主键">
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
              </fieldset>
              <fieldset className="x-fieldset">
                <legend style={legendCss}>SQL信息</legend>
                <Row gutter={24}>
                  <Col span={24}>
                    <FormItem name="SqlSelect" label="Select语句" rules={[{ required: true }]}>
                      <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={24}>
                    <FormItem name="SqlSelectBrw" label="首页Select语句">
                      <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={12}>
                    <FormItem name="JoinType" label="关联类型">
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                  <Col span={12}>
                    {/* <FormItem name="ParentId" label="导出Excel隐藏列">
                                      <ComboGrid api="/api/SmModule/GetPageList" itemkey="ID" itemvalue="ModuleName" />
                                  </FormItem> */}
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={12}>
                    <FormItem name="SqlJoinTable" label="关联表">
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                  <Col span={12}>
                    {/* <FormItem name="ParentId" label="默认查询列索引">
                                      <ComboGrid api="/api/SmModule/GetPageList" itemkey="ID" itemvalue="ModuleName" />
                                  </FormItem> */}
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={12}>
                    <FormItem name="SqlJoinTableAlias" label="关联表别名">
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                  <Col span={12}></Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={24}>
                    <FormItem name="SqlJoinCondition" label="关联条件">
                      <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={24}>
                    <FormItem name="SqlDefaultCondition" label="默认条件*" rules={[{ required: true }]}>
                      <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={24}>
                    <FormItem name="SqlRecycleCondition" label="回收站条件" rules={[{ required: true }]}>
                      <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                <Row gutter={24} justify={"center"}>
                  <Col span={24}>
                    <FormItem name="SqlQueryCondition" label="初始查询条件">
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                {/* <Row gutter={24} justify={"center"}>
                              <Col span={24}>
                                  <FormItem name="RoutePath" label="合并表头">
                                      <Input placeholder="请输入" disabled={IsView} />
                                  </FormItem>
                              </Col>
                          </Row> */}
              </fieldset>
              <fieldset className="x-fieldset">
                <legend style={legendCss}>排序信息</legend>
                <Row gutter={24} justify={"center"}>
                  <Col span={12}>
                    <FormItem
                      name="DefaultSortField"
                      labelCol={{ span: 6 }}
                      label="主表默认排序列名"
                      rules={[{ required: true }]}
                    >
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                  <Col span={12}>
                    <FormItem
                      name="DefaultSortDirection"
                      labelCol={{ span: 6 }}
                      label="主表默认排序方向"
                      rules={[{ required: true }]}
                    >
                      <Input placeholder="请输入" disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
                {/* <Row gutter={24} justify={"center"}>
                              <Col span={12}>
                                  <FormItem name="RoutePath" label="从表默认排序列名">
                                      <Input placeholder="请输入" disabled={IsView} />
                                  </FormItem>
                              </Col>
                              <Col span={12}>
                                  <FormItem name="ParentId" label="从表默认排序方向">
                                      <ComboGrid api="/api/SmModule/GetPageList" itemkey="ID" itemvalue="ModuleName" />
                                  </FormItem>
                              </Col>
                          </Row> */}
              </fieldset>
              <fieldset className="x-fieldset">
                <legend style={legendCss}>描述信息</legend>
                <Row gutter={24} justify={"center"}>
                  <Col span={24}>
                    <FormItem name="Description" label="描述">
                      <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
              </fieldset>
            </TabPane>
            <TabPane tab={<span>完整SQL</span>} key="2">
              {/* <Space style={{ display: 'flex', flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between' }}> */}
              <Row gutter={24} justify={"center"}>
                <Col span={24}>
                  <FormItem name="FullSql" labelCol={{ span: 0 }} wrapperCol={{ span: 24 }}>
                    <TextArea placeholder="请输入" autoSize={{ minRows: 14 }} disabled={IsView} />
                  </FormItem>
                </Col>
              </Row>
              {/* </Space> */}
            </TabPane>
            <TabPane tab={<span>模块列</span>} key="3">
              {/* {this.state.Id ? <DetailTable Id={this.state.Id} /> : null} */}
              <TableList moduleCode="SM_MODULE_COLUMN_MNG" changePage={changePage} masterId={ModuleId} IsView={IsView} />
            </TabPane>
          </Tabs>
          {tabKey != "3" ? (
            <Space style={{ display: "flex", justifyContent: "center" }}>
              {!IsView ? (
                <Button type="primary" htmlType="submit">
                  <SaveOutlined />
                  保存
                </Button>
              ) : (
                ""
              )}
              <Button type="default" onClick={() => changePage("FormIndex")}>
                <RollbackOutlined />
                返回
              </Button>
            </Space>
          ) : (
            ""
          )}
        </Card>
      </Form>
    </>
  );
};

export default SqlEdit;
