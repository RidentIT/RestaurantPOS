import { io, Socket } from "socket.io-client";
import { APP_CONFIG } from "../config";

let socket: Socket | null = null;

export function getSocketClient(): Socket {
  if (!socket) {
    socket = io(APP_CONFIG.wsUrl, {
      autoConnect: false,
      transports: ["websocket"],
    });
  }
  return socket;
}
