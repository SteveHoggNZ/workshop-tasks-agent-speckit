#!/usr/bin/env node
// Environment doctor for the training lab. Node >= 22.20, no dependencies, macOS/Linux/Windows.
//
//   node scripts/doctor.mjs
//
// Prints ✅ ok / ⚠️ warning / ❌ failure per check with a fix-it hint. Exit code 1 only when a ❌ is present.
// Failures (❌): Node, .NET 10 SDK, git. Everything else is a warning, and harness CLIs
// (claude, copilot, cursor-agent, codex, code) are informational only: none is ever required.
import { spawnSync } from 'node:child_process';
import { existsSync, readdirSync, realpathSync, statSync } from 'node:fs';
import net from 'node:net';
import { homedir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const isWin = process.platform === 'win32';
const isMac = process.platform === 'darwin';
const results = [];

function add(status, name, detail, hint) {
  results.push({ status, name, detail, hint });
}

function tryRun(cmd, args) {
  const r = spawnSync(cmd, args, { encoding: 'utf8', timeout: 20000, shell: isWin });
  if (r.error || r.status !== 0) return null;
  return `${r.stdout ?? ''}${r.stderr ?? ''}`.trim();
}

function onPath(cmd) {
  const r = spawnSync(isWin ? 'where' : 'which', [cmd], { encoding: 'utf8' });
  return r.status === 0 && r.stdout.trim().length > 0;
}

function cmpVersion(a, b) {
  for (let i = 0; i < 3; i++) {
    const d = (a[i] ?? 0) - (b[i] ?? 0);
    if (d !== 0) return d;
  }
  return 0;
}

function checkNode() {
  const v = process.versions.node.split('.').map(Number);
  if (cmpVersion(v, [22, 20, 0]) >= 0) add('ok', 'Node.js', `v${process.versions.node}`);
  else add('fail', 'Node.js', `v${process.versions.node} (need >= 22.20)`, 'Install Node 22 LTS or newer: https://nodejs.org or `nvm install 22`');
}

function checkDotnet() {
  const out = tryRun('dotnet', ['--list-sdks']);
  if (out === null) {
    add('fail', '.NET SDK', 'dotnet not found', 'Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0');
    return;
  }
  const sdks = out.split(/\r?\n/).map((l) => l.split(' ')[0]).filter((v) => /^10\./.test(v));
  if (sdks.length) add('ok', '.NET SDK', `10.x found (${sdks.join(', ')})`);
  else add('fail', '.NET SDK', `no 10.x SDK (have: ${out.replace(/\s+/g, ' ')})`, 'Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0');
}

function checkGit() {
  const out = tryRun('git', ['--version']);
  if (out) add('ok', 'git', out);
  else add('fail', 'git', 'not found', 'Install git: https://git-scm.com/downloads');
}

function checkBrowser() {
  const candidates = [];
  const cmds = [];
  if (isMac) {
    for (const app of ['Google Chrome', 'Chromium', 'Microsoft Edge', 'Google Chrome for Testing']) {
      candidates.push(`/Applications/${app}.app`, join(homedir(), 'Applications', `${app}.app`));
    }
  } else if (isWin) {
    const bases = [process.env.PROGRAMFILES, process.env['PROGRAMFILES(X86)'], process.env.LOCALAPPDATA].filter(Boolean);
    for (const b of bases) {
      candidates.push(
        join(b, 'Google', 'Chrome', 'Application', 'chrome.exe'),
        join(b, 'Chromium', 'Application', 'chrome.exe'),
        join(b, 'Microsoft', 'Edge', 'Application', 'msedge.exe'),
      );
    }
  } else {
    cmds.push('google-chrome', 'google-chrome-stable', 'chromium', 'chromium-browser', 'microsoft-edge', 'microsoft-edge-stable');
  }
  const found = candidates.find((p) => existsSync(p)) ?? cmds.find(onPath);
  if (found) add('ok', 'Chrome/Chromium/Edge', found);
  else add('warn', 'Chrome/Chromium/Edge', 'not found', 'Install Google Chrome (https://www.google.com/chrome) or Chromium/Edge; needed for browser-driven labs');
}

function checkAgentBrowser() {
  if (onPath('agent-browser')) add('ok', 'agent-browser CLI', 'found on PATH');
  else add('warn', 'agent-browser CLI', 'not found (optional)', 'npm i -g agent-browser');
}

function portFree(port) {
  return new Promise((res) => {
    const srv = net.createServer();
    srv.once('error', () => res(false));
    srv.once('listening', () => srv.close(() => res(true)));
    srv.listen(port, '127.0.0.1');
  });
}

async function checkPorts() {
  for (const port of [5280, 7258]) {
    if (await portFree(port)) add('ok', `Port ${port}`, 'free');
    else add('warn', `Port ${port}`, 'in use', isWin ? `Find the process: netstat -ano | findstr :${port}` : `Find the process: lsof -i :${port}  (stop any running app instance)`);
  }
}

function checkHarnesses() {
  const found = ['claude', 'copilot', 'cursor-agent', 'codex', 'code'].filter(onPath);
  if (found.length) add('ok', 'Coding-agent harness CLIs', `detected: ${found.join(', ')} (informational)`);
  else add('warn', 'Coding-agent harness CLIs', 'no harness CLI detected — fine if you use an IDE', 'Optional: install any one of claude, copilot, cursor-agent, codex');
}

function skillNames(dir) {
  try {
    return readdirSync(dir).filter((n) => {
      if (n.startsWith('.')) return false;
      try { return statSync(join(dir, n)).isDirectory(); } catch { return false; }
    });
  } catch {
    return [];
  }
}

function checkSkillDuplicates() {
  const home = homedir();
  const dirs = [
    join(home, '.agents', 'skills'),
    join(home, '.claude', 'skills'),
    join(home, '.copilot', 'skills'),
    join(root, '.agents', 'skills'),
    join(root, '.claude', 'skills'),
    join(root, '.github', 'skills'),
  ];
  const seen = new Map();
  for (const d of dirs) {
    for (const n of skillNames(d)) {
      // Symlinks to the same real directory are one copy, not a duplicate.
      let real = join(d, n);
      try { real = realpathSync(real); } catch { /* keep path */ }
      const entries = seen.get(n) ?? new Map();
      if (!entries.has(real)) entries.set(real, d);
      seen.set(n, entries);
    }
  }
  const dupes = [...seen].filter(([, entries]) => entries.size > 1).map(([n, entries]) => [n, [...entries.values()]]);
  if (!dupes.length) {
    add('ok', 'Duplicate skills', 'none');
    return;
  }
  const detail = dupes.map(([n, where]) => `${n} (${where.join(', ')})`).join('; ');
  add('warn', 'Duplicate skills', detail, 'The same skill name in several skill dirs may load twice; keep one copy or remove/symlink the others');
}

checkNode();
checkDotnet();
checkGit();
checkBrowser();
checkAgentBrowser();
await checkPorts();
checkHarnesses();
checkSkillDuplicates();

const icon = { ok: '✅', warn: '⚠️ ', fail: '❌' };
for (const r of results) {
  console.log(`${icon[r.status]} ${r.name}: ${r.detail}`);
  if (r.status !== 'ok' && r.hint) console.log(`     ↳ ${r.hint}`);
}
const fails = results.filter((r) => r.status === 'fail').length;
const warns = results.filter((r) => r.status === 'warn').length;
console.log(`\n${fails} failure(s), ${warns} warning(s).`);
process.exit(fails ? 1 : 0);
