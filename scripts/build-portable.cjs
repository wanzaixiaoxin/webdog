#!/usr/bin/env node
/**
 * 把 webdog 打包成便携版 zip。
 *
 * 产物结构（解压即用，无需安装、不依赖系统 WebView2）：
 *   WebDog-portable/
 *   ├── webdog.exe
 *   └── runtime/        (WebView2 Fixed Runtime，~180MB)
 *
 * 前置条件：
 *   1. 已运行 `npx tauri build`，src-tauri/target/release/webdog.exe 存在
 *   2. 已把 WebView2 Fixed Runtime 解压到 src-tauri/webview2/runtime/
 *
 * 用法：node scripts/build-portable.js
 */
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const ROOT = path.resolve(__dirname, '..');
const EXE = path.join(ROOT, 'src-tauri', 'target', 'release', 'webdog.exe');
const RUNTIME_SRC = path.join(ROOT, 'src-tauri', 'webview2', 'runtime');
const STAGE = path.join(ROOT, 'dist-portable');
const STAGE_RUNTIME = path.join(STAGE, 'runtime');

function log(msg) { console.log(`[build-portable] ${msg}`); }
function die(msg) { console.error(`[build-portable] 错误: ${msg}`); process.exit(1); }

// 1. 前置检查
log('检查前置条件...');
if (!fs.existsSync(EXE)) {
  die(`找不到 ${EXE}。请先运行 \`npx tauri build\`。`);
}
if (!fs.existsSync(path.join(RUNTIME_SRC, 'msedgewebview2.exe'))) {
  die(`找不到运行时。请把 WebView2 Fixed Runtime 解压到 ${RUNTIME_SRC}（需含 msedgewebview2.exe）。`);
}

// 2. 清理旧的暂存目录
log('清理旧的 dist-portable ...');
fs.rmSync(STAGE, { recursive: true, force: true });

// 3. 组装目录
const PKG_DIR = 'WebDog-portable';
const STAGE_PKG = path.join(STAGE, PKG_DIR);
fs.mkdirSync(STAGE_PKG, { recursive: true });

log('复制 webdog.exe ...');
fs.copyFileSync(EXE, path.join(STAGE_PKG, 'webdog.exe'));

log('复制 runtime/（约 180MB，请稍候）...');
execSync(`xcopy /E /I /Q /Y "${RUNTIME_SRC}" "${path.join(STAGE_PKG, 'runtime')}"`, { stdio: 'ignore' });

// 4. 打包成 zip
// 优先用 PowerShell 的 Compress-Archive（Windows 自带，无需装额外工具）
const zipPath = path.join(STAGE, 'WebDog-portable.zip');
fs.rmSync(zipPath, { force: true });

log('压缩为 zip（PowerShell Compress-Archive，较慢，请耐心等待）...');
// 注意：必须先 cd 进 STAGE，让 zip 内层是 WebDog-portable/ 而不是绝对路径
execSync(
  `powershell -NoProfile -Command "Compress-Archive -Path '${PKG_DIR}' -DestinationPath '${path.basename(zipPath)}' -CompressionLevel Optimal"`,
  { cwd: STAGE, stdio: 'inherit' }
);

// 5. 统计结果
const zipSize = fs.statSync(zipPath).size;
const sizeMb = (zipSize / 1024 / 1024).toFixed(1);
log(`完成！`);
log(`  zip: ${zipPath} (${sizeMb} MB)`);
log(`  解压目录: ${STAGE_PKG}`);
log(`  使用方法: 解压后双击 WebDog-portable/webdog.exe 即可（自带运行时，不联网）。`);
