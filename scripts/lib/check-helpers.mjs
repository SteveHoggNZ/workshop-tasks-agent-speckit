// Helper API for labs/<NN>-*/check.mjs validators (plain Node >= 22, no dependencies, no model/harness calls).
//
// A lab validator is labs/<NN>-<name>/check.mjs exporting:
//
//   import { fileExists, readFrontmatter, run, assert } from '../../scripts/lib/check-helpers.mjs';
//   export async function check(ctx) { ... }
//
// ctx = { root, lab, labDir, h, step }
//   root   absolute repo root
//   lab    two-digit lab number string, e.g. "02"
//   labDir absolute path of the lab folder
//   h      this helper module (so checks can use ctx.h.run(...))
//   step   async (name, fn) => records a ✅/❌ line; a throwing fn marks it failed and the check continues
//
// A check passes when it does not throw and no step failed. Helpers:
//   fileExists(path)             -> boolean (file or directory), sync
//   readFrontmatter(path)        -> { data, body } for a file starting with `---` YAML-ish frontmatter.
//                                   Supports `key: value`, quoted strings, booleans, numbers, inline lists `[a, b]`,
//                                   and `- item` block lists. Returns data = {} when no frontmatter exists.
//   run(cmd, args, opts)         -> { code, stdout, stderr, ok }; never throws on non-zero exit. opts: cwd, env, timeoutMs.
//   assert(cond, msg)            -> throws Error(msg) when cond is falsy
//   repoRoot                     -> absolute repo root
import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');

export function fileExists(path) {
  return existsSync(path);
}

export function assert(cond, msg) {
  if (!cond) throw new Error(msg ?? 'Assertion failed');
}

export function run(cmd, args = [], opts = {}) {
  const r = spawnSync(cmd, args, {
    cwd: opts.cwd ?? repoRoot,
    env: opts.env ? { ...process.env, ...opts.env } : process.env,
    encoding: 'utf8',
    timeout: opts.timeoutMs,
    maxBuffer: 256 * 1024 * 1024,
    shell: process.platform === 'win32' && cmd !== process.execPath,
  });
  const code = r.status ?? (r.error ? 127 : 1);
  return { code, ok: code === 0, stdout: r.stdout ?? '', stderr: r.stderr ?? (r.error?.message ?? '') };
}

function parseScalar(v) {
  v = v.trim();
  if (v === '') return '';
  if ((v.startsWith('"') && v.endsWith('"')) || (v.startsWith("'") && v.endsWith("'"))) return v.slice(1, -1);
  if (v === 'true') return true;
  if (v === 'false') return false;
  if (v === 'null' || v === '~') return null;
  if (/^-?\d+(\.\d+)?$/.test(v)) return Number(v);
  if (v.startsWith('[') && v.endsWith(']')) {
    const inner = v.slice(1, -1).trim();
    return inner === '' ? [] : inner.split(',').map(parseScalar);
  }
  return v;
}

export function readFrontmatter(path) {
  const text = readFileSync(path, 'utf8').replace(/^\uFEFF/, '');
  const m = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?([\s\S]*)$/.exec(text);
  if (!m) return { data: {}, body: text };
  const data = {};
  let key = null;
  for (const line of m[1].split(/\r?\n/)) {
    if (/^\s*(#.*)?$/.test(line)) continue;
    const item = /^\s+-\s+(.*)$/.exec(line);
    if (item && key) {
      if (!Array.isArray(data[key])) data[key] = [];
      data[key].push(parseScalar(item[1]));
      continue;
    }
    const kv = /^([A-Za-z0-9_.-]+):\s*(.*)$/.exec(line);
    if (kv) {
      key = kv[1];
      // Folded/literal block scalars (`>` / `|`) are kept as the joined continuation lines.
      data[key] = kv[2] === '>' || kv[2] === '|' ? '' : parseScalar(kv[2]);
    } else if (key && typeof data[key] === 'string') {
      data[key] = `${data[key]} ${line.trim()}`.trim();
    }
  }
  return { data, body: m[2] };
}
