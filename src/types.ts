export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH' | 'HEAD' | 'OPTIONS';

export type Protocol = 'http' | 'ws';

export interface KeyValuePair {
  key: string;
  value: string;
  enabled: boolean;
  id: string;
}

export type BodyType = 'none' | 'json' | 'text' | 'formdata' | 'urlencoded' | 'raw';

export interface RequestConfig {
  method: HttpMethod;
  url: string;
  protocol: Protocol;
  params: KeyValuePair[];
  headers: KeyValuePair[];
  bodyType: BodyType;
  body: string;
}

export interface ResponseData {
  status: number;
  statusText: string;
  headers: Record<string, string>;
  body: string;
  time: number;
  size: number;
}

export interface WsMessage {
  id: string;
  type: 'sent' | 'received' | 'info' | 'error';
  data: string;
  timestamp: Date;
  size?: number;
}

export interface HistoryItem {
  id: string;
  method: HttpMethod | 'WS' | 'WSS';
  url: string;
  status?: number;
  time?: number;
  timestamp: Date;
  request: RequestConfig;
  response?: ResponseData;
}
