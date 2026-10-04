"""Throw-away baseline scan: approximate the brief's 8 script-quality rules over every embedded seed code.cs.

SYNTACTIC APPROXIMATION ONLY (regex + brace counting, strings/comments stripped crudely) - not the planned
Roslyn walker. Counts are an order-of-magnitude baseline, false positives/negatives are expected.

Usage: python scan-seed-quality.py [--md]   (run from anywhere; prints a markdown report)
"""
import glob, os, re, sys
from collections import Counter, defaultdict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
HOSTS = ["HPRebar", "HPAutoCad", "HPCivil3d", "HPNavis", "HPEtabs", "HPSap2000",
         "HPRobot", "HPExcel", "HPPowerBi", "HPTekla"]
BLOCKING = ["R-B1", "R-B2", "R-B3"]
WARNING = ["R-W1", "R-W2", "R-W3", "R-W4", "R-W5"]
VAGUE_NAMES = ["data", "tmp", "temp", "obj", "res", "result2", "val", "item2", "foo", "bar",
               "x1", "x2", "stuff", "thing", "info2"]
CODE_COMMENT = re.compile(r"^\s*(var|if|for|foreach|while|return|using|new|throw|try|catch)\b|\w\s*\(.*\)\s*;\s*$")
CONTROL = re.compile(r"\b(if|else|for|foreach|while|do|switch|try|catch|finally|using|lock)\b")
DECL = re.compile(r"\b(?:var|int|long|double|string|bool|object|dynamic|[A-Z]\w*(?:<[^>]*>)?)\s+(\w+)\s*=(?!=)")
LOCAL_FN = re.compile(r"^\s*(?:static\s+)?[\w<>\[\],.? ]+\s+\w+\s*\(([^)]*)\)\s*(?:\{|=>|$)")


def strip_strings(line):
    line = re.sub(r'@"(?:[^"]|"")*"', '""', line)
    line = re.sub(r'\$?"(?:\\.|[^"\\])*"', '""', line)
    return re.sub(r"'(?:\\.|[^'\\])'", "''", line)


def code_part(line):
    s = strip_strings(line)
    i = s.find("//")
    return s if i < 0 else s[:i]


def blocks(lines):
    """Yield (start, end, header) for every brace block (approximate)."""
    stack = []
    for n, raw in enumerate(lines, 1):
        for ch in code_part(raw):
            if ch == "{":
                stack.append(n)
            elif ch == "}" and stack:
                start = stack.pop()
                header = code_part(lines[start - 1]).strip() or (code_part(lines[start - 2]).strip() if start > 1 else "")
                yield start, n, header


def scan(path):
    lines = open(path, encoding="utf-8-sig").read().splitlines()
    hits = defaultdict(list)
    text = "\n".join(strip_strings(l) for l in lines)
    for n, raw in enumerate(lines, 1):
        m = re.match(r"^\s*//(?!/)(.*)$", raw)
        if m:
            body = m.group(1).strip()
            if body and (body.endswith((";", "{", "}")) or CODE_COMMENT.search(body)):
                hits["R-B1"].append(n)
    for m in re.finditer(r"catch\b[^{]*\{\s*\}", text):
        hits["R-B2"].append(text.count("\n", 0, m.start()) + 1)
    if len(lines) > 300:
        hits["R-B3"].append(len(lines))
    depth_stack = []
    for start, end, header in blocks(lines):
        if end - start + 1 > 50:
            hits["R-W1"].append(start)
        h = header.lstrip("}").strip()
        if h.startswith("catch") and re.match(r"catch\s*\(\s*(System\.)?Exception\b", h):
            body = "\n".join(code_part(l) for l in lines[start - 1:end])
            if not re.search(r"\bthrow\b|\breturn\b", body):
                hits["R-W5"].append(start)
    # nesting: count enclosing braces whose header is a control-flow keyword (approximate)
    stack = []
    for n, raw in enumerate(lines, 1):
        c = code_part(raw)
        for ch in c:
            if ch == "{":
                prev = c.strip() if c.strip() != "{" else (code_part(lines[n - 2]).strip() if n > 1 else "")
                stack.append(bool(CONTROL.search(prev)))
                if sum(stack) > 3 and n not in hits["R-W2"]:
                    hits["R-W2"].append(n)
            elif ch == "}" and stack:
                stack.pop()
    for n, raw in enumerate(lines, 1):
        c = code_part(raw)
        for m in DECL.finditer(c):
            if m.group(1) in VAGUE_NAMES:
                hits["R-W3"].append(n)
        fm = LOCAL_FN.match(c)
        if fm and not CONTROL.match(c.strip()) and re.search(r"(^|,)\s*bool\s+\w+", fm.group(1)):
            hits["R-W4"].append(n)
    return len(lines), hits


def main():
    per_host = {}
    blocking_files, sizes, details = [], [], []
    for host in HOSTS:
        files = sorted(glob.glob(os.path.join(ROOT, host, f"{host}.Mcp.Server", "Registry", "SeedLibrary", "**", "code.cs"), recursive=True))
        counts = Counter()
        for f in files:
            n, hits = scan(f)
            rel = os.path.relpath(f, ROOT).replace("\\", "/")
            sizes.append((n, rel))
            for rule, where in hits.items():
                counts[rule] += len(where)
                if rule in BLOCKING:
                    blocking_files.append((rel, rule, where))
                details.append((rel, rule, where))
        per_host[host] = (len(files), counts)
    print("| Host | code.cs | " + " | ".join(BLOCKING + WARNING) + " |")
    print("|---|---:|" + "---:|" * (len(BLOCKING) + len(WARNING)))
    total = Counter()
    for host, (nfiles, c) in per_host.items():
        total.update(c)
        print(f"| {host} | {nfiles} | " + " | ".join(str(c.get(r, 0)) for r in BLOCKING + WARNING) + " |")
    print(f"| **Total** | {sum(v[0] for v in per_host.values())} | " + " | ".join(str(total.get(r, 0)) for r in BLOCKING + WARNING) + " |")
    print("\n### Files with blocking hits\n")
    print("| File | Rule | Lines |\n|---|---|---|")
    for rel, rule, where in blocking_files:
        print(f"| {rel} | {rule} | {', '.join(map(str, where))} |")
    print("\n### Top 10 largest code.cs\n")
    print("| Lines | File |\n|---:|---|")
    for n, rel in sorted(sizes, reverse=True)[:10]:
        print(f"| {n} | {rel} |")
    if "--details" in sys.argv:
        print("\n### All hits\n")
        for rel, rule, where in details:
            print(f"- {rule} {rel}: {', '.join(map(str, where))}")


if __name__ == "__main__":
    main()
