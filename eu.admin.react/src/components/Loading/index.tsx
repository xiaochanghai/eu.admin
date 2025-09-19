import { useEffect } from "react";
import { Spin } from "antd";
import { Skeleton as AntSkeleton, Card } from "antd";
import NProgress from "@/config/nprogress";
import "./index.less";
interface SkeletonLoadingProps {
  type?: "default" | "form" | "table" | "card" | "simple";
  rows?: number;
  active?: boolean;
}
export const Loading = () => {
  return (
    <div className="loading-box">
      <Spin size="large" />
    </div>
  );
};

export const PageLoader = () => {
  useEffect(() => {
    NProgress.start();
    return () => {
      NProgress.done();
    };
  }, []);

  return <Loading />;
};
export const Skeleton = ({ type = "default", rows = 3, active = true }: SkeletonLoadingProps = {}) => {
  const renderSkeleton = () => {
    switch (type) {
      case "form":
        return (
          <div className="skeleton-box">
            <Card size="small" style={{ width: "100%" }}>
              <AntSkeleton.Input active={active} style={{ width: 200, marginBottom: 16 }} />
              <AntSkeleton active={active} paragraph={{ rows: 2 }} />
              <AntSkeleton.Input active={active} style={{ width: 150, marginBottom: 16 }} />
              <AntSkeleton active={active} paragraph={{ rows: 2 }} />
              <div style={{ display: "flex", gap: 8, marginTop: 16 }}>
                <AntSkeleton.Button active={active} />
                <AntSkeleton.Button active={active} />
              </div>
            </Card>
          </div>
        );

      case "table":
        return (
          <div className="skeleton-box">
            <AntSkeleton.Input active={active} style={{ width: 900, marginBottom: 16 }} />
            <AntSkeleton active={active} paragraph={{ rows: 6 }} />
            <div style={{ display: "flex", justifyContent: "space-between", marginTop: 16 }}>
              <AntSkeleton.Button active={active} size="small" />
              <div style={{ display: "flex", gap: 8 }}>
                <AntSkeleton.Button active={active} size="small" />
                <AntSkeleton.Button active={active} size="small" />
              </div>
            </div>
          </div>
        );

      case "card":
        return (
          <div className="skeleton-box">
            <Card size="small">
              <AntSkeleton active={active} avatar paragraph={{ rows: 2 }} />
            </Card>
          </div>
        );

      case "simple":
        return (
          <div className="skeleton-box">
            <AntSkeleton active={active} paragraph={{ rows }} />
          </div>
        );

      default:
        return (
          <div className="skeleton-box">
            <AntSkeleton active={active} paragraph={{ rows: 4 }} />
            <div style={{ marginTop: 16 }}>
              <AntSkeleton active={active} paragraph={{ rows: 3 }} />
            </div>
          </div>
        );
    }
  };

  return renderSkeleton();
};
