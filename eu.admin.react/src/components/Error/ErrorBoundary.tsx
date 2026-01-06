import { Component, ErrorInfo, ReactNode } from "react";
import { Button, Result } from "antd";
import "./index.less";

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null
    };
  }

  static getDerivedStateFromError(_error: Error): Partial<State> {
    // 更新 state 使下一次渲染能够显示降级后的 UI
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // 你同样可以将错误日志上报给服务器
    console.error("ErrorBoundary caught an error:", error, errorInfo);
    this.setState({
      error,
      errorInfo
    });
  }

  handleReload = () => {
    window.location.reload();
  };

  handleGoHome = () => {
    window.location.href = "/";
  };

  render() {
    if (this.state.hasError) {
      return (
        <Result
          className="error-page"
          status="error"
          title="页面崩溃了"
          subTitle="抱歉，页面发生了错误。您可以尝试刷新页面或返回首页。"
          extra={[
            <Button type="primary" key="reload" onClick={this.handleReload}>
              刷新页面
            </Button>,
            <Button key="home" onClick={this.handleGoHome}>
              返回首页
            </Button>
          ]}
        >
          {process.env.NODE_ENV === "development" && this.state.error && (
            <div style={{ textAlign: "left", maxWidth: 800, margin: "0 auto" }}>
              <details style={{ whiteSpace: "pre-wrap", fontSize: 12 }}>
                <summary style={{ cursor: "pointer", marginBottom: 10 }}>错误详情</summary>
                <div style={{ padding: 10, background: "#f5f5f5", borderRadius: 4 }}>
                  <strong>错误信息:</strong>
                  <pre>{this.state.error.toString()}</pre>
                  <strong>组件堆栈:</strong>
                  <pre>{this.state.errorInfo?.componentStack}</pre>
                </div>
              </details>
            </div>
          )}
        </Result>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
