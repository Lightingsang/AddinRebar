import json
import sys
import os
import time

PIPE_PATH = r'\\.\pipe\hprebar-mcp-r2026'

class RevitReviewerIPC:
    def __init__(self, pipe_path=PIPE_PATH, retries=15, delay=1.0):
        self.pipe_path = pipe_path
        self.msg_id = 9000
        self.f = None
        for i in range(retries):
            try:
                self.f = open(pipe_path, 'r+b', buffering=0)
                break
            except Exception as e:
                if i == retries - 1:
                    raise
                time.sleep(delay)

    def execute(self, code, transaction="none", dry_run=False, timeout=60, label="Reviewer Audit", args=None):
        self.msg_id += 1
        params = {
            'code': code,
            'transaction': transaction,
            'dryRun': dry_run,
            'timeoutSeconds': timeout,
            'label': label
        }
        if args is not None:
            params['args'] = args
        req = {
            'jsonrpc': '2.0',
            'id': self.msg_id,
            'method': 'revit.execute',
            'params': params
        }
        self.f.write((json.dumps(req) + '\n').encode('utf-8'))
        buf = b""
        while True:
            chunk = self.f.read(65536)
            if not chunk:
                break
            buf += chunk
            while b'\n' in buf:
                raw_line, buf = buf.split(b'\n', 1)
                raw_line = raw_line.strip()
                if not raw_line:
                    continue
                try:
                    data = json.loads(raw_line.decode('utf-8'))
                    if data.get('id') == self.msg_id:
                        return data
                except Exception:
                    continue

    def close(self):
        if self.f:
            try:
                self.f.close()
            except Exception:
                pass
            self.f = None

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

if __name__ == '__main__':
    with RevitReviewerIPC() as ipc:
        res = ipc.execute("return doc.Title;", label="Check Doc Title")
        print("Connected to Revit Doc Title:", res)
