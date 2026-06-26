import type { KeyValuePair } from './types';

let _id = 0;
export const genId = () => String(++_id);

/**
 * Whether the app is running inside a Tauri webview (i.e. the packaged desktop
 * app). In that environment there is no Vite dev server, so the
 * `/__webdog_proxy` middleware is unavailable; HTTP requests are issued from
 * Rust via the Tauri http plugin instead (see App.tsx).
 */
export function isTauri(): boolean {
  return typeof window !== 'undefined' && '__TAURI_INTERNALS__' in window;
}

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

export function normalizeHttpUrl(input: string): string {
  const url = input.trim();
  if (!url) return '';
  if (/^[a-z][a-z\d+\-.]*:\/\//i.test(url)) return url;
  if (url.startsWith('//')) return `${window.location.protocol}${url}`;
  if (url.startsWith('/')) return url;
  return `http://${url}`;
}

export function normalizeWsUrl(input: string): string {
  const url = input.trim();
  if (!url) return '';
  if (/^wss?:\/\//i.test(url)) return url;
  if (/^https?:\/\//i.test(url)) return url.replace(/^http/i, 'ws');
  if (url.startsWith('//')) {
    const scheme = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    return `${scheme}${url}`;
  }
  return `ws://${url.replace(/^\/+/, '')}`;
}

export function getHttpFetchUrl(targetUrl: string): string {
  // In the Tauri desktop app there is no dev-server proxy to fall back on;
  // requests are issued through the Tauri http plugin, so the URL must be
  // passed through unchanged.
  if (isTauri()) return targetUrl;
  try {
    const target = new URL(targetUrl, window.location.href);
    if (['http:', 'https:'].includes(target.protocol) && target.origin !== window.location.origin) {
      return `/__webdog_proxy?url=${encodeURIComponent(target.toString())}`;
    }
  } catch {
    return targetUrl;
  }
  return targetUrl;
}

export function buildUrlWithParams(baseUrl: string, params: KeyValuePair[]): string {
  try {
    const enabled = params.filter(p => p.enabled && p.key.trim());
    if (!enabled.length) {
      return baseUrl;
    }
    let url = baseUrl;
    // strip existing params
    try {
      const u = new URL(baseUrl);
      u.search = '';
      url = u.toString();
    } catch {
      // Relative or incomplete URLs are left untouched.
    }
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

export function replaceEnvVars(text: string, envVars: { key: string; value: string; enabled: boolean }[]): string {
  let result = text;
  for (const v of envVars) {
    if (!v.enabled || !v.key.trim()) continue;
    const pattern = new RegExp(`\\{\\{\\s*${escapeRegex(v.key)}\\s*\\}\\}`, 'g');
    result = result.replace(pattern, v.value);
  }
  return result;
}

function escapeRegex(str: string): string {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

export function formatJson(input: string): string {
  try {
    const obj = JSON.parse(input);
    return JSON.stringify(obj, null, 2);
  } catch {
    return input;
  }
}

export function validateJson(input: string): string | null {
  try {
    JSON.parse(input);
    return null;
  } catch (e) {
    return e instanceof Error ? e.message : 'Invalid JSON';
  }
}

const HISTORY_STORAGE_KEY = 'webdog-history';
const ENV_STORAGE_KEY = 'webdog-env';

export function saveHistoryToStorage(history: import('./types').HistoryItem[]): void {
  try {
    window.localStorage.setItem(HISTORY_STORAGE_KEY, JSON.stringify(history.slice(0, 200)));
  } catch {
    // storage full or unavailable
  }
}

export function loadHistoryFromStorage(): import('./types').HistoryItem[] {
  try {
    const raw = window.localStorage.getItem(HISTORY_STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as import('./types').HistoryItem[];
    return Array.isArray(parsed) ? parsed.slice(0, 200) : [];
  } catch {
    return [];
  }
}

export function saveEnvToStorage(envVars: import('./types').EnvVariable[]): void {
  try {
    window.localStorage.setItem(ENV_STORAGE_KEY, JSON.stringify(envVars));
  } catch {
    // storage full or unavailable
  }
}

export function loadEnvFromStorage(): import('./types').EnvVariable[] {
  try {
    const raw = window.localStorage.getItem(ENV_STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as import('./types').EnvVariable[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}
