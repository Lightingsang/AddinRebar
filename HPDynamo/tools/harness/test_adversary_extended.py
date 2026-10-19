#!/usr/bin/env python3
"""Extended empirical stress test harness for benchmark_session.py.

Written by challenger_c_iter2_1 to empirically probe all conceivable edge cases,
boundary conditions, type corruptions, huge payloads, and failure states.
"""

import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from benchmark_session import (
    compute_percentile,
    compute_latency_stats,
    evaluate_outcome,
    calculate_payload_bytes,
    execute_workload,
)


class MockServerExtended:
    def __init__(self, cold_response, warm_responses):
        self.cold_response = cold_response
        self.warm_responses = list(warm_responses)
        self.warm_idx = 0
        self.calls_made = []

    def call(self, tool_name, tool_args):
        self.calls_made.append((tool_name, tool_args))
        if self.warm_idx == 0 and self.cold_response is not None:
            res = self.cold_response
            self.cold_response = None
            return res
        if self.warm_idx < len(self.warm_responses):
            res = self.warm_responses[self.warm_idx]
            self.warm_idx += 1
            return res
        return {"error": {"code": -32603, "message": "No more mock responses"}}


class TestAdversaryExtended(unittest.TestCase):
    # Group 1: Non-Dict Outer Responses
    def test_outer_invalid_types(self):
        for val in [None, [{"result": 1}], "Server Error 500", 0, 42, True, False]:
            status, success, _, _ = evaluate_outcome(val)
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)

    # Group 2: Malformed and Bizarre 'error' Payloads
    def test_error_payload_variants(self):
        for err in [{}, {"code": None, "message": None}, 500, [1, 2, 3], True]:
            status, success, _, detail = evaluate_outcome({"error": err})
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)
            self.assertIn("RPC Error", detail)

    def test_both_error_and_result_present(self):
        resp = {"error": {"code": -32000, "message": "Failed"}, "result": {"content": [{"type": "text", "text": "OK"}]}}
        status, success, _, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)
        self.assertIn("RPC Error", detail)

    # Group 3: Malformed 'result' and 'content' Structures
    def test_result_not_dict(self):
        for res_val in ["string_result", 12345, [1, 2], True]:
            status, success, _, detail = evaluate_outcome({"result": res_val})
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)
            self.assertIn("Malformed result: not a dict", detail)

    def test_content_not_list(self):
        for cnt in [{"text": "hello"}, 123, True, "string"]:
            status, success, _, detail = evaluate_outcome({"result": {"content": cnt}})
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)
            self.assertIn("Missing or empty content", detail)

    def test_content_list_with_no_text_elements(self):
        resp = {"result": {"content": [{"type": "image", "data": "bytes"}, None, 1234]}}
        status, success, _, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)
        self.assertIn("Empty text in tool content", detail)

    def test_content_text_is_whitespace_only(self):
        for ws in ["", " ", "   \n\t\r\n   "]:
            status, success, _, detail = evaluate_outcome({"result": {"content": [{"type": "text", "text": ws}]}})
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)
            self.assertIn("Empty text in tool content", detail)

    def test_content_mixed_valid_and_invalid(self):
        resp = {"result": {"content": [None, {"invalid": True}, {"type": "text", "text": "Valid response payload"}]}}
        status, success, _, _ = evaluate_outcome(resp)
        self.assertEqual(status, "PASS")
        self.assertTrue(success)

    # Group 4: Error String Triggers in Text Content
    def test_text_error_triggers(self):
        triggers = [
            ("The script does not compile: CS1002 ; expected", "Compile Error"),
            ("Execution failed: node 'Point' threw an exception", "Execution Failure"),
            ("Exception thrown while evaluating graph", "Execution Failure"),
        ]
        for text_msg, expected_err in triggers:
            status, success, _, detail = evaluate_outcome({"result": {"content": [{"type": "text", "text": text_msg}]}})
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)
            self.assertIn(expected_err, detail)

    def test_iserror_flag_with_arbitrary_failure(self):
        status, success, _, detail = evaluate_outcome({"result": {"isError": True, "content": [{"type": "text", "text": "Database is locked"}]}})
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)
        self.assertIn("MCP isError", detail)

    def test_offline_pipe_and_no_doc_triggers(self):
        for msg in ["Cannot connect to pipe hpdynamo-mcp-2026: timed out", "No document is open in Revit right now"]:
            status, success, offline, _ = evaluate_outcome({"result": {"content": [{"type": "text", "text": msg}]}})
            self.assertEqual(status, "SKIP")
            self.assertFalse(success)
            self.assertTrue(offline)

    # Group 5: Massive Payloads and Unicode Robustness
    def test_massive_unicode_text(self):
        huge_str = "HPDynamo Thử Nghiệm Đối Kháng 🚀 Tiếng Việt 12345 " * 2000
        resp = {"result": {"content": [{"type": "text", "text": huge_str}]}}
        status, success, _, _ = evaluate_outcome(resp)
        self.assertEqual(status, "PASS")
        self.assertTrue(success)
        req_b, resp_b = calculate_payload_bytes("test_tool", {"arg": huge_str}, resp)
        self.assertGreater(req_b, 100000)
        self.assertGreater(resp_b, 100000)

    # Group 6: Boundary Values in Percentiles and Stats
    def test_percentile_boundaries(self):
        samples = [10.0, 20.0, 30.0, 40.0, 50.0]
        self.assertEqual(compute_percentile(samples, 0.0), 10.0)
        self.assertEqual(compute_percentile(samples, 100.0), 50.0)
        self.assertEqual(compute_percentile([100.0, 200.0], 50.0), 150.0)

    def test_latency_stats_single(self):
        stats = compute_latency_stats([55.5])
        self.assertEqual(stats["min"], 55.5)
        self.assertEqual(stats["max"], 55.5)
        self.assertEqual(stats["mean"], 55.5)
        self.assertEqual(stats["p50"], 55.5)
        self.assertEqual(stats["p95"], 55.5)

    # Group 7: Loop Fail-Fast & Degradation Mechanics in execute_workload
    def test_cold_fail_aborts_immediately(self):
        server = MockServerExtended({"error": {"code": -32600, "message": "Invalid"}}, [{"result": {"content": [{"type": "text", "text": "w"}]}}])
        wl = {"id": "WL-01", "name": "Cold Fail", "tool": "t", "args": {}, "baseline": {}}
        status, rec = execute_workload(server, wl, warm_runs=5, startup_ms=50.0)
        self.assertEqual(status, "FAIL")
        self.assertEqual(len(server.calls_made), 1)
        self.assertEqual(rec["upgraded"]["failureCount"], 1)

    def test_cold_skip_aborts_immediately(self):
        server = MockServerExtended({"result": {"isError": True, "content": [{"type": "text", "text": "Dynamo bridge not connected"}]}}, [])
        wl = {"id": "WL-02", "name": "Cold Skip", "tool": "t", "args": {}, "baseline": {}}
        status, rec = execute_workload(server, wl, warm_runs=5, startup_ms=50.0)
        self.assertEqual(status, "SKIP")
        self.assertEqual(len(server.calls_made), 1)
        self.assertEqual(rec["upgraded"]["failureCount"], 0)

    def test_warm_disconnect_halts_warm_loop(self):
        c_ok = {"result": {"content": [{"type": "text", "text": "OK"}]}}
        w_ok = {"result": {"content": [{"type": "text", "text": "OK"}]}}
        w_off = {"result": {"content": [{"type": "text", "text": "Dynamo bridge not connected"}]}}
        server = MockServerExtended(c_ok, [w_ok, w_off, w_ok, w_ok])
        wl = {"id": "WL-03", "name": "Warm Disconnect", "tool": "t", "args": {}, "baseline": {}}
        status, rec = execute_workload(server, wl, warm_runs=4, startup_ms=50.0)
        self.assertEqual(status, "SKIP")
        self.assertEqual(rec["upgraded"]["status"], "CHƯA TEST (Bridge Offline During Warm)")
        self.assertEqual(len(server.calls_made), 3)

    def test_warm_nine_out_of_ten_failures(self):
        c_ok = {"result": {"content": [{"type": "text", "text": "OK"}]}}
        w_ok = {"result": {"content": [{"type": "text", "text": "OK"}]}}
        w_fail = {"error": {"code": -32000, "message": "Failure"}}
        server = MockServerExtended(c_ok, [w_fail] * 9 + [w_ok])
        wl = {"id": "WL-04", "name": "Degraded", "tool": "t", "args": {}, "baseline": {}}
        status, rec = execute_workload(server, wl, warm_runs=10, startup_ms=50.0)
        self.assertEqual(status, "PASS")
        self.assertEqual(rec["upgraded"]["failureCount"], 9)
        self.assertEqual(rec["upgraded"]["samplesCount"], 1)
        self.assertIn("Degraded: 1/10 passed", rec["upgraded"]["status"])


if __name__ == "__main__":
    unittest.main()
