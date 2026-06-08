import { useState, useCallback, useEffect, useRef } from 'react';
import type {
  HttpMethod, Protocol, RequestConfig, ResponseData,
  WsMessage, HistoryItem, KeyValuePair, BodyType
} from './types';
import { buildUrlWithParams, getHttpFetchUrl, kvPairsToRecord, genId, normalizeHttpUrl, normalizeWsUrl } from './utils';
import RequestPanel from './components/RequestPanel';
import ResponsePanel from './components/ResponsePanel';
import WsPanel from './components/WsPanel';
import HistoryPanel from './components/HistoryPanel';
import './App.css';

// SVG Icons
const IconHistory = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M3 3v5h5"/><path d="M3.05 13A9 9 0 1 0 6 5.3L3 8"/>
    <path d="M12 7v5l4 2"/>
  </svg>
);

const IconSend = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 2L11 13"/><path d="M22 2L15 22L11 13L2 9L22 2Z"/>
  </svg>
);

const IconSun = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="12" cy="12" r="4" />
    <path d="M12 2v2" /><path d="M12 20v2" />
    <path d="m4.93 4.93 1.41 1.41" /><path d="m17.66 17.66 1.41 1.41" />
    <path d="M2 12h2" /><path d="M20 12h2" />
    <path d="m6.34 17.66-1.41 1.41" /><path d="m19.07 4.93-1.41 1.41" />
  </svg>
);

const IconMoon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M20.99 12.66A9 9 0 1 1 11.34 3a7 7 0 0 0 9.65 9.65Z" />
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
  const wsRef = useRef<WebSocket | null>(null);

  // History
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [showHistory, setShowHistory] = useState(true);

  // Splitter
  const [splitPos, setSplitPos] = useState(45);
  const dragging = useRef(false);

  // Sidebar resize
  const [sidebarWidth, setSidebarWidth] = useState(260);
  const sidebarDragging = useRef(false);

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

    const fullUrl = buildUrlWithParams(normalizedUrl, params);
    let headerRecord = kvPairsToRecord(headers);
    const willSendBody = !['GET', 'HEAD'].includes(method) && bodyType !== 'none' && Boolean(body.trim());

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

      // Add body for methods that support it
      if (willSendBody) {
        if (bodyType === 'json') {
          fetchOptions.body = body;
          if (!headerRecord['Content-Type']) {
            fetchOptions.headers = { ...headerRecord, 'Content-Type': 'application/json' };
          }
        } else if (bodyType === 'urlencoded') {
          fetchOptions.body = body;
          if (!headerRecord['Content-Type']) {
            fetchOptions.headers = { ...headerRecord, 'Content-Type': 'application/x-www-form-urlencoded' };
          }
        } else if (bodyType === 'formdata') {
          // Parse key=value pairs and create FormData
          const fd = new FormData();
          try {
            const obj = JSON.parse(body);
            for (const [k, v] of Object.entries(obj)) {
              fd.append(k, String(v));
            }
          } catch {
            body.split('&').forEach(pair => {
              const [k, ...rest] = pair.split('=');
              if (k) fd.append(k, rest.join('='));
            });
          }
          fetchOptions.body = fd;
          // Remove Content-Type to let browser set multipart boundary
          const h = { ...headerRecord };
          delete h['Content-Type'];
          fetchOptions.headers = h;
        } else {
          fetchOptions.body = body;
        }
      }

      const res = await fetch(getHttpFetchUrl(fullUrl), fetchOptions);
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

      // Save to history
      const historyItem: HistoryItem = {
        id: genId(),
        method,
        url: fullUrl,
        status: res.status,
        time,
        timestamp: new Date(),
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
        timestamp: new Date(),
        time,
        request: config,
      };
      setHistory(prev => [historyItem, ...prev].slice(0, 100));
    } finally {
      setLoading(false);
    }
  }, [url, params, headers, method, bodyType, body, protocol]);

  // === WebSocket ===
  const connectWs = useCallback(() => {
    if (!wsUrl.trim()) return;
    if (wsRef.current) {
      wsRef.current.close();
    }
    setWsProtocolInfo('');
    const normalizedUrl = normalizeWsUrl(wsUrl);
    if (normalizedUrl !== wsUrl.trim()) {
      setWsUrl(normalizedUrl);
    }

    const addMsg = (type: WsMessage['type'], data: string, size?: number) => {
      setWsMessages(prev => [...prev, {
        id: genId(), type, data, timestamp: new Date(), size,
      }]);
    };

    try {
      const ws = new WebSocket(normalizedUrl);
      wsRef.current = ws;

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
  }, [wsUrl]);

  const disconnectWs = useCallback(() => {
    if (wsRef.current) {
      wsRef.current.close();
      wsRef.current = null;
    }
    setWsConnected(false);
    setWsProtocolInfo('');
  }, []);

  const sendWsMessage = useCallback((msg: string) => {
    if (wsRef.current && wsRef.current.readyState === WebSocket.OPEN) {
      wsRef.current.send(msg);
      setWsMessages(prev => [...prev, {
        id: genId(), type: 'sent', data: msg, timestamp: new Date(), size: new Blob([msg]).size,
      }]);
    }
  }, []);

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

  // === Sidebar Resize ===
  const handleSidebarMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    sidebarDragging.current = true;
    const startX = e.clientX;
    const startWidth = sidebarWidth;

    const handleMouseMove = (e: MouseEvent) => {
      if (!sidebarDragging.current) return;
      const delta = e.clientX - startX;
      setSidebarWidth(Math.max(180, Math.min(500, startWidth + delta)));
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

  const hasBody = !['GET', 'HEAD'].includes(method);
  const responseSummary = loading
    ? 'Running'
    : error
      ? 'Failed'
      : response
        ? `${response.status} ${response.statusText}`
        : 'Ready';
  const nextTheme = theme === 'light' ? 'dark' : 'light';

  return (
    <div className="app" data-theme={theme}>
      {/* Header */}
      <header className="app-header">
        <div className="app-logo">
          <span className="logo-icon">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none">
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
        </span>
          <span className="logo-copy">
            <span className="logo-text">WebDog</span>
            <span className="logo-subtitle">API workspace</span>
          </span>
        </div>
        <div className="header-actions">
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
              WebSocket
            </button>
          </div>
          <button
            className="btn btn-icon"
            onClick={() => setTheme(nextTheme)}
            title={`Switch to ${nextTheme} theme`}
            aria-label={`Switch to ${nextTheme} theme`}
          >
            {theme === 'light' ? <IconMoon /> : <IconSun />}
          </button>
          <button
            className={`btn btn-icon ${showHistory ? 'active' : ''}`}
            onClick={() => setShowHistory(!showHistory)}
            title="Toggle history"
          >
            <IconHistory />
          </button>
        </div>
      </header>

      <div className="app-body">
        {/* History Sidebar */}
        {showHistory && (
          <>
            <aside className="sidebar" style={{ width: sidebarWidth }}>
              <HistoryPanel
                history={history}
                onSelect={selectHistory}
                onClear={clearHistory}
              />
            </aside>
            <div className="sidebar-resizer" onMouseDown={handleSidebarMouseDown}></div>
          </>
        )}

        {/* Main Content */}
        <div className="main-content">
          {protocol === 'http' ? (
            <>
              {/* Request Section */}
              <div className="request-section" style={{ flex: splitPos }}>
                <div className="section-chrome">
                  <div>
                    <span className="section-eyebrow">Request</span>
                    <strong>{method} endpoint</strong>
                  </div>
                  <span className="section-note">{hasBody ? `${bodyType.toUpperCase()} body` : 'No body'}</span>
                </div>
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
                    placeholder="Enter request URL"
                    spellCheck={false}
                  />
                  <button
                    className="btn btn-send-inline"
                    onClick={sendRequest}
                    disabled={loading || !url.trim()}
                  >
                    <IconSend />
                    {loading ? 'Sending...' : 'Send'}
                  </button>
                </div>
                <RequestPanel
                  params={params}
                  headers={headers}
                  bodyType={bodyType}
                  body={body}
                  onParamsChange={setParams}
                  onHeadersChange={setHeaders}
                  onBodyTypeChange={setBodyType}
                  onBodyChange={setBody}
                  hasBody={hasBody}
                />
              </div>

              {/* Splitter */}
              <div className="splitter" onMouseDown={handleMouseDown}>
                <div className="splitter-line"></div>
              </div>

              {/* Response Section */}
              <div className="response-section" style={{ flex: 100 - splitPos }}>
                <div className="section-chrome">
                  <div>
                    <span className="section-eyebrow">Response</span>
                    <strong>{responseSummary}</strong>
                  </div>
                  {response && <span className="section-note">{Object.keys(response.headers).length} headers</span>}
                </div>
                <ResponsePanel response={response} loading={loading} error={error} />
              </div>
            </>
          ) : (
            <div className="ws-container">
              <div className="section-chrome">
                <div>
                  <span className="section-eyebrow">WebSocket</span>
                  <strong>{wsConnected ? 'Live session' : 'New session'}</strong>
                </div>
                <span className={`section-note ${wsConnected ? 'is-live' : ''}`}>
                  {wsConnected ? 'Connected' : 'Disconnected'}
                </span>
              </div>
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
                  placeholder="ws://localhost:8080/ws"
                  spellCheck={false}
                />
                {!wsConnected ? (
                  <button
                    className="btn btn-send-inline"
                    onClick={connectWs}
                    disabled={!wsUrl.trim()}
                  >
                    Connect
                  </button>
                ) : (
                  <button className="btn btn-danger" onClick={disconnectWs}>
                    Disconnect
                  </button>
                )}
              </div>
              <div className="ws-status-bar">
                <span className={`ws-status ${wsConnected ? 'connected' : 'disconnected'}`}>
                  {wsConnected ? 'Connected' : 'Disconnected'}
                </span>
                {wsConnected && wsProtocolInfo && (
                  <span className="ws-protocol-info">
                    {wsProtocolInfo}
                  </span>
                )}
              </div>
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
