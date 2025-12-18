import React from 'react';
import {
    useXChat,
} from '@ant-design/x-sdk';
import { ChatMessage } from "./index";


export const ChatContext = React.createContext<{
    onReload?: ReturnType<typeof useXChat>['onReload'];
    setMessage?: ReturnType<typeof useXChat<ChatMessage>>['setMessage'];
}>({});