// Slices JavaScript declarations out of the reference HTML tool (the "oracle") so that the
// data and the algorithm are never copied by hand. Every extractor and golden generator goes
// through this module; the oracle path can be overridden with HPGEO_ORACLE.
import { readFileSync, existsSync } from 'node:fs';

export const DEFAULT_ORACLE_PATH =
  'X:\\02-TOOL\\04-Other\\ToolVN2000ConvertGoogleEarth (TheoDiaDanhHanhChinhMoi).html';

export function oraclePath() {
  const p = process.env.HPGEO_ORACLE || DEFAULT_ORACLE_PATH;
  if (!existsSync(p)) {
    throw new Error(`Oracle HTML not found: ${p} (set HPGEO_ORACLE to override)`);
  }
  return p;
}

export function readOracle() {
  return readFileSync(oraclePath(), 'utf8');
}

// Returns the index just past the bracket that closes the one opening at `openIndex`.
// Skips string literals, template literals and comments so a brace inside a label never
// terminates a block early.
function matchBracket(src, openIndex) {
  const open = src[openIndex];
  const close = open === '{' ? '}' : open === '[' ? ']' : ')';
  let depth = 0;
  for (let i = openIndex; i < src.length; i++) {
    const c = src[i];
    if (c === '"' || c === "'" || c === '`') {
      const q = c;
      i++;
      while (i < src.length && src[i] !== q) {
        if (src[i] === '\\') i++;
        i++;
      }
      continue;
    }
    if (c === '/' && src[i + 1] === '/') {
      while (i < src.length && src[i] !== '\n') i++;
      continue;
    }
    if (c === '/' && src[i + 1] === '*') {
      i = src.indexOf('*/', i + 2) + 1;
      continue;
    }
    if (c === open) depth++;
    else if (c === close) {
      depth--;
      if (depth === 0) return i + 1;
    }
  }
  throw new Error(`Unbalanced ${open} starting at ${openIndex}`);
}

// `const NAME = {...};` or `const NAME = [...];` — returns the full statement text.
export function sliceConst(src, name) {
  const m = new RegExp(`(?:const|let|var)\\s+${name}\\s*=\\s*`, 'g').exec(src);
  if (!m) throw new Error(`const ${name} not found in oracle`);
  const start = m.index;
  const valueStart = m.index + m[0].length;
  const end = matchBracket(src, valueStart);
  return src.slice(start, end) + ';';
}

// `function name(...) {...}` — returns the full declaration text.
export function sliceFunction(src, name) {
  const m = new RegExp(`function\\s+${name}\\s*\\(`, 'g').exec(src);
  if (!m) throw new Error(`function ${name} not found in oracle`);
  const bodyOpen = src.indexOf('{', matchBracket(src, m.index + m[0].length - 1));
  const end = matchBracket(src, bodyOpen);
  return src.slice(m.index, end);
}

// Evaluates the sliced declarations in an isolated scope and returns the named bindings.
export function evaluate(declarations, names) {
  const body = `${declarations.join('\n')}\nreturn { ${names.join(', ')} };`;
  // eslint-disable-next-line no-new-func
  return new Function('Math', body)(Math);
}
