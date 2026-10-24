// Builds the frontend for MSBuild, at most one build at a time: HP BIM Toolkit and HPRebar (Revit 2026) both embed this
// folder's build and a solution build compiles them in parallel. The first caller takes the lock and builds; a caller
// that waited finds the output newer than every source and returns at once. Installs node_modules first when the lock
// file changed. The lock lives beside node_modules, not in it (npm ci deletes node_modules), and records its owner's
// pid: a lock whose owner is gone (a failed or cancelled build) is taken over at once.
// Usage: node tools/build-frontend-once.mjs
import { spawnSync } from 'node:child_process';
import { mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const lock = path.join(root, '.build-lock');
const owner = path.join(lock, 'pid');
const LOCK_TIMEOUT_MS = 20 * 60 * 1000;
const STALE_LOCK_MS = 15 * 60 * 1000;

const mtime = (file) => {
  try {
    return statSync(file).mtimeMs;
  } catch {
    return 0;
  }
};

/** Newest modification time under a folder (or of a file). */
const newest = (entry) => {
  const stat = statSync(entry, { throwIfNoEntry: false });
  if (!stat) return 0;
  if (!stat.isDirectory()) return stat.mtimeMs;
  return readdirSync(entry).reduce((latest, name) => Math.max(latest, newest(path.join(entry, name))), stat.mtimeMs);
};

const sources = ['src', 'index.html', 'kata.html', 'package.json', 'package-lock.json', 'vite.config.ts', 'tsconfig.json', 'tsconfig.app.json', 'tsconfig.node.json'];
const isBuilt = () => mtime(path.join(root, 'dist', 'kata.html')) > Math.max(...sources.map((s) => newest(path.join(root, s))));
const isInstalled = () => mtime(path.join(root, 'node_modules', '.package-lock.json')) >= mtime(path.join(root, 'package-lock.json'));

/** Runs npm; a failure is thrown (never process.exit here: the lock must be released by the finally below). */
const npm = (...args) => {
  const result = spawnSync('npm', args, { cwd: root, stdio: 'inherit', shell: process.platform === 'win32' });
  if (result.status !== 0) throw Object.assign(new Error(`npm ${args.join(' ')} failed`), { exitCode: result.status ?? 1 });
};

const isAlive = (pid) => {
  try {
    process.kill(pid, 0);
    return true;
  } catch (error) {
    return error.code === 'EPERM';
  }
};

/** The lock's owner died, or it has been held longer than any build takes. */
const isStale = () => {
  if (Date.now() - mtime(lock) > STALE_LOCK_MS) return true;
  try {
    return !isAlive(Number(readFileSync(owner, 'utf8')));
  } catch {
    return Date.now() - mtime(lock) > 5000; // created but its pid not written yet: give the owner a moment
  }
};

async function acquire() {
  const deadline = Date.now() + LOCK_TIMEOUT_MS;
  for (;;) {
    try {
      mkdirSync(lock);
      writeFileSync(owner, String(process.pid));
      return;
    } catch (error) {
      if (error.code !== 'EEXIST') throw error;
      if (isStale()) rmSync(lock, { recursive: true, force: true });
      else if (Date.now() > deadline) throw new Error(`frontend build lock still held after ${LOCK_TIMEOUT_MS / 60000} min: ${lock}`);
      await new Promise((resolve) => setTimeout(resolve, 500));
    }
  }
}

await acquire();
try {
  if (!isInstalled()) npm('ci', '--no-audit', '--no-fund');
  if (!isBuilt()) npm('run', 'build');
  else console.log('frontend build is up to date');
} catch (error) {
  console.error(error.message);
  process.exitCode = error.exitCode ?? 1;
} finally {
  rmSync(lock, { recursive: true, force: true });
}
