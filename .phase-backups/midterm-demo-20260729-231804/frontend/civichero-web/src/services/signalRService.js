import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useAuthStore } from '../store/authStore.js';
import { useNotificationStore } from '../store/notificationStore.js';

let connection;

export async function startNotificationConnection() {
  const token = useAuthStore.getState().accessToken;
  if (!token || connection?.state === 'Connected' || connection?.state === 'Connecting') return;

  connection = new HubConnectionBuilder()
    .withUrl('/hubs/notifications', { accessTokenFactory: () => useAuthStore.getState().accessToken || '' })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on('notificationReceived', (notification) => useNotificationStore.getState().receive(notification));
  connection.onreconnecting(() => useNotificationStore.getState().setConnectionState('reconnecting'));
  connection.onreconnected(() => useNotificationStore.getState().setConnectionState('connected'));
  connection.onclose(() => useNotificationStore.getState().setConnectionState('disconnected'));

  try {
    useNotificationStore.getState().setConnectionState('connecting');
    await connection.start();
    useNotificationStore.getState().setConnectionState('connected');
  } catch {
    useNotificationStore.getState().setConnectionState('disconnected');
  }
}

export async function stopNotificationConnection() {
  if (connection) {
    try { await connection.stop(); } catch { /* connection may already be closed */ }
    connection = undefined;
  }
  useNotificationStore.getState().setConnectionState('disconnected');
}
