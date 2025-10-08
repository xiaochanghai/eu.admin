import React, { useEffect } from "react";
import { Watermark } from "antd";
import { RootState, useSelector } from "@/redux";
import LayoutVertical from "./LayoutVertical";
import LayoutClassic from "./LayoutClassic";
import LayoutTransverse from "./LayoutTransverse";
import LayoutColumns from "./LayoutColumns";
import ThemeDrawer from "@/layouts/components/ThemeDrawer";

import signalr from "@/utils/signalr";

const LayoutIndex: React.FC = () => {
  const layout = useSelector((state: RootState) => state.global.layout);
  const watermark = useSelector((state: RootState) => state.global.watermark);
  const userInfo = useSelector((state: RootState) => state.user.userInfo);
  let { UserId } = userInfo;
  const LayoutComponents = {
    vertical: <LayoutVertical />,
    classic: <LayoutClassic />,
    transverse: <LayoutTransverse />,
    columns: <LayoutColumns />
  };
  const connect = () => {
    signalr.start().catch((err: any) => {
      console.error("signalr 连接失败", err);
      setTimeout(() => connect(), 5000);
    });
  };
  useEffect(() => {
    connect();
    // 监听连接成功事件
    signalr.on("OnConnected", (connectionId: string) => {
      // let userId = Utility.getCache("userId");

      const encodedMsg = new Date().toLocaleString() + " " + connectionId;
      console.log("OnConnected", encodedMsg);

      // 使用UserId注册
      signalr.invoke("SendRegister", UserId).catch(err => console.log(err.toString()));
    });
    // 监听心跳包
    signalr.on("OnHeartbeat", message => {
      console.log("OnHeartbeat：", message);
    });
    // 监听接收消息
    signalr.on("OnReceiveMessage", message => {
      console.log("监听接收消息:", message);
      // const msg = JSON.parse(message);
      // const html = sanitizeHtml(msg.Content, {
      //   allowedTags: [],
      //   allowedAttributes: {}
      // });
      // this.$notification.info({
      //   message: msg.Title,
      //   description: html
      // });
    });
    // 监听OnConsole

    signalr.on("OnConsole", message => {
      console.log("OnConsole：", message);
    });

    // const vm = this
    signalr.onclose(() => {
      // if (vm.$store.state.app.enableHub) {
      //   console.log("signalr 断开准备重新连接...");
      //   // if (signalr !== "Disconnected") {
      //   //   signalr.stop();
      //   // }
      //   connect();
      // }
    });
  });

  return (
    <Watermark className="watermark-content" zIndex={1001} content={watermark ? ["SuZhou", "EU Cloud"] : []}>
      {LayoutComponents[layout]}
      <ThemeDrawer />
    </Watermark>
  );
};

export default LayoutIndex;
