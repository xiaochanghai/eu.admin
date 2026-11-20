export const pageAnimationOptions = [
  { value: "none", label: "关闭" },
  { value: "fade", label: "淡入" },
  { value: "fade-slide", label: "滑动淡入" },
  { value: "fade-bottom", label: "垂直淡入" },
  { value: "fade-scale", label: "缩小淡入" },
  { value: "zoom-fade", label: "放大淡入" },
  { value: "zoom-out", label: "缩放进入" }
] as const;

export type PageAnimationMode = (typeof pageAnimationOptions)[number]["value"];
export const readPageAnimationMode = (value: unknown): PageAnimationMode =>
  pageAnimationOptions.find(option => option.value === value)?.value ?? "fade-slide";
