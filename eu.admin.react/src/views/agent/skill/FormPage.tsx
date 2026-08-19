import React, { useCallback, useEffect, useImperativeHandle, useRef, useState } from "react";
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Empty,
  Form,
  Input,
  List,
  Modal,
  Popconfirm,
  Space,
  Spin,
  Tabs,
  Tag,
  Typography
} from "antd";
import { DeleteOutlined, PlusOutlined, RocketOutlined } from "@ant-design/icons";
import { message } from "@/hooks/useMessage";
import { SaveTypeEnum } from "@/typings";
import {
  SkillDefinition,
  SkillFileEntry,
  archiveSkill,
  createSkill,
  deleteSkillFile,
  getAgentSkillErrorMessage,
  getSkill,
  listSkillFiles,
  publishSkill,
  readSkillFile,
  saveSkillFile,
  updateSkill
} from "@/api/modules/agentSkill";
import "./index.less";

interface SkillFormValues {
  code: string;
  name: string;
  description: string;
  category: string;
}

interface FormPageProps {
  Id?: string | null;
  IsView?: boolean | null;
  formPageRef: React.RefObject<{
    onSave: () => void;
    onSaveAdd: () => void;
    onBeforeClose?: () => boolean;
  } | null>;
  onReload?: () => void;
  onDisabled?: (disabled: boolean) => void;
}

const formatLocalDateTime = (value: string) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
};

const FormPage: React.FC<FormPageProps> = ({ Id, IsView, formPageRef, onReload, onDisabled }) => {
  const [form] = Form.useForm<SkillFormValues>();
  const onDisabledRef = useRef(onDisabled);
  const [skill, setSkill] = useState<SkillDefinition | null>(null);
  const [files, setFiles] = useState<SkillFileEntry[]>([]);
  const [filePath, setFilePath] = useState("");
  const [fileContent, setFileContent] = useState("");
  const [basicDirty, setBasicDirty] = useState(false);
  const [fileDirty, setFileDirty] = useState(false);
  const [filePersisted, setFilePersisted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [publishOpen, setPublishOpen] = useState(false);
  const [versionLabel, setVersionLabel] = useState("");

  const archived = skill?.Status === "Archived";
  const readOnly = Boolean(IsView || archived);
  const protectedFile = filePath.trim().toUpperCase() === "SKILL.MD";

  useEffect(() => {
    onDisabledRef.current = onDisabled;
  }, [onDisabled]);

  const fillSkill = useCallback(
    async (value: SkillDefinition) => {
      setSkill(value);
      form.setFieldsValue({
        code: value.Code,
        name: value.Name,
        description: value.Description,
        category: value.Category
      });
      setFiles(await listSkillFiles(value.Id));
      setFilePath("");
      setFileContent("");
      setBasicDirty(false);
      setFileDirty(false);
      setFilePersisted(false);
      onDisabledRef.current?.(true);
    },
    [form]
  );

  const resetEditor = useCallback(() => {
    setSkill(null);
    setFiles([]);
    setFilePath("");
    setFileContent("");
    setBasicDirty(false);
    setFileDirty(false);
    setFilePersisted(false);
    form.resetFields();
    onDisabledRef.current?.(Boolean(IsView));
  }, [IsView, form]);

  const load = useCallback(async () => {
    setLoading(true);
    setLoadError("");
    try {
      if (Id) await fillSkill(await getSkill(Id));
      else resetEditor();
    } catch (error) {
      setLoadError(getAgentSkillErrorMessage(error, "Skill 加载失败"));
    } finally {
      setLoading(false);
    }
  }, [Id, fillSkill, resetEditor]);

  useEffect(() => {
    void load();
  }, [load]);

  const save = useCallback(
    async (saveType = SaveTypeEnum.Save) => {
      if (readOnly || submitting) return;
      if (fileDirty) {
        message.warning("请先保存当前 Draft 文件，再保存基础信息");
        return;
      }
      const values = await form.validateFields();
      setSubmitting(true);
      try {
        const saved = skill
          ? await updateSkill(skill.Id, {
              expectedDraftRevision: skill.DraftRevision,
              name: values.name.trim(),
              description: values.description || "",
              category: values.category || ""
            })
          : await createSkill({
              code: values.code.trim(),
              name: values.name.trim(),
              description: values.description || "",
              category: values.category || ""
            });
        await fillSkill(await getSkill(saved.Id));
        onReload?.();
        message.success(skill ? "Skill 基础信息已保存" : "Skill 已创建");
        if (saveType === SaveTypeEnum.SaveAdd) resetEditor();
      } catch (error) {
        message.error(getAgentSkillErrorMessage(error, "Skill 保存失败"));
      } finally {
        setSubmitting(false);
      }
    },
    [fillSkill, form, onReload, readOnly, resetEditor, skill, submitting]
  );

  useImperativeHandle(
    formPageRef,
    () => ({
      onSave: () => void save(),
      onSaveAdd: () => void save(SaveTypeEnum.SaveAdd),
      onBeforeClose: () => {
        if (submitting) {
          message.warning("操作正在执行，请稍候");
          return false;
        }
        if (basicDirty || fileDirty) {
          message.warning("存在未保存修改，请先保存或取消新建文件");
          return false;
        }
        return true;
      }
    }),
    [basicDirty, fileDirty, save, submitting]
  );

  const requireSaved = () => {
    if (!basicDirty && !fileDirty) return true;
    message.warning("请先保存未提交的修改");
    return false;
  };

  const openFile = async (path: string) => {
    if (!skill || fileDirty) {
      if (fileDirty) message.warning("请先保存当前 Draft 文件");
      return;
    }
    try {
      const content = await readSkillFile(skill.Id, path);
      setFilePath(path);
      setFileContent(content);
      setFileDirty(false);
      setFilePersisted(true);
    } catch (error) {
      message.error(getAgentSkillErrorMessage(error, "Draft 文件读取失败"));
    }
  };

  const newFile = () => {
    if (fileDirty) {
      message.warning("请先保存当前 Draft 文件");
      return;
    }
    setFilePath("");
    setFileContent("");
    setFileDirty(false);
    setFilePersisted(false);
  };

  const cancelNewFile = () => {
    setFilePath("");
    setFileContent("");
    setFileDirty(false);
    setFilePersisted(false);
  };

  const saveFile = async () => {
    if (!skill || !filePath.trim() || submitting) return;
    if (basicDirty) {
      message.warning("请先保存基础信息");
      return;
    }
    if (!filePersisted && files.some(file => file.Path.toUpperCase() === filePath.trim().toUpperCase())) {
      message.warning("该 Draft 文件已存在，请从文件列表打开后编辑");
      return;
    }
    setSubmitting(true);
    try {
      const savedPath = filePath.trim();
      const saved = await saveSkillFile(skill.Id, {
        expectedDraftRevision: skill.DraftRevision,
        path: savedPath,
        content: fileContent
      });
      const [detail, savedContent] = await Promise.all([getSkill(saved.Id), readSkillFile(saved.Id, savedPath)]);
      await fillSkill(detail);
      setFilePath(savedPath);
      setFileContent(savedContent);
      setFileDirty(false);
      setFilePersisted(true);
      onReload?.();
      message.success("Draft 文件已保存");
    } catch (error) {
      message.error(getAgentSkillErrorMessage(error, "Draft 文件保存失败"));
    } finally {
      setSubmitting(false);
    }
  };

  const removeFile = async () => {
    if (!skill || !filePath || submitting) return;
    if (protectedFile) {
      message.warning("SKILL.md 是必需入口文件，不能删除");
      return;
    }
    if (basicDirty) {
      message.warning("请先保存基础信息");
      return;
    }
    setSubmitting(true);
    try {
      const saved = await deleteSkillFile(skill.Id, {
        expectedDraftRevision: skill.DraftRevision,
        path: filePath
      });
      await fillSkill(await getSkill(saved.Id));
      onReload?.();
      message.success("Draft 文件已删除");
    } catch (error) {
      message.error(getAgentSkillErrorMessage(error, "Draft 文件删除失败"));
    } finally {
      setSubmitting(false);
    }
  };

  const handlePublish = async () => {
    if (!skill || !versionLabel.trim() || submitting || !requireSaved()) return;
    setSubmitting(true);
    try {
      const saved = await publishSkill(skill.Id, skill.DraftRevision, versionLabel.trim());
      await fillSkill(await getSkill(saved.Id));
      setPublishOpen(false);
      setVersionLabel("");
      onReload?.();
      message.success("Skill 已发布");
    } catch (error) {
      // message.error(getAgentSkillErrorMessage(error, "Skill 发布失败"));
    } finally {
      setSubmitting(false);
    }
  };

  const handleArchive = async (archivedValue: boolean) => {
    if (!skill || submitting || !requireSaved()) return;
    setSubmitting(true);
    try {
      const saved = await archiveSkill(skill.Id, skill.DraftRevision, archivedValue);
      await fillSkill(await getSkill(saved.Id));
      onReload?.();
      message.success(archivedValue ? "Skill 已归档" : "Skill 已恢复");
    } catch (error) {
      message.error(getAgentSkillErrorMessage(error, archivedValue ? "Skill 归档失败" : "Skill 恢复失败"));
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <Spin className="skill-form__loading" />;
  if (loadError) return <Alert type="error" showIcon message="Skill 加载失败" description={loadError} />;

  const basicPanel = (
    <Form
      form={form}
      layout="vertical"
      onValuesChange={() => {
        setBasicDirty(true);
        onDisabledRef.current?.(false);
      }}
    >
      <Form.Item
        name="code"
        label="Skill Code"
        rules={[{ required: true }, { pattern: /^[a-z0-9]+(?:-[a-z0-9]+)*$/, message: "请输入小写 kebab-case" }]}
      >
        <Input disabled={Boolean(skill) || readOnly} maxLength={128} placeholder="例如 business-query" />
      </Form.Item>
      <Form.Item name="name" label="名称" rules={[{ required: true }]}>
        <Input disabled={readOnly} maxLength={256} />
      </Form.Item>
      <Form.Item name="category" label="分类">
        <Input disabled={readOnly} maxLength={128} />
      </Form.Item>
      <Form.Item name="description" label="说明">
        <Input.TextArea disabled={readOnly} autoSize={{ minRows: 4, maxRows: 8 }} />
      </Form.Item>
    </Form>
  );

  const filesPanel = skill ? (
    <div className="skill-form__files">
      <Card
        size="small"
        title="Draft 文件"
        extra={
          <Button size="small" icon={<PlusOutlined />} disabled={readOnly || submitting} onClick={newFile}>
            新建
          </Button>
        }
      >
        <List
          locale={{ emptyText: "暂无 Draft 文件" }}
          dataSource={files}
          renderItem={file => (
            <List.Item className={file.Path === filePath ? "skill-form__file--active" : undefined}>
              <Button type="link" disabled={fileDirty && file.Path !== filePath} onClick={() => void openFile(file.Path)}>
                {file.Path}
              </Button>
              <Typography.Text type="secondary">{file.Size} B</Typography.Text>
            </List.Item>
          )}
        />
      </Card>
      <Card
        size="small"
        title={filePath || "新文件"}
        extra={
          <Space>
            <Button
              type="primary"
              loading={submitting}
              disabled={readOnly || !filePath.trim() || !fileDirty}
              onClick={() => void saveFile()}
            >
              保存文件
            </Button>
            {!filePersisted && fileDirty && filePath ? (
              <Button disabled={readOnly || submitting} onClick={cancelNewFile}>
                取消新建
              </Button>
            ) : (
              <Popconfirm title="删除该 Draft 文件？" onConfirm={() => void removeFile()}>
                <Button
                  danger
                  icon={<DeleteOutlined />}
                  title={protectedFile ? "SKILL.md 是必需入口文件，不能删除" : undefined}
                  disabled={readOnly || submitting || !filePersisted || protectedFile}
                />
              </Popconfirm>
            )}
          </Space>
        }
      >
        <Input
          value={filePath}
          disabled={readOnly || filePersisted}
          placeholder="相对路径，例如 SKILL.md"
          onChange={event => {
            setFilePath(event.target.value);
            setFileDirty(true);
          }}
        />
        <Input.TextArea
          className="skill-form__editor"
          value={fileContent}
          disabled={readOnly}
          onChange={event => {
            setFileContent(event.target.value);
            setFileDirty(true);
          }}
        />
      </Card>
    </div>
  ) : (
    <Empty description="请先保存 Skill，再维护 Draft 文件" />
  );

  const versionsPanel = skill?.PublishedVersions.length ? (
    <List
      dataSource={skill.PublishedVersions}
      renderItem={version => (
        <List.Item>
          <Descriptions
            size="small"
            column={3}
            items={[
              { key: "label", label: "版本", children: <Tag color="blue">v{version.Label}</Tag> },
              { key: "time", label: "发布时间", children: formatLocalDateTime(version.PublishedAtUtc) },
              {
                key: "hash",
                label: "Manifest SHA-256",
                children: <Typography.Text copyable>{version.ManifestSha256}</Typography.Text>
              },
              { key: "files", label: "文件数", children: version.Files.length },
              {
                key: "agents",
                label: "绑定 Agent",
                span: 2,
                children: version.BoundAgents?.map(item => item.Name || item.Code).join("、") || "无"
              }
            ]}
          />
        </List.Item>
      )}
    />
  ) : (
    <Empty description="尚未发布版本" />
  );

  return (
    <div className="skill-form">
      {skill && (
        <Space className="skill-form__actions" wrap>
          <Tag color={skill.Status === "Active" ? "success" : "warning"}>{skill.Status}</Tag>
          <Typography.Text type="secondary">Draft REV {skill.DraftRevision}</Typography.Text>
          {!readOnly && (
            <Button icon={<RocketOutlined />} disabled={submitting} onClick={() => requireSaved() && setPublishOpen(true)}>
              发布
            </Button>
          )}
          {!IsView && (
            <Popconfirm title={archived ? "恢复此 Skill？" : "归档此 Skill？"} onConfirm={() => void handleArchive(!archived)}>
              <Button danger={!archived} disabled={submitting}>
                {archived ? "恢复" : "归档"}
              </Button>
            </Popconfirm>
          )}
        </Space>
      )}
      <Tabs
        items={[
          { key: "basic", label: "基础信息", children: basicPanel },
          { key: "files", label: "Draft 文件", children: filesPanel },
          { key: "versions", label: "发布版本", children: versionsPanel }
        ]}
      />
      <Modal
        title="发布 Skill"
        open={publishOpen}
        confirmLoading={submitting}
        okButtonProps={{ disabled: !versionLabel.trim() }}
        onOk={() => void handlePublish()}
        onCancel={() => {
          if (!submitting) setPublishOpen(false);
        }}
      >
        <Input value={versionLabel} onChange={event => setVersionLabel(event.target.value)} placeholder="SemVer，例如 1.0.0" />
      </Modal>
    </div>
  );
};

export default FormPage;
