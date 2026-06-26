import { useState, useRef, useCallback, useEffect } from 'react';
import { formatJson, validateJson, jsonHighlight } from '../utils';

interface Props {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

export default function JsonEditor({ value, onChange, placeholder }: Props) {
  const [error, setError] = useState<string | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const highlightRef = useRef<HTMLPreElement>(null);

  const lines = value.split('\n');
  const lineCount = Math.max(1, lines.length);

  useEffect(() => {
    setError(validateJson(value));
  }, [value]);

  const handleChange = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onChange(e.target.value);
  }, [onChange]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Tab') {
      e.preventDefault();
      const target = e.target as HTMLTextAreaElement;
      const start = target.selectionStart;
      const end = target.selectionEnd;
      const newValue = value.substring(0, start) + '  ' + value.substring(end);
      onChange(newValue);
      requestAnimationFrame(() => {
        target.selectionStart = target.selectionEnd = start + 2;
      });
    }
  }, [value, onChange]);

  const handleFormat = useCallback(() => {
    onChange(formatJson(value));
  }, [value, onChange]);

  // Sync scroll between textarea and highlight overlay
  const handleScroll = useCallback(() => {
    if (textareaRef.current && highlightRef.current) {
      highlightRef.current.scrollTop = textareaRef.current.scrollTop;
      highlightRef.current.scrollLeft = textareaRef.current.scrollLeft;
    }
  }, []);

  const highlighted = jsonHighlight(value || ' ');

  return (
    <div className="json-editor-wrapper">
      <div className="json-editor-toolbar">
        <button className="btn btn-sm btn-secondary" onClick={handleFormat} title="Format JSON">
          Format
        </button>
        {error ? (
          <span className="json-editor-error-badge">{error}</span>
        ) : (
          <span className="json-editor-valid-badge">Valid JSON</span>
        )}
      </div>
      <div className="json-editor-container">
        <div className="json-editor-line-numbers">
          {Array.from({ length: lineCount }, (_, i) => (
            <div key={i} className="json-line-number">{i + 1}</div>
          ))}
        </div>
        <div className="json-editor-area">
          <pre
            ref={highlightRef}
            className="json-editor-highlight"
            aria-hidden="true"
            dangerouslySetInnerHTML={{ __html: highlighted + '\n' }}
          />
          <textarea
            ref={textareaRef}
            className="json-editor-textarea"
            value={value}
            onChange={handleChange}
            onKeyDown={handleKeyDown}
            onScroll={handleScroll}
            placeholder={placeholder}
            spellCheck={false}
            autoCapitalize="off"
            autoComplete="off"
            autoCorrect="off"
          />
        </div>
      </div>
    </div>
  );
}
