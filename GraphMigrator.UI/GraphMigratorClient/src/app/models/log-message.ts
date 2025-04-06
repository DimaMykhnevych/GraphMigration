export interface LogMessage {
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  timestamp: Date;
}
