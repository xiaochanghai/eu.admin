import { defineConfig, loadEnv, ConfigEnv, UserConfig } from "vite";
import { createVitePlugins } from "./build/plugins";
import { createProxy } from "./build/proxy";
import { wrapperEnv } from "./build/getEnv";
import { resolve } from "path";
import pkg from "./package.json";
import dayjs from "dayjs";

const { dependencies, devDependencies, name, version } = pkg;
const __APP_INFO__ = {
  pkg: { dependencies, devDependencies, name, version },
  lastBuildTime: dayjs().format("YYYY-MM-DD HH:mm:ss")
};

// @see: https://vitejs.dev/config/
export default defineConfig(({ mode }: ConfigEnv): UserConfig => {
  const root = process.cwd();
  const env = loadEnv(mode, root);
  const viteEnv = wrapperEnv(env);

  return {
    base: viteEnv.VITE_PUBLIC_PATH,
    root,
    resolve: {
      alias: {
        "@": resolve(__dirname, "./src")
      }
    },
    define: {
      __APP_INFO__: JSON.stringify(__APP_INFO__)
    },
    server: {
      host: "0.0.0.0",
      port: viteEnv.VITE_PORT,
      open: viteEnv.VITE_OPEN,
      cors: true,
      // Load proxy configuration from .env.development
      proxy: createProxy(viteEnv.VITE_PROXY)
    },
    plugins: createVitePlugins(viteEnv, __APP_INFO__.lastBuildTime),
    esbuild: {
      drop: viteEnv.VITE_DROP_CONSOLE ? ["console", "debugger"] : []
    },
    build: {
      outDir: "dist",
      minify: "esbuild",
      // esbuild is faster to package, but cannot remove console.log, terser is slower to package, but can remove console.log
      // minify: "terser",
      // terserOptions: {
      // 	compress: {
      // 		drop_console: viteEnv.VITE_DROP_CONSOLE,
      // 		drop_debugger: true
      // 	}
      // },
      sourcemap: false,
      // Disable gzip compressed size reporting, which slightly reduces pack time
      reportCompressedSize: false,
      // Determine the chunk size that triggers the warning
      chunkSizeWarningLimit: 5000,
      rollupOptions: {
        output: {
          // Static resource classification and packaging
          chunkFileNames: "assets/js/[name]-[hash].js",
          entryFileNames: "assets/js/[name]-[hash].js",
          assetFileNames: "assets/[ext]/[name]-[hash].[ext]",
          // 代码分割配置 - 对象方式（仅第三方包）
          manualChunks: {
            // 第三方UI库单独打包
            "antd-vendor": ["antd", "@ant-design/icons"],
            "antd-pro": ["@ant-design/pro-components"],
            "antd-x": ["@ant-design/x"],

            // 图表相关
            charts: ["echarts", "echarts-liquidfill", "@ant-design/plots"],

            // React生态
            "react-vendor": ["react", "react-dom", "react-router-dom"],

            // Redux状态管理
            "redux-vendor": ["react-redux", "@reduxjs/toolkit", "redux-persist"],

            // 工具库
            "utils-vendor": ["lodash", "dayjs", "axios", "classnames"],

            // 分离两套拖拽库
            "react-dnd-vendor": ["react-dnd", "react-dnd-html5-backend"],
            "dnd-kit-vendor": ["@dnd-kit/core", "@dnd-kit/sortable", "@dnd-kit/utilities"],

            // 编辑器相关
            "editor-vendor": ["react-ace", "markdown-it"],

            // 其他常用第三方库
            "misc-vendor": ["ahooks", "antd-style", "styled-components", "uuid", "qs", "md5"],
            "chat-module": [resolve(__dirname, "./src/components/Chat/index.tsx")]

            // // 工作流编辑器 (大模块)
            // "workflow-editor": [/.*workflow-editor.*/]

            // // 表单设计器 (大模块)
            // 'form-design': [/.*FormDesign.*/],

            // // 聊天模块
            // "chat-module": [/.*[Cc]hat.*/]
          }
        }
      }
    }
  };
});
