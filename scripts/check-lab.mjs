#!/usr/bin/env node
// Harness-free lab validator. Never calls a model or any coding-agent harness.
//
//   node scripts/check-lab.mjs 00
//
// Looks for labs/<NN>-*/check.mjs exporting `async function check(ctx)` (see scripts/lib/check-helpers.mjs
// for the ctx and helper API) and runs it. For lab 00, when no check.mjs exists, a built-in check runs:
// `dotnet build`, `dotnet test`, and `scripts/doctor.mjs` must have no ❌.
// Prints a ✅/❌ list; exits 1 if any step failed (2 on usage errors).
import { readdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import * as h from './lib/check-helpers.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const arg = process.argv[2];
if (!arg || !/^\d{1,2}$/.test(arg)) {
  console.error('Usage: node scripts/check-lab.mjs <NN>   (e.g. 00)');
  process.exit(2);
}
const lab = arg.padStart(2, '0');

const steps = [];
async function step(name, fn) {
  try {
    await fn();
    steps.push({ name, ok: true });
    console.log(`✅ ${name}`);
  } catch (e) {
    steps.push({ name, ok: false });
    console.log(`❌ ${name}`);
    console.log(String(e?.message ?? e).split('\n').map((l) => `     ${l}`).join('\n'));
  }
}

function tail(text, n = 15) {
  return text.trim().split(/\r?\n/).slice(-n).join('\n');
}

async function builtinLab00() {
  await step('dotnet build succeeds', () => {
    const r = h.run('dotnet', ['build', 'CRUDTasksWithAgent.slnx', '-nologo']);
    h.assert(r.ok, `dotnet build exited ${r.code}\n${tail(r.stdout + r.stderr)}`);
  });
  await step('dotnet test passes', () => {
    const r = h.run('dotnet', ['test', 'CRUDTasksWithAgent.slnx', '-nologo']);
    h.assert(r.ok, `dotnet test exited ${r.code}\n${tail(r.stdout + r.stderr)}`);
  });
  await step('doctor reports no ❌', () => {
    const r = h.run(process.execPath, [join(root, 'scripts', 'doctor.mjs')]);
    h.assert(r.ok, `doctor exited ${r.code}\n${r.stdout.split('\n').filter((l) => l.includes('❌') || l.includes('↳')).join('\n')}`);
  });
}

const labsDir = join(root, 'labs');
let labDir = null;
try {
  const match = readdirSync(labsDir).find((d) => d.startsWith(`${lab}-`));
  if (match) labDir = join(labsDir, match);
} catch { /* no labs directory yet */ }

const checkFile = labDir ? join(labDir, 'check.mjs') : null;
console.log(`Checking lab ${lab}${labDir ? ` (${labDir.slice(root.length + 1)})` : ''}\n`);

if (checkFile && h.fileExists(checkFile)) {
  let mod;
  try {
    mod = await import(pathToFileURL(checkFile).href);
  } catch (e) {
    console.log(`❌ load check.mjs\n     ${e.message}`);
    process.exit(1);
  }
  if (typeof mod.check !== 'function') {
    console.log('❌ check.mjs does not export `async function check(ctx)`');
    process.exit(1);
  }
  try {
    await mod.check({ root, lab, labDir, h, step });
  } catch (e) {
    steps.push({ name: 'check()', ok: false });
    console.log(`❌ check() threw\n${String(e?.message ?? e).split('\n').map((l) => `     ${l}`).join('\n')}`);
  }
  if (steps.length === 0) {
    steps.push({ name: 'check() ran', ok: true });
    console.log('✅ check() completed');
  }
} else if (lab === '00') {
  await builtinLab00();
} else {
  console.log(`❌ No labs/${lab}-*/check.mjs found`);
  process.exit(1);
}

const failed = steps.filter((s) => !s.ok).length;
console.log(`\n${steps.length - failed}/${steps.length} checks passed.`);
process.exit(failed ? 1 : 0);
