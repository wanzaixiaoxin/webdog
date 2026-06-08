import { useState } from 'react';
import type { ResponseData } from '../types';
import { formatSize, formatTime, jsonHighlight } from '../utils';

const IconRocket = () => (
  <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ opacity: 0.25 }}>
    <path d="M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z"/>
    <path d="m12 15-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z"/>
    <path d="M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0"/>
    <path d="M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5"/>
  </svg>
);

interface Props {
  response: ResponseData | null;
  loading: boolean;
  error: string | null;
}

export default function ResponsePanel({ response, loading, error }: Props) {
  const [activeTab, setActiveTab] = useState<'body' | 'headers'>('body');
  const [bodyView, setBodyView] = useState<'pretty' | 'raw'>('pretty');

  if (loading) {
    return (
      <div className="response-panel">
        <div className="response-loading">
          <div className="spinner"></div>
          <span>Sending request...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="response-panel">
        <div className="response-error">
          <h3>Request Failed</h3>
          <pre>{error}</pre>
        </div>
      </div>
    );
  }

  if (!response) {
    return (
      <div className="response-panel">
        <div className="response-empty">
          <IconRocket />
          <h3>Ready</h3>
          <p>Response details will appear here.</p>
        </div>
      </div>
    );
  }

  const statusClass = response.status < 300 ? 'status-success' :
    response.status < 400 ? 'status-redirect' :
    response.status < 500 ? 'status-client-error' : 'status-server-error';

  const formatBody = () => {
    if (bodyView === 'raw') {
      return response.body.replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
    try {
      const obj = JSON.parse(response.body);
      return jsonHighlight(JSON.stringify(obj, null, 2));
    } catch {
      return response.body.replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
  };

  return (
    <div className="response-panel">
      <div className="response-status-bar">
        <span className={`response-status ${statusClass}`}>
          {response.status} {response.statusText}
        </span>
        <span className="response-meta">
          <span title="Response time">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ display: 'inline', verticalAlign: '-2px', marginRight: '4px' }}>
              <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
            </svg>
            {formatTime(response.time)}
          </span>
          <span title="Response size">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ display: 'inline', verticalAlign: '-2px', marginRight: '4px' }}>
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>
            </svg>
            {formatSize(response.size)}
          </span>
        </span>
      </div>

      <div className="response-tabs">
        <button
          className={`tab ${activeTab === 'body' ? 'active' : ''}`}
          onClick={() => setActiveTab('body')}
        >
          Body
        </button>
        <button
          className={`tab ${activeTab === 'headers' ? 'active' : ''}`}
          onClick={() => setActiveTab('headers')}
        >
          Headers ({Object.keys(response.headers).length})
        </button>
        {activeTab === 'body' && (
          <div className="body-view-toggle">
            <button
              className={`view-btn ${bodyView === 'pretty' ? 'active' : ''}`}
              onClick={() => setBodyView('pretty')}
            >
              Pretty
            </button>
            <button
              className={`view-btn ${bodyView === 'raw' ? 'active' : ''}`}
              onClick={() => setBodyView('raw')}
            >
              Raw
            </button>
          </div>
        )}
      </div>

      <div className="response-content">
        {activeTab === 'body' && (
          <pre
            className="response-body"
            dangerouslySetInnerHTML={{ __html: formatBody() }}
          />
        )}
        {activeTab === 'headers' && (
          <div className="response-headers">
            {Object.entries(response.headers).map(([key, value]) => (
              <div key={key} className="response-header-row">
                <span className="response-header-key">{key}</span>
                <span className="response-header-value">{value}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
