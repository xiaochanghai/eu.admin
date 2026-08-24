import React, { lazy, Suspense } from "react";
import XMarkdown from "@ant-design/x-markdown";
import { Card, Skeleton, Tag, Typography } from "antd";

const SupplierModule = lazy(() => import("@/views/basedata/supplier"));

export interface EmbeddedModuleReference {
  moduleCode: "BD_SUPPLIER_MNG";
  path: "basedata/supplier/index";
  viewType: string;
}

const supplierReference: EmbeddedModuleReference = {
  moduleCode: "BD_SUPPLIER_MNG",
  path: "basedata/supplier/index",
  viewType: "module_list"
};

const normalizePath = (value: string) =>
  value.trim().replaceAll("\\", "/").replace(/^\/+|\/+$/g, "").toLowerCase();

const asReference = (value: unknown): EmbeddedModuleReference | undefined => {
  if (typeof value === "string") {
    const text = value.trim();
    if (!text || text.length > 20_000) return undefined;
    try {
      return asReference(JSON.parse(text) as unknown);
    } catch {
      const path = normalizePath(text);
      return path === supplierReference.path || path.endsWith(`/${supplierReference.path}`)
        ? supplierReference
        : undefined;
    }
  }
  if (!value || typeof value !== "object" || Array.isArray(value)) return undefined;
  const record = value as Record<string, unknown>;
  const moduleCode = record.moduleCode ?? record.ModuleCode;
  const rawPath = record.path ?? record.Path ?? record.component ?? record.Component;
  const matchesCode = moduleCode === supplierReference.moduleCode;
  const matchesPath = typeof rawPath === "string" && normalizePath(rawPath) === supplierReference.path;
  if (!matchesCode && !matchesPath) return undefined;
  const viewType = record.type ?? record.Type;
  return {
    ...supplierReference,
    viewType: typeof viewType === "string" && viewType.trim() ? viewType.trim() : supplierReference.viewType
  };
};

export const extractEmbeddedModules = (value: unknown): EmbeddedModuleReference[] => {
  const found = new Map<string, EmbeddedModuleReference>();
  const visit = (candidate: unknown, depth: number) => {
    if (depth > 4 || candidate === null || candidate === undefined) return;
    const reference = asReference(candidate);
    if (reference) found.set(`${reference.moduleCode}:${reference.viewType}`, reference);
    if (typeof candidate === "string") {
      try {
        visit(JSON.parse(candidate) as unknown, depth + 1);
      } catch {
        return;
      }
      return;
    }
    if (Array.isArray(candidate)) {
      candidate.slice(0, 20).forEach(item => visit(item, depth + 1));
      return;
    }
    if (typeof candidate === "object") {
      Object.values(candidate as Record<string, unknown>).slice(0, 30).forEach(item => visit(item, depth + 1));
    }
  };
  visit(value, 0);
  return Array.from(found.values());
};

interface EmbeddedModuleContentProps {
  content: string;
  modules: EmbeddedModuleReference[];
  streaming: boolean;
}

const EmbeddedModuleContent: React.FC<EmbeddedModuleContentProps> = ({ content, modules, streaming }) => (
  <div className="agent-chat-assistant-content">
    {content ? (
      streaming ? <div className="agent-chat-streaming-text">{content}</div> : <XMarkdown paragraphTag="div">{content}</XMarkdown>
    ) : null}
    {modules.map(module => (
      <Card
        className="agent-chat-embedded-module"
        key={`${module.moduleCode}:${module.viewType}`}
        title={<span>供应商管理<Tag>{module.viewType}</Tag></span>}
        extra={<Typography.Text type="secondary">{module.moduleCode}</Typography.Text>}
      >
        <Suspense fallback={<Skeleton active paragraph={{ rows: 6 }} />}>
          <SupplierModule />
        </Suspense>
      </Card>
    ))}
  </div>
);

export default EmbeddedModuleContent;
