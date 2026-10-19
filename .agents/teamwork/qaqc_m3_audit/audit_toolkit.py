import json
import os
import sys

REVIT_PIPE = r'\\.\pipe\hprebar-mcp-r2026'
ACAD_PIPE = r'\\.\pipe\hpautocad-mcp-2026'

class PipeClient:
    def __init__(self, pipe_path, method_prefix):
        self.pipe_path = pipe_path
        self.method_prefix = method_prefix
        self.f = open(pipe_path, 'r+b', buffering=0)
        self.msg_id = 100

    def execute(self, code, transaction="none", timeout=60, label="Audit Query", args=None):
        self.msg_id += 1
        params = {
            'code': code,
            'transaction': transaction,
            'dryRun': False,
            'timeoutSeconds': timeout,
            'label': label
        }
        if args is not None:
            params['args'] = args
        req = {
            'jsonrpc': '2.0',
            'id': self.msg_id,
            'method': f'{self.method_prefix}.execute',
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
        try:
            self.f.close()
        except Exception:
            pass

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

def get_revit_client():
    return PipeClient(REVIT_PIPE, 'revit')

def get_acad_client():
    return PipeClient(ACAD_PIPE, 'autocad')
