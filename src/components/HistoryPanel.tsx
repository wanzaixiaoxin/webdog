import { useState } from 'react';
import type { HistoryItem } from '../types';

interface Props {
  history: HistoryItem[];
  onSelect: (item: HistoryItem) => void;
  onClear: () => void;
}

export default function HistoryPanel({ history, onSelect, onClear }: Props) {
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
      WS: '#8b5cf6',
      WSS: '#8b5cf6',
    };
    return colors[method] || '#94a3b8';
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
          <div className="history-empty">No history yet</div>
        )}
        {filtered.map(item => (
          <div
            key={item.id}
            className="history-item"
            onClick={() => onSelect(item)}
            title={item.url}
          >
            <span
              className="history-method"
              style={{ backgroundColor: methodColor(item.method) }}
            >
              {item.method}
            </span>
            <span className="history-url">
              {item.url.length > 35 ? item.url.substring(0, 35) + '...' : item.url}
            </span>
            {item.status && (
              <span className={`history-status ${item.status < 400 ? 'status-success' : 'status-error'}`}>
                {item.status}
              </span>
            )}
            <span className="history-time-label">
              {item.timestamp.toLocaleTimeString()}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
