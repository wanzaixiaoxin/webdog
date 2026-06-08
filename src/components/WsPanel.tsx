import { useState, useRef, useEffect } from 'react';
import type { WsMessage } from '../types';
import { formatSize, jsonHighlight } from '../utils';

const IconSend = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 2L11 13"/><path d="M22 2L15 22L11 13L2 9L22 2Z"/>
  </svg>
);

const IconTrash = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
  </svg>
);

interface Props {
  messages: WsMessage[];
  onSend: (msg: string) => void;
  onClear: () => void;
  connected: boolean;
}

export default function WsPanel({ messages, onSend, onClear, connected }: Props) {
  const [input, setInput] = useState('');
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [messages]);

  const handleSend = () => {
    if (input.trim() && connected) {
      onSend(input.trim());
      setInput('');
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const formatMsg = (data: string) => {
    try {
      const obj = JSON.parse(data);
      return jsonHighlight(JSON.stringify(obj, null, 2));
    } catch {
      return data.replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }
  };

  return (
    <div className="ws-panel">
      <div className="ws-log" ref={logRef}>
        {messages.length === 0 && (
          <div className="ws-empty">No messages yet</div>
        )}
        {messages.map(msg => (
          <div key={msg.id} className={`ws-msg ws-msg-${msg.type}`}>
            <div className="ws-msg-header">
              <span className={`ws-msg-badge ws-badge-${msg.type}`}>
                {msg.type === 'sent' ? '↑ Sent' : msg.type === 'received' ? '↓ Received' : msg.type === 'error' ? '⚠ Error' : 'ℹ Info'}
              </span>
              <span className="ws-msg-time">
                {msg.timestamp.toLocaleTimeString()}
              </span>
              {msg.size != null && (
                <span className="ws-msg-size">{formatSize(msg.size)}</span>
              )}
            </div>
            <pre
              className="ws-msg-body"
              dangerouslySetInnerHTML={{ __html: formatMsg(msg.data) }}
            />
          </div>
        ))}
      </div>
      <div className="ws-input-area">
        <textarea
          className="ws-input"
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={connected ? "Type a message and press Enter to send..." : "Connect first..."}
          disabled={!connected}
          rows={3}
        />
        <div className="ws-input-actions">
          <button
            className="btn btn-primary"
            onClick={handleSend}
            disabled={!connected || !input.trim()}
          >
            <IconSend /> Send
          </button>
          <button className="btn btn-secondary" onClick={onClear}>
            <IconTrash /> Clear
          </button>
        </div>
      </div>
    </div>
  );
}
