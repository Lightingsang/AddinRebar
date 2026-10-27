// Form paths as the host names fields in its errors ("detail.endZoneFraction", "special.lapRows[2].diameter"): the page
// edits by the same path, so an error lands on the input that holds the value.

type Key = string | number;

export function parsePath(path: string): Key[] {
  return path.split('.').flatMap((part) => {
    const match = /^(\w+)((?:\[\d+\])*)$/.exec(part);
    if (!match) throw new Error(`Not a form path: '${path}'`);
    return [match[1]!, ...[...match[2]!.matchAll(/\[(\d+)\]/g)].map((m) => Number(m[1]))];
  });
}

export function getIn(value: unknown, path: string): unknown {
  return parsePath(path).reduce<unknown>((node, key) => (node == null ? undefined : (node as Record<Key, unknown>)[key]), value);
}

/** A copy of `value` with the node at `path` replaced; the objects and arrays along the path are new, the rest shared. */
export function setIn<T>(value: T, path: string, next: unknown): T {
  const keys = parsePath(path);
  const write = (node: unknown, depth: number): unknown => {
    const key = keys[depth]!;
    const child = depth === keys.length - 1 ? next : write((node as Record<Key, unknown>)[key], depth + 1);
    if (Array.isArray(node)) return node.map((item, i) => (i === key ? child : item));
    return { ...(node as object), [key]: child };
  };
  return write(value, 0) as T;
}

/** The tab a field belongs to: the first part of its path. */
export const tabOf = (path: string) => path.split(/[.[]/)[0]!;
