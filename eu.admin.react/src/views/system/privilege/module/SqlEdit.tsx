import React, { useState, useEffect, useCallback } from "react";
import { Button, Tabs, Input, Card, Form, Row, Col, Space, Modal, Skeleton } from "antd";
import type { } from "antd";
import { getModuleFullSql, add, update, getModuleSqlInfo } from "@/api/modules/module";
import {
  getModuleLanguageConfig,
  addLanguageConfig,
  updateLanguageConfig,
} from "@/api/modules/smLanguageConfig";
import { TableList, Icon } from "@/components";
import { message } from "@/hooks/useMessage";
import ColumnFormPage from "./ColumnFormPage";
import { ViewType, TabsProps } from "@/typings";

const { TextArea } = Input;

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
  onReload?: () => void;
}

/**
 * 模块SQL信息接口
 */
interface ModuleSqlInfo {
  ID?: string;
  Id?: string;
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
  url?: string;
  [key: string]: any;
}

/**
 * 表单字段配置接口
 */
interface FormFieldConfig {
  name: string;
  label: string;
  required?: boolean;
  span?: number;
  type?: "input" | "textarea";
  rows?: number;
  placeholder?: string;
}

/**
 * 样式常量
 */
const LEGEND_STYLE: React.CSSProperties = {
  width: "auto",
  fontSize: 14,
  border: 0,
  paddingLeft: 10,
  paddingRight: 10,
  color: "#333"
};

/**
 * 默认SQL配置
 */
const DEFAULT_SQL_CONFIG = {
  TableAliasNames: "A",
  SqlDefaultCondition: "A.IsActive = 'true' AND A.IsDeleted = 'false'",
  SqlRecycleCondition: "A.IsActive = 'true' AND A.IsDeleted = 'true'",
  SqlSelect: "SELECT A.*,A.ID AS DELETE_CONFIRM_MSG"
};

/**
 * 表信息字段配置
 */
const TABLE_INFO_FIELDS: FormFieldConfig[] = [
  { name: "PrimaryTableName", label: "主表名", required: true, span: 12 },
  { name: "TableAliasNames", label: "全部表别名", required: true, span: 12 },
  { name: "TableNames", label: "全部表名", required: true, span: 12 },
  { name: "PrimaryKey", label: "主键", span: 12 }
];

/**
 * SQL信息字段配置
 */
const SQL_INFO_FIELDS: FormFieldConfig[] = [
  { name: "SqlSelect", label: "Select语句", required: true, span: 24, type: "textarea", rows: 6 },
  { name: "SqlSelectBrw", label: "首页Select语句", span: 24, type: "textarea", rows: 6 },
  { name: "JoinType", label: "关联类型", span: 24 },
  { name: "SqlJoinTable", label: "关联表", span: 24 },
  { name: "SqlJoinTableAlias", label: "关联表别名", span: 24 },
  { name: "SqlJoinCondition", label: "关联条件", span: 24, type: "textarea", rows: 6 },
  { name: "SqlDefaultCondition", label: "默认条件*", required: true, span: 24, type: "textarea", rows: 6 },
  { name: "SqlRecycleCondition", label: "回收站条件", required: true, span: 24, type: "textarea", rows: 6 },
  { name: "SqlQueryCondition", label: "初始查询条件", span: 24 }
];

/**
 * 排序信息字段配置
 */
const SORT_INFO_FIELDS: FormFieldConfig[] = [
  { name: "DefaultSortField", label: "主表默认排序列名", required: true, span: 12 },
  { name: "DefaultSortDirection", label: "主表默认排序方向", required: true, span: 12 }
];

/**
 * 通用表单字段组件
 */
interface FormFieldsProps {
  fields: FormFieldConfig[];
  isView: boolean;
  isLoad: boolean;
}

const FormFields: React.FC<FormFieldsProps> = ({ fields, isView, isLoad }) => {
  if (isLoad) return <Skeleton active />;

  // 将字段按行分组（每行最多24 span）
  const rows: FormFieldConfig[][] = [];
  let currentRow: FormFieldConfig[] = [];
  let currentSpan = 0;

  fields.forEach(field => {
    const span = field.span || 24;
    if (currentSpan + span > 24) {
      rows.push(currentRow);
      currentRow = [field];
      currentSpan = span;
    } else {
      currentRow.push(field);
      currentSpan += span;
    }
  });

  if (currentRow.length > 0) rows.push(currentRow);

  return (
    <>
      {rows.map((row, rowIndex) => (
        <Row gutter={24} justify="center" key={rowIndex}>
          {row.map(field => (
            <Col span={field.span || 24} key={field.name}>
              <Form.Item
                name={field.name}
                label={field.label}
                rules={field.required ? [{ required: true, message: `请输入${field.label}` }] : []}
              >
                {field.type === "textarea" ? (
                  <TextArea
                    placeholder={field.placeholder || "请输入"}
                    autoSize={{ minRows: field.rows || 4 }}
                    disabled={isView}
                  />
                ) : (
                  <Input placeholder={field.placeholder || "请输入"} disabled={isView} />
                )}
              </Form.Item>
            </Col>
          ))}
        </Row>
      ))}
    </>
  );
};

/**
 * 字段集组件
 */
interface FieldsetProps {
  title: string;
  children: React.ReactNode;
  isLoad: boolean;
}

const Fieldset: React.FC<FieldsetProps> = ({ title, children, isLoad }) => (
  <fieldset style={{ border: "1px solid #d9d9d9", borderRadius: 10 }}>
    <legend style={LEGEND_STYLE}>{title}</legend>
    {!isLoad ? children : <Skeleton active />}
  </fieldset>
);

/**
 * SQL编辑器组件
 *
 * 该组件用于编辑模块的SQL配置信息，包括表信息、SQL语句、排序信息等。
 * 支持查看完整SQL、保存配置、查看模块列等功能。
 *
 * @param props 组件属性
 * @returns React 组件
 */
const SqlEdit: React.FC<SqlEditProps> = ({ IsView = false, ModuleId, changePage, onReload }) => {
  // 状态定义
  const [showFullSql, setShowFullSql] = useState<boolean>(false);
  const [isLoad, setIsLoad] = useState<boolean>(true);
  const [fullSql, setFullSql] = useState<string>("");
  const [tabKey, setTabKey] = useState<string>("1");
  const [id, setId] = useState<string>("");
  const [form] = Form.useForm<ModuleSqlInfo>();

  // 多语配置状态
  const [langForm] = Form.useForm();
  const [langConfigId, setLangConfigId] = useState<string>("");
  const [langIsLoad, setLangIsLoad] = useState<boolean>(true);

  /**
   * 获取模块SQL信息
   */
  const fetchModuleSqlInfo = useCallback(async () => {
    try {
      const { Data, Success } = await getModuleSqlInfo(ModuleId);
      if (Success && Data) {
        if (Data.module) {
          // 如果没有SQL配置信息，则初始化默认值
          if (!Data.moduleSql) {
            Data.moduleSql = DEFAULT_SQL_CONFIG;
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
  }, [ModuleId, form]);

  /**
   * 初始化加载模块SQL信息
   */
  useEffect(() => {
    if (ModuleId) {
      fetchModuleSqlInfo();
    }
  }, [ModuleId, fetchModuleSqlInfo]);

  /**
   * 获取模块多语配置
   */
  const fetchLanguageConfig = useCallback(async () => {
    try {
      setLangIsLoad(true);
      const { Data, Success } = await getModuleLanguageConfig(ModuleId);
      if (Success && Data) {
        setLangConfigId(Data.Id || Data.ID || "");
        langForm.setFieldsValue({
          Value_ZH: Data.Value_ZH || "",
          Value_EN: Data.Value_EN || "",
          Remark: Data.Remark || "",
        });
      } else {
        langForm.setFieldsValue({
          Value_ZH: "",
          Value_EN: "",
          Remark: "",
        });
        setLangConfigId("");
      }
      setLangIsLoad(false);
    } catch (error) {
      console.error("获取多语配置失败:", error);
      setLangIsLoad(false);
    }
  }, [ModuleId, langForm]);

  /**
   * 多语配置Tab切换时加载数据
   */
  useEffect(() => {
    if (tabKey === "4" && ModuleId && langIsLoad) {
      fetchLanguageConfig();
    }
  }, [tabKey, ModuleId, langIsLoad, fetchLanguageConfig]);

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
        Object.keys(data).forEach(key => {
          if (data[key] === undefined) {
            data[key] = null;
          }
        });

        // 根据是否有ID决定新增或更新
        const { Data, Success, Message } = id ? await update(data) : await add(data);

        if (Success) {
          message.success(Message);
          if (!id && Data) setId(Data);
        }
      } catch (error) {
        console.error("保存SQL配置失败:", error);
        message.error("保存SQL配置失败");
      }
    },
    [id, ModuleId]
  );

  /**
   * 多语配置表单提交
   */
  const handleLangFormSubmit = useCallback(
    async (data: Record<string, any>) => {
      debugger
      try {
        data.RefId = ModuleId;

        // 将undefined值转换为null
        Object.keys(data).forEach(key => {
          if (data[key] === undefined) {
            data[key] = null;
          }
        });

        const { Data, Success, Message } = langConfigId
          ? await updateLanguageConfig({ ...data, Id: langConfigId })
          : await addLanguageConfig(data);

        if (Success) {
          message.success(Message);
          if (!langConfigId && Data) setLangConfigId(Data);
        }
      } catch (error) {
        console.error("保存多语配置失败:", error);
        message.error("保存多语配置失败");
      }
    },
    [langConfigId, ModuleId]
  );

  /**
   * 返回按钮处理
   */
  const handleGoBack = () => {
    onReload?.();
    changePage(ViewType.INDEX);
  };

  /**
   * Tabs 配置
   */
  const tabItems: TabsProps["items"] = [
    {
      key: "1",
      label: "模块SQL",
      children: (
        <Space orientation="vertical" size="middle" style={{ width: "100%" }}>
          {/* 表信息字段集 */}
          <Fieldset title="表信息" isLoad={isLoad}>
            <FormFields fields={TABLE_INFO_FIELDS} isView={IsView} isLoad={isLoad} />
          </Fieldset>

          {/* SQL信息字段集 */}
          <Fieldset title="SQL信息" isLoad={isLoad}>
            <FormFields fields={SQL_INFO_FIELDS} isView={IsView} isLoad={isLoad} />
          </Fieldset>

          {/* 排序信息字段集 */}
          <Fieldset title="排序信息" isLoad={isLoad}>
            <FormFields fields={SORT_INFO_FIELDS} isView={IsView} isLoad={isLoad} />
          </Fieldset>

          {/* 描述信息字段集 */}
          <Fieldset title="描述信息" isLoad={isLoad}>
            {!isLoad ? (
              <Row gutter={24} justify="center">
                <Col span={24}>
                  <Form.Item name="Description" label="描述">
                    <TextArea placeholder="请输入" autoSize={{ minRows: 6 }} disabled={IsView} />
                  </Form.Item>
                </Col>
              </Row>
            ) : (
              <Skeleton active />
            )}
          </Fieldset>
        </Space>
      )
    },
    {
      key: "2",
      label: "完整SQL",
      children: (
        <Row gutter={24} justify="center">
          <Col span={24}>
            <Form.Item name="FullSql" labelCol={{ span: 0 }} wrapperCol={{ span: 24 }}>
              <TextArea placeholder="请输入" autoSize={{ minRows: 14 }} disabled={IsView} />
            </Form.Item>
          </Col>
        </Row>
      )
    },
    {
      key: "3",
      label: "模块列",
      children: (
        <TableList
          moduleCode="SM_MODULE_COLUMN_MNG"
          changePage={changePage}
          masterId={ModuleId}
          IsView={IsView}
          DynamicFormPage={ColumnFormPage}
        />
      )
    },
    {
      key: "4",
      label: "多语设置",
      children: (
        <div style={{ padding: "16px 0" }}>
          {langIsLoad ? (
            <Skeleton active />
          ) : (
            <Form
              form={langForm}
              labelCol={{ span: 6 }}
              wrapperCol={{ span: 14 }}
              style={{ maxWidth: 700, margin: "0 auto" }}
            >
              {/* 简体中文 */}
              {/* <Row gutter={24}>
                <Col span={24}>
                  <Form.Item label="简体中文" name="Value_ZH">
                    <Input placeholder="请输入" disabled />
                  </Form.Item>
                </Col>
              </Row> */}

              {/* English */}
              <Row gutter={24}>
                <Col span={24}>
                  <Form.Item label="English" name="Value_EN">
                    <Input placeholder="请输入" disabled={IsView} />
                  </Form.Item>
                </Col>
              </Row>

              {/* 备注 */}
              <Row gutter={24}>
                <Col span={24}>
                  <Form.Item label="备注" name="Remark">
                    <TextArea placeholder="请输入" autoSize={{ minRows: 3 }} disabled={IsView} />
                  </Form.Item>
                </Col>
              </Row>

              {/* 底部按钮 */}
              {!IsView && (
                <Row>
                  <Col span={24}>
                    <Space style={{ display: "flex", justifyContent: "center", marginTop: 16 }}>
                      <Button onClick={() => langForm.resetFields()}>取消</Button>
                      <Button
                        type="primary"
                        icon={<Icon name="SaveOutlined" />}
                        onClick={() => {
                          langForm.validateFields().then(values => {
                            handleLangFormSubmit(values);
                          });
                        }}
                      >
                        确认
                      </Button>
                    </Space>
                  </Col>
                </Row>
              )}
            </Form>
          )}
        </div>
      )
    }
  ];

  return (
    <Space orientation="vertical" size="middle" style={{ width: "100%" }}>
      {/* 完整SQL对话框 */}
      <Modal title="完整SQL" open={showFullSql} width={800} footer={null} onCancel={() => setShowFullSql(false)}>
        {showFullSql ? <TextArea rows={8} value={fullSql} disabled /> : <Skeleton active />}
      </Modal>

      {/* SQL编辑表单 */}
      <Form labelCol={{ span: 4 }} wrapperCol={{ span: 16 }} onFinish={handleFormSubmit} form={form}>
        {/* 顶部工具栏 */}
        <Space style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button type="default" icon={<Icon name="InfoCircleOutlined" />} onClick={handleGetFullSql}>
            查看完整SQL
          </Button>
          <Button type="default" icon={<Icon name="RollbackOutlined" />} onClick={handleGoBack}>
            返回
          </Button>
        </Space>

        {/* 模块基本信息卡片 */}
        <Card className="mt-10">
          {!isLoad ? (
            <Row gutter={24} justify="center">
              <Col span={12}>
                <Form.Item name="ModuleCode" label="模块代码" rules={[{ required: true }]} style={{ marginBottom: 0 }}>
                  <Input placeholder="请输入" disabled />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="ModuleName" label="模块名称" rules={[{ required: true }]} style={{ marginBottom: 0 }}>
                  <Input placeholder="请输入" disabled />
                </Form.Item>
              </Col>
            </Row>
          ) : (
            <Skeleton active />
          )}
        </Card>

        {/* 主要内容卡片 */}
        <Card className="mt-10">
          <Tabs activeKey={tabKey} onChange={setTabKey} items={tabItems} />

          {/* 底部按钮区域 - 仅在非模块列和多语设置标签页显示 */}
          {tabKey !== "3" && tabKey !== "4" && (
            <Space style={{ display: "flex", justifyContent: "center", marginTop: 16 }}>
              {!IsView && (
                <Button type="primary" htmlType="submit" icon={<Icon name="SaveOutlined" />}>
                  保存
                </Button>
              )}
              <Button type="default" icon={<Icon name="RollbackOutlined" />} onClick={() => changePage(ViewType.INDEX)}>
                返回
              </Button>
            </Space>
          )}
        </Card>
      </Form>
    </Space>
  );
};

// 使用 React.memo 优化组件性能，避免不必要的重渲染
export default React.memo(SqlEdit);
