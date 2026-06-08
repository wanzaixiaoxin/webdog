import type { IncomingMessage, ServerResponse } from 'node:http'
import { defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'

const PROXY_PREFIX = '/__webdog_proxy'
const REQUEST_HEADER_BLOCKLIST = new Set([
  'connection',
  'content-length',
  'host',
  'origin',
  'referer',
  'sec-fetch-dest',
  'sec-fetch-mode',
  'sec-fetch-site',
  'sec-fetch-user',
  'transfer-encoding',
])
const RESPONSE_HEADER_BLOCKLIST = new Set([
  'connection',
  'content-encoding',
  'content-length',
  'keep-alive',
  'transfer-encoding',
  'upgrade',
])

function readBody(req: IncomingMessage): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    const chunks: Buffer[] = []
    req.on('data', chunk => chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk)))
    req.on('end', () => resolve(Buffer.concat(chunks)))
    req.on('error', reject)
  })
}

function copyRequestHeaders(req: IncomingMessage) {
  const headers = new Headers()
  Object.entries(req.headers).forEach(([name, value]) => {
    const lowerName = name.toLowerCase()
    if (REQUEST_HEADER_BLOCKLIST.has(lowerName)) return
    if (value == null) return
    if (Array.isArray(value)) {
      value.forEach(item => headers.append(name, item))
      return
    }
    headers.set(name, value)
  })
  return headers
}

function sendJson(res: ServerResponse, status: number, payload: unknown) {
  res.statusCode = status
  res.setHeader('Content-Type', 'application/json')
  res.end(JSON.stringify(payload))
}

function webdogProxyPlugin(): Plugin {
  return {
    name: 'webdog-api-proxy',
    configureServer(server) {
      server.middlewares.use(PROXY_PREFIX, async (req, res) => {
        try {
          const requestUrl = new URL(req.url || '/', 'http://webdog.local')
          const target = requestUrl.searchParams.get('url')
          if (!target) {
            sendJson(res, 400, { error: 'Missing target URL' })
            return
          }

          const targetUrl = new URL(target)
          if (!['http:', 'https:'].includes(targetUrl.protocol)) {
            sendJson(res, 400, { error: 'Only HTTP and HTTPS targets are supported' })
            return
          }

          const method = req.method || 'GET'
          const body = ['GET', 'HEAD'].includes(method) ? undefined : await readBody(req)
          const upstream = await fetch(targetUrl, {
            body,
            headers: copyRequestHeaders(req),
            method,
            redirect: 'manual',
          })

          res.statusCode = upstream.status
          res.statusMessage = upstream.statusText
          res.setHeader('X-WebDog-Proxy-Target', targetUrl.toString())
          upstream.headers.forEach((value, name) => {
            if (!RESPONSE_HEADER_BLOCKLIST.has(name.toLowerCase())) {
              res.setHeader(name, value)
            }
          })
          res.end(Buffer.from(await upstream.arrayBuffer()))
        } catch (error) {
          sendJson(res, 502, {
            error: error instanceof Error ? error.message : String(error),
          })
        }
      })
    },
  }
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), webdogProxyPlugin()],
})
