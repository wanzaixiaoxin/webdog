import { useState } from 'react';
import type { KeyValuePair, BodyType } from '../types';
import KeyValueEditor from './KeyValueEditor';

interface Props {
  params: KeyValuePair[];
  headers: KeyValuePair[];
  bodyType: BodyType;
  body: string;
  onParamsChange: (p: KeyValuePair[]) => void;
  onHeadersChange: (h: KeyValuePair[]) => void;
  onBodyTypeChange: (b: BodyType) => void;
  onBodyChange: (b: string) => void;
  hasBody: boolean;
}

const BODY_TYPES: { value: BodyType; label: string }[] = [
  { value: 'none', label: 'none' },
  { value: 'json', label: 'JSON' },
  { value: 'text', label: 'Text' },
  { value: 'formdata', label: 'Form Data' },
  { value: 'urlencoded', label: 'URL Encoded' },
  { value: 'raw', label: 'Raw' },
];

export default function RequestPanel({
  params, headers, bodyType, body,
  onParamsChange, onHeadersChange,
  onBodyTypeChange, onBodyChange, hasBody,
}: Props) {
  const [activeTab, setActiveTab] = useState<'params' | 'headers' | 'body'>('params');

  const activeParamsCount = params.filter(p => p.enabled && p.key.trim()).length;
  const activeHeadersCount = headers.filter(p => p.enabled && p.key.trim()).length;

  return (
    <div className="request-panel">
      <div className="request-tabs">
        <button
          className={`tab ${activeTab === 'params' ? 'active' : ''}`}
          onClick={() => setActiveTab('params')}
        >
          Params {activeParamsCount > 0 && <span className="tab-badge">{activeParamsCount}</span>}
        </button>
        <button
          className={`tab ${activeTab === 'headers' ? 'active' : ''}`}
          onClick={() => setActiveTab('headers')}
        >
          Headers {activeHeadersCount > 0 && <span className="tab-badge">{activeHeadersCount}</span>}
        </button>
        <button
          className={`tab ${activeTab === 'body' ? 'active' : ''}`}
          onClick={() => setActiveTab('body')}
        >
          Body
        </button>
      </div>

      <div className="tab-content">
        {activeTab === 'params' && (
          <KeyValueEditor
            pairs={params}
            onChange={onParamsChange}
            keyPlaceholder="Parameter name"
            valuePlaceholder="Value"
          />
        )}
        {activeTab === 'headers' && (
          <KeyValueEditor
            pairs={headers}
            onChange={onHeadersChange}
            keyPlaceholder="Header name"
            valuePlaceholder="Value"
          />
        )}
        {activeTab === 'body' && (
          <div className="body-panel">
            <div className="body-type-selector">
              {BODY_TYPES.map(bt => (
                <button
                  key={bt.value}
                  className={`body-type-btn ${bodyType === bt.value ? 'active' : ''}`}
                  onClick={() => onBodyTypeChange(bt.value)}
                >
                  {bt.label}
                </button>
              ))}
            </div>
            {hasBody && bodyType !== 'none' ? (
              <textarea
                className="body-editor"
                value={body}
                onChange={e => onBodyChange(e.target.value)}
                placeholder={bodyType === 'json' ? '{\n  "key": "value"\n}' : 'Enter body content...'}
                spellCheck={false}
              />
            ) : (
              <div className="body-empty">
                {hasBody ? 'No request body will be sent.' : 'This method does not send a request body.'}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
