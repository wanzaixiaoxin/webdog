import type { KeyValuePair } from './types';

let _id = 0;
export const genId = () => String(++_id);

export function createKvPair(key = '', value = '', enabled = true): KeyValuePair {
  return { key, value, enabled, id: genId() };
}

export function parseUrlParams(url: string): KeyValuePair[] {
  try {
    const u = new URL(url);
    const pairs: KeyValuePair[] = [];
    u.searchParams.forEach((v, k) => {
      pairs.push({ key: k, value: v, enabled: true, id: genId() });
    });
    return pairs;
  } catch {
    return [];
  }
}

export function buildUrlWithParams(baseUrl: string, params: KeyValuePair[]): string {
  try {
    const enabled = params.filter(p => p.enabled && p.key.trim());
    if (!enabled.length) {
      // strip existing query params if none enabled
      try {
        const u = new URL(baseUrl);
        u.search = '';
        return u.toString();
      } catch {
        return baseUrl;
      }
    }
    let url = baseUrl;
    // strip existing params
    try {
      const u = new URL(baseUrl);
      u.search = '';
      url = u.toString();
    } catch {}
    const qs = enabled.map(p => `${encodeURIComponent(p.key)}=${encodeURIComponent(p.value)}`).join('&');
    return url + (url.includes('?') ? '&' : '?') + qs;
  } catch {
    return baseUrl;
  }
}

export function kvPairsToRecord(pairs: KeyValuePair[]): Record<string, string> {
  const record: Record<string, string> = {};
  for (const p of pairs) {
    if (p.enabled && p.key.trim()) {
      record[p.key] = p.value;
    }
  }
  return record;
}

export function formatSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}

export function formatTime(ms: number): string {
  if (ms < 1000) return ms + ' ms';
  return (ms / 1000).toFixed(2) + ' s';
}

export function tryParseJson(str: string): unknown | null {
  try {
    return JSON.parse(str);
  } catch {
    return null;
  }
}

export function jsonHighlight(json: string): string {
  return json.replace(
    /("(\\u[a-fA-F0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+-]?\d+)?)/g,
    (match) => {
      let cls = 'json-number';
      if (/^"/.test(match)) {
        cls = /:$/.test(match) ? 'json-key' : 'json-string';
      } else if (/true|false/.test(match)) {
        cls = 'json-boolean';
      } else if (/null/.test(match)) {
        cls = 'json-null';
      }
      return `<span class="${cls}">${match}</span>`;
    }
  );
}
