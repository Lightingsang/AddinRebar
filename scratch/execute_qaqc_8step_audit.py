import sys
import os

# Redirect execution to authentic_qaqc_8step_audit.py to eliminate hardcoded shortcuts
sys.path.append(os.path.dirname(__file__))
from authentic_qaqc_8step_audit import AuthenticQaqcAuditHarness

if __name__ == '__main__':
    harness = AuthenticQaqcAuditHarness()
    results = harness.execute_audit()
    if results['verdict'].startswith('ĐÚNG'):
        sys.exit(0)
    else:
        sys.exit(1)
