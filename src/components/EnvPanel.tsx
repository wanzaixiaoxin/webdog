import { useCallback } from 'react';
import type { EnvVariable } from '../types';
import { createKvPair } from '../utils';

const IconTrash = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
    <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
  </svg>
);

interface Props {
  envVars: EnvVariable[];
  onChange: (vars: EnvVariable[]) => void;
}

export default function EnvPanel({ envVars, onChange }: Props) {
  const update = useCallback((id: string, field: keyof EnvVariable, value: string | boolean) => {
    onChange(envVars.map(v => v.id === id ? { ...v, [field]: value } : v));
  }, [envVars, onChange]);

  const add = useCallback(() => {
    onChange([...envVars, { ...createKvPair(), enabled: true }]);
  }, [envVars, onChange]);

  const remove = useCallback((id: string) => {
    onChange(envVars.filter(v => v.id !== id));
  }, [envVars, onChange]);

  return (
    <div className="env-panel">
      <div className="env-panel-header">
        <h3>Environment Variables</h3>
        <span className="env-panel-hint">Use {"{{variable}}"} in URL, headers, params, body</span>
      </div>
      <div className="env-panel-list">
        {envVars.length === 0 && (
          <div className="env-panel-empty">No variables defined</div>
        )}
        {envVars.map(v => (
          <div key={v.id} className="env-panel-row">
            <input
              type="checkbox"
              checked={v.enabled}
              onChange={e => update(v.id, 'enabled', e.target.checked)}
              className="env-panel-checkbox"
              title="Enable"
            />
            <input
              type="text"
              value={v.key}
              onChange={e => update(v.id, 'key', e.target.value)}
              placeholder="VAR_NAME"
              className="env-panel-input"
            />
            <input
              type="text"
              value={v.value}
              onChange={e => update(v.id, 'value', e.target.value)}
              placeholder="value"
              className="env-panel-input"
            />
            <button className="env-panel-delete" onClick={() => remove(v.id)} title="Delete">
              <IconTrash />
            </button>
          </div>
        ))}
      </div>
      <button className="env-panel-add" onClick={add}>+ Add Variable</button>
    </div>
  );
}
