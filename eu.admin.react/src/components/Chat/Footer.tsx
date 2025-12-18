import React from "react";
import { Actions } from "@ant-design/x";
import { Icon } from "@/components";
import { ChatMessage } from "./index";
import { ChatContext } from "./ChatContext";
import { message, Pagination } from "antd";
import { useTranslation } from "react-i18next";

export const Footer: React.FC<{
  id?: string | number;
  content: string;
  status?: string;
  extraInfo?: ChatMessage["extraInfo"];
}> = ({ id, content, extraInfo, status }) => {
  const context = React.useContext(ChatContext);
  const { t } = useTranslation();

  const Items = [
    {
      key: "pagination",
      actionRender: <Pagination simple total={1} pageSize={1} />
    },
    {
      key: "retry",
      label: t("chat.retry"),
      icon: <Icon name="SyncOutlined" />,
      onItemClick: () => {
        if (id) {
          context?.onReload?.(id, {
            userAction: "retry"
          });
        }
      }
    },
    {
      key: "copy",
      actionRender: <Actions.Copy text={content} />
    },
    {
      key: "audio",
      actionRender: (
        <Actions.Audio
          onClick={() => {
            message.info(t("chat.isMock"));
          }}
        />
      )
    },
    {
      key: "feedback",
      actionRender: (
        <Actions.Feedback
          styles={{
            liked: {
              color: "#f759ab"
            }
          }}
          value={extraInfo?.feedback || "default"}
          key="feedback"
          onChange={val => {
            if (id) {
              context?.setMessage?.(id, () => ({
                extraInfo: {
                  feedback: val
                }
              }));
              message.success(`${id}: ${val}`);
            } else {
              message.error("has no id!");
            }
          }}
        />
      )
    }
  ];
  return status !== "updating" && status !== "loading" ? (
    <div style={{ display: "flex" }}>{id && <Actions items={Items} />}</div>
  ) : null;
};
