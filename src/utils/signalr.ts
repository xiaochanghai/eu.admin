import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

const signalr = new HubConnectionBuilder().withUrl("/api/api2/chatHub").configureLogging(LogLevel.None).build();

export default signalr;
