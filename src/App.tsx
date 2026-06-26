import { useState, useCallback, useEffect, useRef } from 'react';
import type {
  HttpMethod, Protocol, RequestConfig, ResponseData,
  WsMessage, HistoryItem, KeyValuePair, BodyType, EnvVariable
} from './types';
import {
  buildUrlWithParams, getHttpFetchUrl, isTauri, kvPairsToRecord,
  genId, normalizeHttpUrl, normalizeWsUrl, replaceEnvVars,
  saveHistoryToStorage, loadHistoryFromStorage,
  saveEnvToStorage, loadEnvFromStorage,
} from './utils';
import RequestPanel from './components/RequestPanel';
import ResponsePanel from './components/ResponsePanel';
import WsPanel from './components/WsPanel';
import HistoryPanel from './components/HistoryPanel';
import EnvPanel from './components/EnvPanel';
import './App.css';

// SVG Icons
const IconSend = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 2L11 13"/><path d="M22 2L15 22L11 13L2 9L22 2Z"/>
  </svg>
);

const IconSun = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="12" cy="12" r="4" />
    <path d="M12 2v2" /><path d="M12 20v2" />
    <path d="m4.93 4.93 1.41 1.41" /><path d="m17.66 17.66 1.41 1.41" />
    <path d="M2 12h2" /><path d="M20 12h2" />
    <path d="m6.34 17.66-1.41 1.41" /><path d="m19.07 4.93-1.41 1.41" />
  </svg>
);

const IconMoon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M20.99 12.66A9 9 0 1 1 11.34 3a7 7 0 0 0 9.65 9.65Z" />
  </svg>
);

const IconSidebar = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><line x1="9" y1="3" x2="9" y2="21"/>
  </svg>
);

const IconEnv = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
    <path d="m9 12 2 2 4-4"/>
  </svg>
);

const METHOD_COLORS: Record<string, string> = {
  GET: '#38bdf8',
  POST: '#34d399',
  PUT: '#f59e0b',
  DELETE: '#fb7185',
  PATCH: '#2dd4bf',
  HEAD: '#94a3b8',
  OPTIONS: '#60a5fa',
};

const DEFAULT_HEADERS: KeyValuePair[] = [];
type Theme = 'light' | 'dark';
const THEME_STORAGE_KEY = 'webdog-theme';

const getInitialTheme = (): Theme => {
  if (typeof window === 'undefined') return 'light';
  const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
  return stored === 'dark' || stored === 'light' ? stored : 'light';
};

function App() {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    window.localStorage.setItem(THEME_STORAGE_KEY, theme);
  }, [theme]);

  // Protocol mode
  const [protocol, setProtocol] = useState<Protocol>('http');

  // HTTP request state
  const [method, setMethod] = useState<HttpMethod>('GET');
  const [url, setUrl] = useState('');
  const [params, setParams] = useState<KeyValuePair[]>([]);
  const [headers, setHeaders] = useState<KeyValuePair[]>([...DEFAULT_HEADERS]);
  const [bodyType, setBodyType] = useState<BodyType>('json');
  const [body, setBody] = useState('');

  // HTTP response state
  const [response, setResponse] = useState<ResponseData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // WebSocket state
  const [wsUrl, setWsUrl] = useState('');
  const [wsMessages, setWsMessages] = useState<WsMessage[]>([]);
  const [wsConnected, setWsConnected] = useState(false);
  const [wsProtocolInfo, setWsProtocolInfo] = useState('');
  type WsHandle =
    | { kind: 'native'; ws: WebSocket }
    | { kind: 'tauri'; conn: import('@tauri-apps/plugin-websocket').default; unlisten: () => void };
  const wsRef = useRef<WsHandle | null>(null);

  // History
  const [history, setHistory] = useState<HistoryItem[]>(() => loadHistoryFromStorage());
  const [showHistory, setShowHistory] = useState(false);

  // Environment variables
  const [envVars, setEnvVars] = useState<EnvVariable[]>(() => loadEnvFromStorage());
  const [showEnvPanel, setShowEnvPanel] = useState(false);

  // Splitter
  const [splitPos, setSplitPos] = useState(45);
  const dragging = useRef(false);

  // Sidebar resize
  const [sidebarWidth, setSidebarWidth] = useState(260);
  const sidebarDragging = useRef(false);

  // Persist history
  useEffect(() => {
    saveHistoryToStorage(history);
  }, [history]);

  // Persist env vars
  useEffect(() => {
    saveEnvToStorage(envVars);
  }, [envVars]);

  // Detect protocol from URL
  const handleUrlChange = (u: string) => {
    setUrl(u);
    if (u.startsWith('ws://') || u.startsWith('wss://')) {
      if (protocol !== 'ws') {
        setProtocol('ws');
        setWsUrl(u);
      } else {
        setWsUrl(u);
      }
    }
  };

  const handleProtocolSwitch = (p: Protocol) => {
    setProtocol(p);
    if (p === 'ws' && url) {
      setWsUrl(normalizeWsUrl(url));
    }
  };

  // Apply env vars to a string
  const applyEnv = useCallback((text: string) => replaceEnvVars(text, envVars), [envVars]);

  // Apply env vars to key-value pairs
  const applyEnvToPairs = useCallback((pairs: KeyValuePair[]): KeyValuePair[] =>
    pairs.map(p => ({ ...p, key: applyEnv(p.key), value: applyEnv(p.value) })),
  [applyEnv]);

  // === HTTP Request ===
  const sendRequest = useCallback(async () => {
    if (!url.trim()) return;

    setLoading(true);
    setError(null);
    setResponse(null);

    const normalizedUrl = normalizeHttpUrl(url);
    if (normalizedUrl !== url.trim()) {
      setUrl(normalizedUrl);
    }

    // Apply environment variables
    const envUrl = applyEnv(normalizedUrl);
    const envParams = applyEnvToPairs(params);
    const envHeaders = applyEnvToPairs(headers);
    const envBody = applyEnv(body);

    const fullUrl = buildUrlWithParams(envUrl, envParams);
    let headerRecord = kvPairsToRecord(envHeaders);
    const willSendBody = !['GET', 'HEAD'].includes(method) && bodyType !== 'none' && Boolean(envBody.trim());

    if (!willSendBody) {
      const headersWithoutBodyType = { ...headerRecord };
      delete headersWithoutBodyType['Content-Type'];
      delete headersWithoutBodyType['content-type'];
      headerRecord = headersWithoutBodyType;
    }

    const config: RequestConfig = {
      method, url: normalizedUrl, protocol, params, headers, bodyType, body,
    };

    const startTime = performance.now();

    try {
      const fetchOptions: RequestInit = {
        method,
        headers: headerRecord,
      };

      if (willSendBody) {
        if (bodyType === 'json') {
          fetchOptions.body = envBody;
          if (!headerRecord['Content-Type']) {
            fetchOptions.headers = { ...headerRecord, 'Content-Type': 'application/json' };
          }
        } else if (bodyType === 'urlencoded') {
          fetchOptions.body = envBody;
          if (!headerRecord['Content-Type']) {
            fetchOptions.headers = { ...headerRecord, 'Content-Type': 'application/x-www-form-urlencoded' };
          }
        } else if (bodyType === 'formdata') {
          const fd = new FormData();
          try {
            const obj = JSON.parse(envBody);
            for (const [k, v] of Object.entries(obj)) {
              fd.append(k, String(v));
            }
          } catch {
            envBody.split('&').forEach(pair => {
              const [k, ...rest] = pair.split('=');
              if (k) fd.append(k, rest.join('='));
            });
          }
          fetchOptions.body = fd;
          const h = { ...headerRecord };
          delete h['Content-Type'];
          fetchOptions.headers = h;
        } else {
          fetchOptions.body = envBody;
        }
      }

      const doFetch = isTauri()
        ? (await import('@tauri-apps/plugin-http')).fetch
        : window.fetch;
      const res = await doFetch(getHttpFetchUrl(fullUrl), fetchOptions as any);
      const endTime = performance.now();
      const time = Math.round(endTime - startTime);

      const resHeaders: Record<string, string> = {};
      res.headers.forEach((v, k) => { resHeaders[k] = v; });

      const resBody = await res.text();
      const size = new Blob([resBody]).size;

      const responseData: ResponseData = {
        status: res.status,
        statusText: res.statusText,
        headers: resHeaders,
        body: resBody,
        time,
        size,
      };

      setResponse(responseData);

      const historyItem: HistoryItem = {
        id: genId(),
        method,
        url: fullUrl,
        status: res.status,
        time,
        timestamp: new Date().toISOString(),
        request: config,
        response: responseData,
      };
      setHistory(prev => [historyItem, ...prev].slice(0, 100));
    } catch (err) {
      const endTime = performance.now();
      const time = Math.round(endTime - startTime);
      const errorMsg = err instanceof Error ? err.message : String(err);
      const normalizedMessage = errorMsg === 'Failed to fetch'
        ? [
          'Failed to fetch',
          '',
          'The browser could not complete this request. Check that the backend is running, the URL is correct, and the backend allows browser requests (CORS).',
          '',
          'For cross-origin APIs, the backend needs Access-Control-Allow-Origin and may need to handle OPTIONS preflight requests.',
        ].join('\n')
        : errorMsg;
      setError(normalizedMessage);

      const historyItem: HistoryItem = {
        id: genId(),
        method,
        url: fullUrl,
        timestamp: new Date().toISOString(),
        time,
        request: config,
      };
      setHistory(prev => [historyItem, ...prev].slice(0, 100));
    } finally {
      setLoading(false);
    }
  }, [url, params, headers, method, bodyType, body, protocol, applyEnv, applyEnvToPairs]);

  // === WebSocket ===
  const connectWs = useCallback(() => {
    if (!wsUrl.trim()) return;
    const existing = wsRef.current;
    if (existing) {
      if (existing.kind === 'native') existing.ws.close();
      else { existing.unlisten(); void existing.conn.disconnect(); }
      wsRef.current = null;
    }
    setWsProtocolInfo('');
    const normalizedUrl = normalizeWsUrl(applyEnv(wsUrl));
    if (normalizedUrl !== wsUrl.trim()) {
      setWsUrl(normalizedUrl);
    }

    const addMsg = (type: WsMessage['type'], data: string, size?: number) => {
      setWsMessages(prev => [...prev, {
        id: genId(), type, data, timestamp: new Date(), size,
      }]);
    };

    if (isTauri()) {
      void import('@tauri-apps/plugin-websocket').then((module) => {
        const WS = module.default;
        WS.connect(normalizedUrl)
          .then((conn) => {
            const unlisten = conn.addListener((msg) => {
              const t = msg.type;
              if (t === 'Text') {
                const data = msg.data;
                addMsg('received', data, new Blob([data]).size);
              } else if (t === 'Binary') {
                addMsg('received', '[binary]');
              } else if (t === 'Close') {
                setWsConnected(false);
                setWsProtocolInfo('');
                const frame = msg.data;
                const detail = frame ? ` (code: ${frame.code}, reason: ${frame.reason || 'N/A'})` : '';
                addMsg('info', `Connection closed${detail}`);
                wsRef.current = null;
              }
            });
            wsRef.current = { kind: 'tauri', conn, unlisten };
            setWsConnected(true);
            setWsProtocolInfo('default protocol');
            addMsg('info', `Connected to ${normalizedUrl}`);
          })
          .catch((err) => {
            const msg = err instanceof Error ? err.message : String(err);
            setError(msg);
            addMsg('error', msg);
          });
      });
      return;
    }

    try {
      const ws = new WebSocket(normalizedUrl);
      wsRef.current = { kind: 'native', ws };

      ws.onopen = () => {
        setWsConnected(true);
        setWsProtocolInfo(ws.protocol || 'default protocol');
        addMsg('info', `Connected to ${normalizedUrl}`);
      };

      ws.onmessage = (event) => {
        const data = typeof event.data === 'string' ? event.data : JSON.stringify(event.data);
        addMsg('received', data, new Blob([data]).size);
      };

      ws.onerror = () => {
        addMsg('error', 'Connection error occurred');
      };

      ws.onclose = (event) => {
        setWsConnected(false);
        setWsProtocolInfo('');
        addMsg('info', `Connection closed (code: ${event.code}, reason: ${event.reason || 'N/A'})`);
        wsRef.current = null;
      };
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  }, [wsUrl, applyEnv]);

  const disconnectWs = useCallback(() => {
    const handle = wsRef.current;
    if (handle) {
      if (handle.kind === 'native') {
        handle.ws.close();
      } else {
        handle.unlisten();
        void handle.conn.disconnect();
      }
      wsRef.current = null;
    }
    setWsConnected(false);
    setWsProtocolInfo('');
  }, []);

  const sendWsMessage = useCallback((msg: string) => {
    const handle = wsRef.current;
    if (!handle) return;
    const finalMsg = applyEnv(msg);
    if (handle.kind === 'native') {
      if (handle.ws.readyState === WebSocket.OPEN) {
        handle.ws.send(finalMsg);
        setWsMessages(prev => [...prev, {
          id: genId(), type: 'sent', data: finalMsg, timestamp: new Date(), size: new Blob([finalMsg]).size,
        }]);
      }
    } else {
      void handle.conn.send(finalMsg);
      setWsMessages(prev => [...prev, {
        id: genId(), type: 'sent', data: finalMsg, timestamp: new Date(), size: new Blob([finalMsg]).size,
      }]);
    }
  }, [applyEnv]);

  // === History ===
  const selectHistory = useCallback((item: HistoryItem) => {
    setProtocol(item.request.protocol);
    setMethod(item.method as HttpMethod);
    setUrl(item.request.url);
    setParams(item.request.params);
    setHeaders(item.request.headers);
    setBodyType(item.request.bodyType);
    setBody(item.request.body);
    setResponse(item.response || null);
    setError(null);
  }, []);

  const clearHistory = useCallback(() => setHistory([]), []);
  const deleteHistoryItem = useCallback((id: string) => {
    setHistory(prev => prev.filter(h => h.id !== id));
  }, []);

  // === Sidebar Resize ===
  const handleSidebarMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    sidebarDragging.current = true;
    const startX = e.clientX;
    const startWidth = sidebarWidth;

    const handleMouseMove = (e: MouseEvent) => {
      if (!sidebarDragging.current) return;
      const delta = e.clientX - startX;
      setSidebarWidth(Math.max(200, Math.min(480, startWidth + delta)));
    };
    const handleMouseUp = () => {
      sidebarDragging.current = false;
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, [sidebarWidth]);

  // === Splitter ===
  const handleMouseDown = useCallback(() => {
    dragging.current = true;
    const handleMouseMove = (e: MouseEvent) => {
      if (!dragging.current) return;
      const container = document.querySelector('.main-content');
      if (container) {
        const rect = container.getBoundingClientRect();
        const stacked = window.matchMedia('(max-width: 960px)').matches;
        const pos = stacked
          ? ((e.clientY - rect.top) / rect.height) * 100
          : ((e.clientX - rect.left) / rect.width) * 100;
        setSplitPos(Math.max(28, Math.min(72, pos)));
      }
    };
    const handleMouseUp = () => {
      dragging.current = false;
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, []);

  const nextTheme = theme === 'light' ? 'dark' : 'light';

  return (
    <div className="app" data-theme={theme}>
      {/* Header - compact toolbar */}
      <header className="app-header">
        <div className="header-left">
          <div className="app-logo">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none">
              <path d="M12 2L2 7l10 5 10-5-10-5z" fill="url(#g1)" opacity="0.9"/>
              <path d="M2 17l10 5 10-5" stroke="url(#g1)" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
              <path d="M2 12l10 5 10-5" stroke="url(#g1)" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
              <defs>
                <linearGradient id="g1" x1="0" y1="0" x2="24" y2="24">
                  <stop stopColor="#2dd4bf"/>
                  <stop offset="1" stopColor="#f59e0b"/>
                </linearGradient>
              </defs>
            </svg>
            <span className="logo-text">WebDog</span>
          </div>

          {/* Protocol toggle - integrated into header left */}
          <div className="protocol-toggle">
            <button
              className={`protocol-btn ${protocol === 'http' ? 'active' : ''}`}
              onClick={() => handleProtocolSwitch('http')}
            >
              HTTP
            </button>
            <button
              className={`protocol-btn ${protocol === 'ws' ? 'active' : ''}`}
              onClick={() => handleProtocolSwitch('ws')}
            >
              WS
            </button>
          </div>
        </div>

        <div className="header-right">
          <button
            className={`btn btn-icon-sm ${showEnvPanel ? 'active' : ''}`}
            onClick={() => setShowEnvPanel(!showEnvPanel)}
            title="Toggle environment panel"
          >
            <IconEnv />
          </button>
          <button
            className={`btn btn-icon-sm ${showHistory ? 'active' : ''}`}
            onClick={() => setShowHistory(!showHistory)}
            title="Toggle history panel"
          >
            <IconSidebar />
          </button>
          <button
            className="btn btn-icon-sm"
            onClick={() => setTheme(nextTheme)}
            title={`Switch to ${nextTheme} theme`}
            aria-label={`Switch to ${nextTheme} theme`}
          >
            {theme === 'light' ? <IconMoon /> : <IconSun />}
          </button>
        </div>
      </header>

      <div className="app-body">
        {/* Environment Panel */}
        {showEnvPanel && (
          <>
            <aside className="sidebar env-sidebar" style={{ width: 300 }}>
              <EnvPanel envVars={envVars} onChange={setEnvVars} />
            </aside>
            <div className="sidebar-resizer" onMouseDown={handleSidebarMouseDown}></div>
          </>
        )}

        {/* History Sidebar */}
        {showHistory && (
          <>
            <aside className="sidebar" style={{ width: sidebarWidth }}>
              <HistoryPanel
                history={history}
                onSelect={selectHistory}
                onClear={clearHistory}
                onDelete={deleteHistoryItem}
              />
            </aside>
            <div className="sidebar-resizer" onMouseDown={handleSidebarMouseDown}></div>
          </>
        )}

        {/* Main Content */}
        <div className="main-content">
          {protocol === 'http' ? (
            <>
              {/* URL Bar - extracted to top level for prominence */}
              <div className="url-bar-dock">
                <div className="url-bar">
                  <select
                    className="method-select"
                    value={method}
                    onChange={e => setMethod(e.target.value as HttpMethod)}
                    style={{ color: METHOD_COLORS[method] || '#94a3b8' }}
                  >
                    {['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'].map(m => (
                      <option key={m} value={m}>{m}</option>
                    ))}
                  </select>
                  <input
                    type="text"
                    className="url-input"
                    value={url}
                    onChange={e => handleUrlChange(e.target.value)}
                    onKeyDown={e => { if (e.key === 'Enter' && !loading) sendRequest(); }}
                    placeholder="Enter request URL..."
                    spellCheck={false}
                  />
                  <button
                    className="btn btn-send"
                    onClick={sendRequest}
                    disabled={loading || !url.trim()}
                  >
                    <IconSend />
                    {loading ? 'Sending' : 'Send'}
                  </button>
                </div>
              </div>

              <div className="panels-container">
                {/* Request Panel */}
                <div className="request-section" style={{ flex: splitPos }}>
                  <RequestPanel
                    params={params}
                    headers={headers}
                    bodyType={bodyType}
                    body={body}
                    onParamsChange={setParams}
                    onHeadersChange={setHeaders}
                    onBodyTypeChange={setBodyType}
                    onBodyChange={setBody}
                    hasBody={!['GET', 'HEAD'].includes(method)}
                  />
                </div>

                {/* Splitter */}
                <div className="splitter" onMouseDown={handleMouseDown}>
                  <div className="splitter-handle"></div>
                </div>

                {/* Response Panel */}
                <div className="response-section" style={{ flex: 100 - splitPos }}>
                  <ResponsePanel response={response} loading={loading} error={error} />
                </div>
              </div>
            </>
          ) : (
            <div className="ws-container">
              {/* WS URL Bar */}
              <div className="url-bar-dock">
                <div className="ws-url-bar">
                  <select
                    className="method-select ws-method"
                    value={wsUrl.startsWith('wss://') ? 'wss' : 'ws'}
                    onChange={e => {
                      const newScheme = e.target.value;
                      setWsUrl(wsUrl.replace(/^wss?/, newScheme));
                    }}
                    style={{ color: '#2dd4bf' }}
                  >
                    <option value="ws">WS</option>
                    <option value="wss">WSS</option>
                  </select>
                  <input
                    type="text"
                    className="url-input"
                    value={wsUrl}
                    onChange={e => setWsUrl(e.target.value)}
                    onKeyDown={e => { if (e.key === 'Enter' && !wsConnected) connectWs(); }}
                    placeholder="ws://localhost:8080/ws"
                    spellCheck={false}
                  />
                  {!wsConnected ? (
                    <button
                      className="btn btn-send"
                      onClick={connectWs}
                      disabled={!wsUrl.trim()}
                    >
                      Connect
                    </button>
                  ) : (
                    <button className="btn btn-disconnect" onClick={disconnectWs}>
                      Disconnect
                    </button>
                  )}
                </div>
              </div>

              {/* WS Status */}
              {wsConnected && (
                <div className="ws-status-strip">
                  <span className="ws-status-dot connected"></span>
                  <span>Connected</span>
                  {wsProtocolInfo && (
                    <span className="ws-proto-badge">{wsProtocolInfo}</span>
                  )}
                </div>
              )}

              <WsPanel
                messages={wsMessages}
                onSend={sendWsMessage}
                onClear={() => setWsMessages([])}
                connected={wsConnected}
              />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
