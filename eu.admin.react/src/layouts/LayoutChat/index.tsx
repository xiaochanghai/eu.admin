import React, { useEffect } from "react";
import { ChatMain } from "@/components/Chat";
import RouterGuard from "@/routers/helper/RouterGuard";
import { useNavigate } from "react-router-dom";
import { RootState, useSelector } from "@/redux";
import { LOGIN_URL } from "@/config";

const LayoutChat: React.FC = () => {
  const navigate = useNavigate();
  const token = useSelector((state: RootState) => state.user.token);

  // Redirect to login immediately when token becomes empty (after logout)
  useEffect(() => {
    if (!token) {
      navigate(LOGIN_URL, { replace: true });
    }
  }, [token]);

  return (
    <RouterGuard>
      <ChatMain />
    </RouterGuard>
  );
};

export default LayoutChat;
