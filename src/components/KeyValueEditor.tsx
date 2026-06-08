import { useState, useCallback } from 'react';
import type { KeyValuePair } from '../types';
import { createKvPair } from '../utils';

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
          <button className="kv-delete" onClick={() => remove(p.id)} title="Delete">×</button>
        </div>
      ))}
      <button className="kv-add" onClick={add}>+ Add</button>
    </div>
  );
}
