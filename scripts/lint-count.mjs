#!/usr/bin/env node
// Counts unique build warnings per rule ID for CRUDTasksWithAgent.slnx.
//
//   node scripts/lint-count.mjs           # print totals per rule
//   node scripts/lint-count.mjs --check   # also compare with docs/lint-baseline.md
//                                         # (<!-- lint-baseline-total: N -->), exit 1 on mismatch
//
// msbuild prints each warning once per project plus again in the summary, so lines are
// de-duplicated on "file(line,col): warning ID: message" (trailing "[project]" removed).
// Requires Node >= 22, no dependencies.
import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const check = process.argv.includes('--check');

const build = spawnSync(
  'dotnet',
  ['build', 'CRUDTasksWithAgent.slnx', '--no-incremental', '-nologo'],
  { cwd: root, encoding: 'utf8', maxBuffer: 256 * 1024 * 1024, shell: process.platform === 'win32' },
);
if (build.error) {
  console.error(`Failed to run dotnet: ${build.error.message}`);
  process.exit(2);
}
const output = `${build.stdout}\n${build.stderr}`;
if (build.status !== 0) {
  console.error(output);
  console.error('dotnet build failed; cannot count warnings.');
  process.exit(2);
}

const unique = new Set();
for (const raw of output.split(/\r?\n/)) {
  if (!/: warning [A-Za-z]+\d+/.test(raw)) continue;
  unique.add(raw.trim().replace(/\s+\[[^\]]*\]$/, ''));
}

const perRule = new Map();
for (const line of unique) {
  const id = /: warning ([A-Za-z]+\d+)/.exec(line)[1];
  perRule.set(id, (perRule.get(id) ?? 0) + 1);
}
const total = unique.size;

console.log('Rule      Count');
for (const [id, n] of [...perRule].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))) {
  console.log(`${id.padEnd(9)} ${n}`);
}
console.log(`TOTAL     ${total}`);

if (check) {
  const doc = readFileSync(resolve(root, 'docs/lint-baseline.md'), 'utf8');
  const m = /<!--\s*lint-baseline-total:\s*(\d+)\s*-->/.exec(doc);
  if (!m) {
    console.error('docs/lint-baseline.md has no "<!-- lint-baseline-total: N -->" line.');
    process.exit(1);
  }
  const expected = Number(m[1]);
  if (expected !== total) {
    console.error(`❌ Lint total ${total} differs from baseline ${expected}.`);
    console.error(total < expected
      ? 'Debt was fixed: update docs/lint-baseline.md if intentional.'
      : 'New warnings were introduced.');
    process.exit(1);
  }
  console.log(`✅ Lint total matches baseline (${expected}).`);
}
