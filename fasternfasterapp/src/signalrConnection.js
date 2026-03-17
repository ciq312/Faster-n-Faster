import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

const connection = new HubConnectionBuilder()
  .withUrl("/gameHub")
  .withAutomaticReconnect([2000, 5000, 10000, 30000])
  .configureLogging(LogLevel.Information)
  .build();

export default connection;
