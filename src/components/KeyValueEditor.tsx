import { useCallback } from 'react';
import type { KeyValuePair } from '../types';
import { createKvPair } from '../utils';

const IconTrash = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
    <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
  </svg>
);

interface Props {
  pairs: KeyValuePair[];
  onChange: (pairs: KeyValuePair[]) => void;
  keyPlaceholder?: string;
  valuePlaceholder?: string;
}

export default function KeyValueEditor({ pairs, onChange, keyPlaceholder = 'Key', valuePlaceholder = 'Value' }: Props) {
  const update = useCallback((id: string, field: keyof KeyValuePair, value: string | boolean) => {
    onChange(pairs.map(p => p.id === id ? { ...p, [field]: value } : p));
  }, [pairs, onChange]);

  const add = useCallback(() => {
    onChange([...pairs, createKvPair()]);
  }, [pairs, onChange]);

  const remove = useCallback((id: string) => {
    onChange(pairs.filter(p => p.id !== id));
  }, [pairs, onChange]);

  return (
    <div className="kv-editor">
      <div className="kv-header">
        <span></span>
        <span>{keyPlaceholder}</span>
        <span>{valuePlaceholder}</span>
        <span></span>
      </div>
      {pairs.map(p => (
        <div key={p.id} className="kv-row">
          <input
            type="checkbox"
            checked={p.enabled}
            onChange={e => update(p.id, 'enabled', e.target.checked)}
            className="kv-checkbox"
          />
          <input
            type="text"
            value={p.key}
            onChange={e => update(p.id, 'key', e.target.value)}
            placeholder={keyPlaceholder}
            className="kv-input"
          />
          <input
            type="text"
            value={p.value}
            onChange={e => update(p.id, 'value', e.target.value)}
            placeholder={valuePlaceholder}
            className="kv-input"
          />
          <button className="kv-delete" onClick={() => remove(p.id)} title="Delete" aria-label="Delete row">
            <IconTrash />
          </button>
        </div>
      ))}
      <button className="kv-add" onClick={add}>+ Add</button>
    </div>
  );
}
