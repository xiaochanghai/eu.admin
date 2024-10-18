import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

const signalr = new HubConnectionBuilder().withUrl("/api/api2/chatHub").configureLogging(LogLevel.Information).build();

export default signalr;
