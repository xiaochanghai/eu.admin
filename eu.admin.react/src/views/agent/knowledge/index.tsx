import {
  BookOutlined,
  FileTextOutlined,
  InboxOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
  UploadOutlined
} from "@ant-design/icons";
import {
  Alert,
  Button,
  Empty,
  Flex,
  Form,
  Input,
  List,
  Pagination,
  Popconfirm,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
  Upload,
  type TableColumnsType,
  type UploadFile
} from "antd";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  createKnowledge,
  getKnowledge,
  getKnowledgeErrorMessage,
  importKnowledgePdf,
  importKnowledgeText,
  listKnowledge,
  listKnowledgeChunks,
  listKnowledgeDocuments,
  searchKnowledge,
  setKnowledgeArchived,
  updateKnowledge,
  type KnowledgeChunkPage,
  type KnowledgeDetail,
  type KnowledgeDocument,
  type KnowledgeListItem,
  type KnowledgeSearchResult,
  type KnowledgeStatus
} from "@/api/modules/agentKnowledge";
import { getModuleInfo } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import "./index.less";

const MODULE_CODE = "AG_KNOWLEDGE_BASE_MNG";

interface KnowledgeFormValues {
  code: string;
  name: string;
  description: string;
  status: Exclude<KnowledgeStatus, "Archived">;
}

const statusMeta: Record<KnowledgeStatus, { text: string; color: string }> = {
  Enabled: { text: "已启用", color: "success" },
  Disabled: { text: "已停用", color: "default" },
  Archived: { text: "已归档", color: "warning" }
};

const chunkPageSize = 10;

const KnowledgePage = () => {
  const [form] = Form.useForm<KnowledgeFormValues>();
  const [moduleActions, setModuleActions] = useState<Set<string>>(() => new Set());
  const [statusFilter, setStatusFilter] = useState<KnowledgeStatus | undefined>();
  const [items, setItems] = useState<KnowledgeListItem[]>([]);
  const [current, setCurrent] = useState<KnowledgeDetail | null>(null);
  const [creating, setCreating] = useState(false);
  const [documents, setDocuments] = useState<KnowledgeDocument[]>([]);
  const [selectedDocumentId, setSelectedDocumentId] = useState<string>();
  const [chunks, setChunks] = useState<KnowledgeChunkPage | null>(null);
  const [searchResults, setSearchResults] = useState<KnowledgeSearchResult[]>([]);
  const [query, setQuery] = useState("");
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [listLoading, setListLoading] = useState(false);
  const [contentLoading, setContentLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [importing, setImporting] = useState(false);
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState("");
  const requestSequence = useRef(0);
  const listRequestSequence = useRef(0);
  const searchRequestSequence = useRef(0);

  const archived = current?.Status === "Archived";
  const canAdd = moduleActions.has("Add");
  const canUpdate = moduleActions.has("Update");

  useEffect(() => {
    let active = true;

    const loadModuleActions = async () => {
      try {
        const { Data } = await getModuleInfo(MODULE_CODE);
        if (active) setModuleActions(new Set(Data.actions || []));
      } catch (loadError) {
        if (active) setError(getKnowledgeErrorMessage(loadError, "知识库模块权限加载失败"));
      }
    };

    void loadModuleActions();
    return () => {
      active = false;
    };
  }, []);

  const loadList = useCallback(async () => {
    const sequence = ++listRequestSequence.current;
    setListLoading(true);
    try {
      const loaded = await listKnowledge(statusFilter);
      if (sequence === listRequestSequence.current) setItems(loaded);
    } catch (loadError) {
      if (sequence === listRequestSequence.current) {
        setError(getKnowledgeErrorMessage(loadError, "知识库列表加载失败"));
      }
    } finally {
      if (sequence === listRequestSequence.current) setListLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => {
    void loadList();
  }, [loadList]);

  const loadChunks = useCallback(async (knowledgeId: string, documentId: string, skip = 0, sequence?: number) => {
    const activeSequence = sequence ?? ++requestSequence.current;
    setContentLoading(true);
    try {
      const page = await listKnowledgeChunks(knowledgeId, documentId, skip, chunkPageSize);
      if (activeSequence !== requestSequence.current) return;
      setSelectedDocumentId(documentId);
      setChunks(page);
    } catch (loadError) {
      if (activeSequence === requestSequence.current) {
        setError(getKnowledgeErrorMessage(loadError, "知识分块加载失败"));
      }
    } finally {
      if (activeSequence === requestSequence.current) setContentLoading(false);
    }
  }, []);

  const openKnowledge = useCallback(
    async (id: string) => {
      const sequence = ++requestSequence.current;
      searchRequestSequence.current += 1;
      setSearching(false);
      setCreating(false);
      setContentLoading(true);
      setError("");
      setSearchResults([]);
      setFileList([]);
      try {
        const [detail, loadedDocuments] = await Promise.all([getKnowledge(id), listKnowledgeDocuments(id)]);
        if (sequence !== requestSequence.current) return;
        setCurrent(detail);
        setDocuments(loadedDocuments);
        form.resetFields(["code", "name", "description", "status"]);
        form.setFieldsValue({
          code: detail.Code,
          name: detail.Name,
          description: detail.Description,
          status: detail.Status === "Enabled" ? "Enabled" : "Disabled"
        });
        if (loadedDocuments[0]) {
          await loadChunks(id, loadedDocuments[0].Id, 0, sequence);
        } else {
          setSelectedDocumentId(undefined);
          setChunks(null);
          setContentLoading(false);
        }
      } catch (loadError) {
        if (sequence === requestSequence.current) {
          setContentLoading(false);
          setError(getKnowledgeErrorMessage(loadError, "知识库加载失败"));
        }
      }
    },
    [form, loadChunks]
  );

  const startCreate = () => {
    requestSequence.current += 1;
    searchRequestSequence.current += 1;
    setSearching(false);
    setCreating(true);
    setCurrent(null);
    setDocuments([]);
    setChunks(null);
    setSelectedDocumentId(undefined);
    setSearchResults([]);
    setFileList([]);
    setError("");
    form.resetFields();
    form.setFieldsValue({ code: "", name: "", description: "", status: "Enabled" });
  };

  const refreshCurrent = async (id: string) => {
    await Promise.all([openKnowledge(id), loadList()]);
  };

  const hasUnsavedMetadata = () =>
    !!current &&
    (form.getFieldValue("name")?.trim() !== current.Name ||
      (form.getFieldValue("description") || "") !== current.Description);

  const save = async () => {
    if (saving || archived) return;
    const values = await form.validateFields();
    setSaving(true);
    setError("");
    try {
      const saved = current
        ? await updateKnowledge(current.Id, {
            expectedLogicalRevision: current.LogicalRevision,
            name: values.name.trim(),
            description: values.description || "",
            status: current.Status === "Disabled" ? "Disabled" : "Enabled"
          })
        : await createKnowledge({
            code: values.code.trim(),
            name: values.name.trim(),
            description: values.description || ""
          });
      message.success(current ? "知识库已保存" : "知识库已创建");
      await refreshCurrent(saved.Id);
    } catch (saveError) {
      setError(getKnowledgeErrorMessage(saveError, "知识库保存失败"));
    } finally {
      setSaving(false);
    }
  };

  const toggleStatus = async () => {
    if (!current || saving || archived) return;
    const values = await form.validateFields();
    const target = current.Status === "Enabled" ? "Disabled" : "Enabled";
    setSaving(true);
    setError("");
    try {
      const saved = await updateKnowledge(current.Id, {
        expectedLogicalRevision: current.LogicalRevision,
        name: values.name.trim(),
        description: values.description || "",
        status: target
      });
      message.success(target === "Enabled" ? "知识库已启用" : "知识库已停用");
      await refreshCurrent(saved.Id);
    } catch (statusError) {
      setError(getKnowledgeErrorMessage(statusError, "知识库状态更新失败"));
    } finally {
      setSaving(false);
    }
  };

  const toggleArchived = async () => {
    if (!current || saving) return;
    if (!archived && current.Status !== "Disabled") {
      message.warning("请先停用知识库，再执行归档");
      return;
    }
    if (hasUnsavedMetadata()) {
      message.warning("存在未保存的基础信息修改，请先保存再执行归档");
      return;
    }
    setSaving(true);
    setError("");
    try {
      const saved = await setKnowledgeArchived(current.Id, current.LogicalRevision, !archived);
      message.success(archived ? "知识库已恢复为停用状态" : "知识库已归档");
      await refreshCurrent(saved.Id);
    } catch (archiveError) {
      setError(getKnowledgeErrorMessage(archiveError, "知识库归档状态更新失败"));
    } finally {
      setSaving(false);
    }
  };

  const importDocument = async () => {
    const file = fileList[0]?.originFileObj;
    if (!current || !file || importing || archived) return;
    if (hasUnsavedMetadata()) {
      message.warning("存在未保存的基础信息修改，请先保存再导入文档");
      return;
    }
    const isPdf = /\.pdf$/i.test(file.name);
    const validExtension = /\.(txt|md|pdf)$/i.test(file.name);
    const maximumBytes = isPdf ? 10_485_760 : 2_000_000;
    if (!validExtension || file.size > maximumBytes) {
      setError("仅允许不超过 2 MB 的 .txt/.md，或不超过 10 MiB 的 .pdf 文件");
      return;
    }
    const textContent = isPdf ? null : await file.text();
    const normalizedText = textContent
      ?.replace(/\r\n/g, "\n")
      .replace(/\r/g, "\n")
      .trim();
    if (normalizedText !== undefined && normalizedText.length > 1_000_000) {
      setError("TXT/Markdown 文档规范化后不能超过 1,000,000 个字符");
      return;
    }
    setImporting(true);
    setError("");
    try {
      const saved = isPdf
        ? await importKnowledgePdf(current.Id, current.LogicalRevision, file)
        : await importKnowledgeText(current.Id, {
            expectedLogicalRevision: current.LogicalRevision,
            fileName: file.name,
            mediaType: /\.md$/i.test(file.name) ? "text/markdown" : "text/plain",
            content: textContent || ""
          });
      setFileList([]);
      message.success(`${file.name} 已导入并建立索引`);
      await refreshCurrent(saved.Id);
    } catch (importError) {
      setError(getKnowledgeErrorMessage(importError, "知识文档导入失败"));
    } finally {
      setImporting(false);
    }
  };

  const runSearch = async () => {
    const value = query.trim();
    if (!current || !value || searching || archived) return;
    const knowledgeId = current.Id;
    const sequence = ++searchRequestSequence.current;
    setSearching(true);
    setError("");
    try {
      const results = await searchKnowledge(knowledgeId, value);
      if (sequence === searchRequestSequence.current) setSearchResults(results);
    } catch (searchError) {
      if (sequence === searchRequestSequence.current) {
        setError(getKnowledgeErrorMessage(searchError, "知识库检索失败"));
      }
    } finally {
      if (sequence === searchRequestSequence.current) setSearching(false);
    }
  };

  const columns = useMemo<TableColumnsType<KnowledgeListItem>>(
    () => [
      {
        title: "知识库",
        key: "identity",
        render: (_, item) => (
          <div className="knowledge-page__identity">
            <Typography.Text strong>{item.Name || item.Code}</Typography.Text>
            <Typography.Text type="secondary" code>
              {item.Code}
            </Typography.Text>
          </div>
        )
      },
      {
        title: "内容",
        key: "content",
        width: 104,
        render: (_, item) => `${item.DocumentCount} / ${item.ChunkCount}`
      },
      {
        title: "状态",
        dataIndex: "Status",
        width: 86,
        render: (status: KnowledgeStatus) => <Tag color={statusMeta[status].color}>{statusMeta[status].text}</Tag>
      }
    ],
    []
  );

  return (
    <div className="knowledge-page">
      <Flex justify="space-between" align="center" gap={16} wrap className="knowledge-page__header">
        <div>
          <Typography.Title level={3}>知识库维护</Typography.Title>
          <Typography.Text type="secondary">维护可发布给 Agent 的知识来源、索引分块与检索结果。</Typography.Text>
        </div>
        <Space wrap>
          <Select<KnowledgeStatus | undefined>
            allowClear
            placeholder="全部状态"
            value={statusFilter}
            options={Object.entries(statusMeta).map(([value, meta]) => ({ value, label: meta.text }))}
            onChange={setStatusFilter}
            className="knowledge-page__status-filter"
          />
          <Button icon={<ReloadOutlined />} loading={listLoading} onClick={() => void loadList()}>
            刷新
          </Button>
          {canAdd && (
            <Button type="primary" icon={<PlusOutlined />} onClick={startCreate}>
              新建知识库
            </Button>
          )}
        </Space>
      </Flex>

      {error && (
        <Alert type="error" showIcon closable message={error} onClose={() => setError("")} className="knowledge-page__alert" />
      )}

      <div className="knowledge-page__layout">
        <aside className="knowledge-page__catalog">
          <div className="knowledge-page__section-title">
            <BookOutlined />
            <span>知识库</span>
            <Tag>{items.length}</Tag>
          </div>
          <Table<KnowledgeListItem>
            rowKey="Id"
            size="small"
            loading={listLoading}
            columns={columns}
            dataSource={items}
            pagination={false}
            locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无知识库" /> }}
            rowClassName={item => (item.Id === current?.Id ? "knowledge-page__row--active" : "")}
            onRow={item => ({ onClick: () => void openKnowledge(item.Id) })}
          />
        </aside>

        <main className="knowledge-page__workspace">
          {!current && !creating ? (
            <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="选择一个知识库，或新建知识库开始维护" />
          ) : (
            <Spin spinning={contentLoading || saving}>
              <Flex justify="space-between" align="center" gap={12} wrap className="knowledge-page__workspace-heading">
                <div>
                  <Typography.Title level={4}>{creating ? "新建知识库" : current?.Name || current?.Code}</Typography.Title>
                  {current && (
                    <Typography.Text type="secondary">
                      {current.DocumentCount} 个文档 · {current.ChunkCount} 个分块 · REV {current.LogicalRevision}
                    </Typography.Text>
                  )}
                </div>
                {current && (
                  <Space wrap>
                    <Tag color={statusMeta[current.Status].color}>{statusMeta[current.Status].text}</Tag>
                    {canUpdate && !archived && (
                      <Button onClick={() => void toggleStatus()} disabled={saving}>
                        {current.Status === "Enabled" ? "停用" : "启用"}
                      </Button>
                    )}
                    {canUpdate && <Popconfirm
                      title={archived ? "恢复此知识库？" : "归档此知识库？"}
                      description={!archived && current.Status === "Enabled" ? "已启用的知识库可能被 Agent 引用，请先确认影响。" : undefined}
                      onConfirm={() => void toggleArchived()}
                    >
                      <Button
                        danger={!archived}
                        icon={<InboxOutlined />}
                        disabled={saving || (!archived && current.Status === "Enabled")}
                        title={!archived && current.Status === "Enabled" ? "请先停用知识库" : undefined}
                      >
                        {archived ? "恢复" : "归档"}
                      </Button>
                    </Popconfirm>}
                  </Space>
                )}
              </Flex>

              {archived && <Alert type="warning" showIcon message="该知识库已归档，当前仅允许查看或恢复。" />}

              <section className="knowledge-page__section">
                <div className="knowledge-page__section-title">基础信息</div>
                <Form<KnowledgeFormValues>
                  form={form}
                  layout="vertical"
                  requiredMark="optional"
                  disabled={archived || saving || !(creating ? canAdd : canUpdate)}
                >
                  <Flex gap={16} wrap>
                    <Form.Item
                      name="code"
                      label="Knowledge Code"
                      rules={[
                        { required: true },
                        { pattern: /^[a-z0-9]+(?:-[a-z0-9]+)*$/, message: "请输入小写 kebab-case" }
                      ]}
                      className="knowledge-page__half"
                    >
                      <Input disabled={Boolean(current) || archived} maxLength={128} placeholder="例如 product-handbook" />
                    </Form.Item>
                    <Form.Item name="name" label="名称" rules={[{ required: true }]} className="knowledge-page__half">
                      <Input maxLength={256} />
                    </Form.Item>
                  </Flex>
                  <Form.Item name="description" label="说明">
                    <Input.TextArea autoSize={{ minRows: 2, maxRows: 5 }} maxLength={1000} showCount />
                  </Form.Item>
                  {!archived && (creating ? canAdd : canUpdate) && (
                    <Button type="primary" loading={saving} onClick={() => void save()}>
                      {creating ? "创建知识库" : "保存基础信息"}
                    </Button>
                  )}
                </Form>
              </section>

              {current && (
                <>
                  <section className="knowledge-page__section">
                    <Flex justify="space-between" align="center" gap={12} wrap>
                      <div className="knowledge-page__section-title">
                        <UploadOutlined />
                        <span>导入文档</span>
                      </div>
                      <Typography.Text type="secondary">
                        TXT / Markdown ≤ 2 MB 且 ≤ 1,000,000 字符，PDF ≤ 10 MiB
                      </Typography.Text>
                    </Flex>
                    <Flex gap={12} align="center" wrap>
                      <Upload
                        accept=".txt,.md,.pdf"
                        maxCount={1}
                        beforeUpload={() => false}
                        fileList={fileList}
                        disabled={archived || importing || !canUpdate}
                        onChange={({ fileList: next }) => setFileList(next.slice(-1))}
                      >
                        <Button icon={<FileTextOutlined />} disabled={archived || importing || !canUpdate}>
                          选择文档
                        </Button>
                      </Upload>
                      <Button
                        type="primary"
                        icon={<UploadOutlined />}
                        loading={importing}
                        disabled={archived || fileList.length === 0 || !canUpdate}
                        onClick={() => void importDocument()}
                      >
                        导入并索引
                      </Button>
                    </Flex>
                  </section>

                  <section className="knowledge-page__section knowledge-page__browser">
                    <div className="knowledge-page__documents">
                      <div className="knowledge-page__section-title">文档 ({documents.length})</div>
                      <List
                        dataSource={documents}
                        locale={{ emptyText: "尚未导入文档" }}
                        renderItem={document => (
                          <List.Item
                            className={document.Id === selectedDocumentId ? "knowledge-page__document--active" : undefined}
                            onClick={() => void loadChunks(current.Id, document.Id)}
                          >
                            <div className="knowledge-page__document-info">
                              <Typography.Text strong ellipsis>
                                {document.FileName}
                              </Typography.Text>
                              <Typography.Text type="secondary">
                                {document.ChunkCount} 个分块 · {document.CharacterCount.toLocaleString()} 字符
                              </Typography.Text>
                              <Typography.Text type="secondary" className="knowledge-page__sha">
                                SHA {document.Sha256.slice(0, 12)} · {new Date(document.ImportedAtUtc).toLocaleString()}
                              </Typography.Text>
                            </div>
                          </List.Item>
                        )}
                      />
                    </div>
                    <div className="knowledge-page__chunks">
                      <Flex justify="space-between" align="center" gap={12} wrap>
                        <div>
                          <div className="knowledge-page__section-title">{chunks?.FileName || "索引分块"}</div>
                          <Typography.Text type="secondary">{chunks ? `${chunks.TotalCount} 个分块` : "选择文档查看内容"}</Typography.Text>
                        </div>
                        {chunks && chunks.TotalCount > chunkPageSize && (
                          <Pagination
                            simple
                            current={Math.floor(chunks.Skip / chunks.Take) + 1}
                            pageSize={chunks.Take}
                            total={chunks.TotalCount}
                            onChange={page => void loadChunks(current.Id, chunks.DocumentId, (page - 1) * chunks.Take)}
                          />
                        )}
                      </Flex>
                      {chunks?.Items.length ? (
                        <div className="knowledge-page__chunk-list">
                          {chunks.Items.map(chunk => (
                            <article key={chunk.Id} className="knowledge-page__chunk">
                              <Flex justify="space-between" align="center">
                                <Typography.Text strong>#{chunk.Sequence}</Typography.Text>
                                <Typography.Text type="secondary">{chunk.CharacterCount.toLocaleString()} 字符</Typography.Text>
                              </Flex>
                              <pre>{chunk.Content}</pre>
                            </article>
                          ))}
                        </div>
                      ) : (
                        <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无可显示的分块" />
                      )}
                    </div>
                  </section>

                  <section className="knowledge-page__section">
                    <div className="knowledge-page__section-title">
                      <SearchOutlined />
                      <span>检索验证</span>
                    </div>
                    <Input.Search
                      value={query}
                      enterButton="检索"
                      loading={searching}
                      disabled={archived}
                      placeholder="输入问题或关键词，验证召回结果"
                      onChange={event => setQuery(event.target.value)}
                      onSearch={() => void runSearch()}
                    />
                    {searchResults.length > 0 && (
                      <div className="knowledge-page__search-results">
                        {searchResults.map(result => (
                          <article key={result.ChunkId} className="knowledge-page__search-result">
                            <Flex justify="space-between" align="center" gap={12}>
                              <Typography.Text strong>
                                {result.FileName} #{result.ChunkSequence}
                              </Typography.Text>
                              <Tag>score {result.Score.toFixed(3)}</Tag>
                            </Flex>
                            <pre>{result.Content}</pre>
                          </article>
                        ))}
                      </div>
                    )}
                  </section>
                </>
              )}
            </Spin>
          )}
        </main>
      </div>
    </div>
  );
};

export default KnowledgePage;
