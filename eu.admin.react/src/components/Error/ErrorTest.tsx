import { useState } from "react";
import { Button, Card, Space } from "antd";

/**
 * 测试组件 - 用于验证 ErrorBoundary 功能
 * 仅用于开发环境测试
 */
const ErrorTest = () => {
  const [shouldThrow, setShouldThrow] = useState(false);

  if (shouldThrow) {
    // 模拟组件崩溃
    throw new Error("这是一个测试错误！ErrorBoundary 应该捕获此错误。");
  }

  const throwAsyncError = () => {
    // 异步错误不会被 ErrorBoundary 捕获
    setTimeout(() => {
      throw new Error("异步错误 - ErrorBoundary 无法捕获");
    }, 100);
  };

  const throwPromiseError = () => {
    // Promise 错误不会被 ErrorBoundary 捕获
    Promise.reject(new Error("Promise 错误 - ErrorBoundary 无法捕获"));
  };

  return (
    <div style={{ padding: 24 }}>
      <Card title="ErrorBoundary 测试工具" style={{ maxWidth: 600 }}>
        <Space orientation="vertical" style={{ width: "100%" }} size="large">
          <div>
            <h3>测试 1: 渲染错误（会被捕获）</h3>
            <p>点击按钮后组件会抛出错误，ErrorBoundary 应该显示错误页面。</p>
            <Button type="primary" danger onClick={() => setShouldThrow(true)}>
              触发渲染错误
            </Button>
          </div>

          <div>
            <h3>测试 2: 异步错误（不会被捕获）</h3>
            <p>ErrorBoundary 无法捕获异步错误，会在控制台显示。</p>
            <Button onClick={throwAsyncError}>触发异步错误（查看控制台）</Button>
          </div>

          <div>
            <h3>测试 3: Promise 错误（不会被捕获）</h3>
            <p>ErrorBoundary 无法捕获 Promise 错误，会在控制台显示。</p>
            <Button onClick={throwPromiseError}>触发 Promise 错误（查看控制台）</Button>
          </div>

          <div style={{ marginTop: 16, padding: 12, background: "#f0f0f0", borderRadius: 4 }}>
            <strong>说明：</strong>
            <ul style={{ marginBottom: 0, paddingLeft: 20 }}>
              <li>ErrorBoundary 只能捕获渲染期间的错误</li>
              <li>事件处理器中的异步错误需要使用 try-catch 处理</li>
              <li>测试完成后请刷新页面恢复正常</li>
            </ul>
          </div>
        </Space>
      </Card>
    </div>
  );
};

export default ErrorTest;
