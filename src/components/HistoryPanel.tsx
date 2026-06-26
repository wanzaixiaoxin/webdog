import { useState } from 'react';
import type { HistoryItem } from '../types';

interface Props {
  history: HistoryItem[];
  onSelect: (item: HistoryItem) => void;
  onClear: () => void;
  onDelete?: (id: string) => void;
}

const IconTrashSmall = () => (
  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/>
    <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
  </svg>
);

export default function HistoryPanel({ history, onSelect, onClear, onDelete }: Props) {
  const [search, setSearch] = useState('');

  const filtered = history.filter(h =>
    h.url.toLowerCase().includes(search.toLowerCase()) ||
    h.method.toLowerCase().includes(search.toLowerCase())
  );

  const methodColor = (method: string) => {
    const colors: Record<string, string> = {
      GET: '#60a5fa',
      POST: '#4ade80',
      PUT: '#fbbf24',
      DELETE: '#f87171',
      PATCH: '#22d3ee',
      HEAD: '#94a3b8',
      OPTIONS: '#3b82f6',
      WS: '#2dd4bf',
      WSS: '#2dd4bf',
    };
    return colors[method] || '#94a3b8';
  };

  const statusClass = (status: number) => {
    if (status < 300) return 'status-success';
    if (status < 400) return 'status-redirect';
    if (status < 500) return 'status-client-error';
    return 'status-server-error';
  };

  return (
    <div className="history-panel">
      <div className="history-header">
        <h3>History</h3>
        {history.length > 0 && (
          <button className="btn btn-sm btn-secondary" onClick={onClear}>Clear</button>
        )}
      </div>
      <input
        type="text"
        className="history-search"
        placeholder="Search history..."
        value={search}
        onChange={e => setSearch(e.target.value)}
      />
      <div className="history-list">
        {filtered.length === 0 && (
          <div className="history-empty">No matching requests</div>
        )}
        {filtered.map(item => (
          <div
            key={item.id}
            className="history-item"
            title={item.url}
          >
            <div className="history-item-main" onClick={() => onSelect(item)}>
              <span
                className="history-method"
                style={{ backgroundColor: methodColor(item.method) }}
              >
                {item.method}
              </span>
              <span className="history-url">
                {item.url}
              </span>
              {item.status && (
                <span className={`history-status ${statusClass(item.status)}`}>
                  {item.status}
                </span>
              )}
              <span className="history-time-label">
                {new Date(item.timestamp).toLocaleTimeString()}
              </span>
            </div>
            {onDelete && (
              <button
                className="history-delete-btn"
                onClick={(e) => { e.stopPropagation(); onDelete(item.id); }}
                title="Delete"
                aria-label="Delete history item"
              >
                <IconTrashSmall />
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
