import { useState } from 'react';

interface Props {
  data: unknown;
  level?: number;
}

function isObject(v: unknown): v is Record<string, unknown> {
  return v !== null && typeof v === 'object' && !Array.isArray(v);
}

function isArray(v: unknown): v is unknown[] {
  return Array.isArray(v);
}

function getTypeLabel(v: unknown): string {
  if (v === null) return 'null';
  if (Array.isArray(v)) return `array[${v.length}]`;
  return typeof v;
}

function getPreview(v: unknown): string {
  if (v === null) return 'null';
  if (typeof v === 'string') {
    const s = v.length > 60 ? v.slice(0, 60) + '...' : v;
    return `"${s}"`;
  }
  if (typeof v === 'number' || typeof v === 'boolean') return String(v);
  if (Array.isArray(v)) return `Array(${v.length})`;
  return `{${Object.keys(v as object).length}}`;
}

export default function JsonTree({ data, level = 0 }: Props) {
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const toggle = (path: string) => {
    setExpanded(prev => {
      const next = new Set(prev);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });
  };

  const renderNode = (key: string, value: unknown, path: string, idx: number): JSX.Element => {
    const expandable = isObject(value) || isArray(value);
    const isExpanded = expanded.has(path);
    const indent = level * 16;

    if (!expandable) {
      return (
        <div key={path} className="json-tree-row" style={{ paddingLeft: indent + 20 }}>
          <span className="json-tree-key">{key}:</span>
          <span className={`json-tree-value json-tree-${typeof value}`}>
            {typeof value === 'string' ? `"${value}"` : String(value)}
          </span>
        </div>
      );
    }

    const entries = isArray(value)
      ? value.map((v, i) => [String(i), v] as [string, unknown])
      : Object.entries(value);

    return (
      <div key={path}>
        <div
          className="json-tree-row json-tree-branch"
          style={{ paddingLeft: indent + 4 }}
          onClick={() => toggle(path)}
        >
          <span className={`json-tree-chevron ${isExpanded ? 'expanded' : ''}`}>▶</span>
          <span className="json-tree-key">{key}:</span>
          <span className="json-tree-preview">
            {isExpanded ? '' : getPreview(value)}
          </span>
          <span className="json-tree-type">{getTypeLabel(value)}</span>
        </div>
        {isExpanded && (
          <div className="json-tree-children">
            {entries.map(([k, v], i) => (
              <JsonTree key={`${path}.${k}`} data={v} level={level + 1} />
            ))}
          </div>
        )}
      </div>
    );
  };

  if (isObject(data)) {
    return (
      <div className="json-tree">
        {Object.entries(data).map(([key, value], idx) => renderNode(key, value, key, idx))}
      </div>
    );
  }

  if (isArray(data)) {
    return (
      <div className="json-tree">
        {data.map((value, idx) => renderNode(String(idx), value, String(idx), idx))}
      </div>
    );
  }

  return (
    <div className="json-tree">
      <div className="json-tree-row">
        <span className={`json-tree-value json-tree-${typeof data}`}>
          {typeof data === 'string' ? `"${data}"` : String(data)}
        </span>
      </div>
    </div>
  );
}
