// Only display measured values. Missing, invalid or imprecise numbers must never become zero.
export const formatRunCount = (value?: number | null): string =>
  typeof value === "number" && Number.isSafeInteger(value) && value >= 0 ? value.toLocaleString("zh-CN") : "未知";

export const formatRunMilliseconds = (value?: number | null): string => {
  const count = formatRunCount(value);
  return count === "未知" ? count : `${count} ms`;
};

export const formatRunDuration = (start?: string | null, end?: string | null): string => {
  if (!start || !end) return "未知";
  return formatRunMilliseconds(Date.parse(end) - Date.parse(start));
};

export const formatRunDate = (value?: string | null): string => {
  if (!value) return "未知";
  const date = new Date(value);
  return Number.isFinite(date.getTime()) ? date.toLocaleString("zh-CN", { hour12: false }) : "未知";
};

export const describeRunStatus = (status: string): { label: string; color: string } => {
  switch (status) {
    case "Completed": return { label: "已完成", color: "success" };
    case "Failed": return { label: "失败", color: "error" };
    case "Cancelled": return { label: "已取消", color: "default" };
    case "WaitingForApproval": return { label: "等待审批", color: "warning" };
    case "Running": return { label: "运行中", color: "processing" };
    default: return { label: status || "未知", color: "default" };
  }
};

export const describeRunUsage = (status?: string | null): { label: string; color: string } => {
  switch (status) {
    case "Reported": return { label: "完整统计", color: "success" };
    case "Partial": return { label: "部分统计", color: "warning" };
    default: return { label: "用量未知", color: "default" };
  }
};

export const describeRunTerminalEvent = (name: string, errorCode?: string): string | undefined => {
  const suffix = errorCode ? ` · ${errorCode}` : "";
  switch (name) {
    case "completed": return "运行完成";
    case "failed": return `运行失败${suffix}`;
    case "cancelled": return `运行已取消${suffix}`;
    case "approvalrequired": return "等待审批（运行暂停）";
    default: return undefined;
  }
};
