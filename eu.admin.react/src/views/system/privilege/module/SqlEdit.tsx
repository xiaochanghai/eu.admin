import React, { useState, useEffect, useCallback } from "react";
import { Button, Tabs, Input, Card, Form, Row, Col, Space, Modal, Skeleton } from "antd";
import { getModuleFullSql, add, update, getModuleSqlInfo } from "@/api/modules/module";
import TableList from "../../common/components/TableList";
import { message } from "@/hooks/useMessage";
import { Icon } from "@/components";
import ColumnFormPage from "./ColumnFormPage";
import { ViewType } from "@/typings";

const { TextArea } = Input;
const FormItem = Form.Item;
const { TabPane } = Tabs;

/**
 * SQL编辑器组件属性接口
 */
interface SqlEditProps {
  /** 是否为查看模式 */
  IsView?: boolean;
  /** 模块ID */
  ModuleId: string;
  /** 页面切换回调函数 */
  changePage: (viewType: ViewType, id?: string, isView?: boolean) => void;
}

/**
 * 模块SQL信息接口
 */
interface ModuleSqlInfo {
  ID?: string;
  ModuleId?: string;
  ModuleCode?: string;
  ModuleName?: string;
  PrimaryTableName?: string;
  TableAliasNames?: string;
  TableNames?: string;
  PrimaryKey?: string;
  SqlSelect?: string;
  SqlSelectBrw?: string;
  JoinType?: string;
  SqlJoinTable?: string;
  SqlJoinTableAlias?: string;
  SqlJoinCondition?: string;
  SqlDefaultCondition?: string;
  SqlRecycleCondition?: string;
  SqlQueryCondition?: string;
  DefaultSortField?: string;
  DefaultSortDirection?: string;
  Description?: string;
  FullSql?: string;
  [key: string]: any;
}

/**
 * 字段集样式常量
 */
const LEGEND_STYLE = {
  width: "auto",
  fontSize: 14,
  border: 0,
  paddingLeft: 10,
  paddingRight: 10,
  color: "#333"
};

/**
 * SQL编辑器组件
 *
 * 该组件用于编辑模块的SQL配置信息，包括表信息、SQL语句、排序信息等。
 * 支持查看完整SQL、保存配置、查看模块列等功能。
 *
 * @param props 组件属性
 * @returns React 组件
 */
const SqlEdit: React.FC<SqlEditProps> = ({ IsView = false, ModuleId, changePage }) => {
  // 状态定义
  const [showFullSql, setShowFullSql] = useState<boolean>(false);
  const [isLoad, setIsLoad] = useState<boolean>(true);
  const [fullSql, setFullSql] = useState<string>("");
  const [tabKey, setTabKey] = useState<string>("");
  const [id, setId] = useState<string>("");
  const [form] = Form.useForm<ModuleSqlInfo>();

  /**
   * 初始化加载模块SQL信息
   */
  useEffect(() => {
    if (ModuleId) fetchModuleSqlInfo();
  }, [ModuleId]);

  /**
   * 获取模块SQL信息
   */
  const fetchModuleSqlInfo = async () => {
    try {
      const { Data, Success } = await getModuleSqlInfo(ModuleId);
      if (Success && Data) {
        if (Data.module) {
          // 如果没有SQL配置信息，则初始化默认值
          if (!Data.moduleSql) {
            Data.moduleSql = {
              TableAliasNames: "A",
              SqlDefaultCondition: "A.IsActive = 'true' AND A.IsDeleted = 'false'",
              SqlRecycleCondition: "A.IsActive = 'true' AND A.IsDeleted = 'true'",
              SqlSelect: "SELECT A.*,A.ID AS DELETE_CONFIRM_MSG"
            };
          }
          setIsLoad(false);
          setId(Data.moduleSql.ID);
          // 合并模块基本信息到SQL配置中
          Object.assign(Data.moduleSql, Data.module);
        }
        form.setFieldsValue(Data.moduleSql);
      }
    } catch (error) {
      console.error("获取模块SQL信息失败:", error);
      message.error("获取模块SQL信息失败");
    }
  };

  /**
   * 关闭完整SQL对话框
   */
  const handleCloseFullSql = () => setShowFullSql(false);

  /**
   * 标签页切换处理
   * @param key 标签页键值
   */
  const handleTabClick = (key: string) => setTabKey(key);

  /**
   * 获取并显示完整SQL
   */
  const handleGetFullSql = async () => {
    try {
      const { Data, Success } = await getModuleFullSql(ModuleId);
      if (Success) {
        setShowFullSql(true);
        setFullSql(Data);
      }
    } catch (error) {
      console.error("获取完整SQL失败:", error);
      message.error("获取完整SQL失败");
    }
  };

  /**
   * 表单提交处理
   * @param data 表单数据
   */
  const handleFormSubmit = useCallback(
    async (data: ModuleSqlInfo) => {
      try {
        // 设置ID和ModuleId
        if (id) data.Id = id;
        data.ModuleId = ModuleId;
        data.url = "/api/SmModuleSql";

        // 将undefined值转换为null
        for (let key in data) {
          data[key] = data[key] ?? null;
        }

        // 根据是否有ID决定新增或更新
        const { Data, Success, Message } = id ? await update(data) : await add(data);

        if (Success) {
          message.success(Message);
          if (!id) setId(Data);
        }
      } catch (error) {
        console.error("保存SQL配置失败:", error);
        message.error("保存SQL配置失败");
      }
    },
    [id, ModuleId]
  );

  /**
   * 渲染表单字段集
   * @param title 字段集标题
   * @param children 字段集内容
   */
  const renderFieldset = (title: string, children: React.ReactNode) => (
    <fieldset className="x-fieldset">
      <legend style={LEGEND_STYLE}>{title}</legend>
      {!isLoad ? children : <Skeleton active />}
    </fieldset>
  );

  return (
    <>
      {/* 完整SQL对话框 */}
      <Modal title="完整SQL" open={showFullSql} width={800} footer={null} onCancel={handleCloseFullSql}>
        {showFullSql ? <TextArea rows={8} value={fullSql} disabled={true} /> : <Skeleton active />}
      </Modal>

      <div style={{ height: 10 }}></div>

      {/* SQL编辑表单 */}
      <Form labelCol={{ span: 4 }} wrapperCol={{ span: 16 }} onFinish={handleFormSubmit} form={form}>
        {/* 顶部工具栏 */}
        <Space style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button type="default" onClick={handleGetFullSql}>
            <Icon name="InfoCircleOutlined" />
            查看完整SQL
          </Button>
          <Button type="default" onClick={() => changePage(ViewType.INDEX)}>
            <Icon name="RollbackOutlined" />
            返回
          </Button>
        </Space>

        <div style={{ height: 10 }}></div>

        {/* 模块基本信息卡片 */}
        <Card>
          {!isLoad ? (
            <Row gutter={24} justify={"center"}>
              <Col span={12}>
                <FormItem name="ModuleCode" label="模块代码" rules={[{ required: true }]} style={{ marginBottom: 0 }}>
                  <Input placeholder="请输入" disabled={true} />
                </FormItem>
              </Col>
              <Col span={12}>
                <FormItem name="ModuleName" label="模块名称" rules={[{ required: true }]} style={{ marginBottom: 0 }}>
                  <Input placeholder="请输入" disabled={true} />
                </FormItem>
              </Col>
            </Row>
          ) : (
            <Skeleton active />
          )}
        </Card>

        <div style={{ height: 10 }}></div>

        {/* 主要内容卡片 */}
        <Card>
          <Tabs onTabClick={handleTabClick}>
            {/* 模块SQL标签页 */}
            <TabPane tab={<span>模块SQL</span>} key="1">
              {/* 表信息字段集 */}
              {renderFieldset(
                "表信息",
                <>
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
                </>
              )}

              {/* SQL信息字段集 */}
              {renderFieldset(
                "SQL信息",
                <>
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
                    <Col span={12}></Col>
                  </Row>
                  <Row gutter={24} justify={"center"}>
                    <Col span={12}>
                      <FormItem name="SqlJoinTable" label="关联表">
                        <Input placeholder="请输入" disabled={IsView} />
                      </FormItem>
                    </Col>
                    <Col span={12}></Col>
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
                </>
              )}

              {/* 排序信息字段集 */}
              {renderFieldset(
                "排序信息",
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
              )}

              {/* 描述信息字段集 */}
              {renderFieldset(
                "描述信息",
                <Row gutter={24} justify={"center"}>
                  <Col span={24}>
                    <FormItem name="Description" label="描述">
                      <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                    </FormItem>
                  </Col>
                </Row>
              )}
            </TabPane>

            {/* 完整SQL标签页 */}
            <TabPane tab={<span>完整SQL</span>} key="2">
              <Row gutter={24} justify={"center"}>
                <Col span={24}>
                  <FormItem name="FullSql" labelCol={{ span: 0 }} wrapperCol={{ span: 24 }}>
                    <TextArea placeholder="请输入" autoSize={{ minRows: 14 }} disabled={IsView} />
                  </FormItem>
                </Col>
              </Row>
            </TabPane>

            {/* 模块列标签页 */}
            <TabPane tab={<span>模块列</span>} key="3">
              <TableList
                moduleCode="SM_MODULE_COLUMN_MNG"
                changePage={changePage}
                masterId={ModuleId}
                IsView={IsView}
                DynamicFormPage={ColumnFormPage}
              />
            </TabPane>
          </Tabs>

          {/* 底部按钮区域 - 仅在非模块列标签页显示 */}
          {tabKey !== "3" && (
            <Space style={{ display: "flex", justifyContent: "center" }}>
              {!IsView && (
                <Button type="primary" htmlType="submit">
                  <Icon name="SaveOutlined" />
                  保存
                </Button>
              )}
              <Button type="default" onClick={() => changePage(ViewType.INDEX)}>
                <Icon name="RollbackOutlined" />
                返回
              </Button>
            </Space>
          )}
        </Card>
      </Form>
    </>
  );
};

// 使用 React.memo 优化组件性能，避免不必要的重渲染
export default React.memo(SqlEdit);
