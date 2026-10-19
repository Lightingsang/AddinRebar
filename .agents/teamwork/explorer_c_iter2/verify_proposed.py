#!/usr/bin/env python3
"""Adversarial stress harness testing proposed_benchmark_session.py."""

import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import proposed_benchmark_session as pbs
from proposed_benchmark_session import (
    compute_percentile,
    compute_latency_stats,
    evaluate_outcome,
    calculate_payload_bytes,
    execute_workload,
)

class MockServer:
    def __init__(self, cold_response, warm_responses):
        self.cold_response = cold_response
        self.warm_responses = list(warm_responses)
        self.warm_idx = 0

    def call(self, tool_name, tool_args):
        if self.warm_idx == 0 and self.cold_response is not None:
            res = self.cold_response
            self.cold_response = None
            return res
        if self.warm_idx < len(self.warm_responses):
            res = self.warm_responses[self.warm_idx]
            self.warm_idx += 1
            return res
        return {"error": {"code": -32603, "message": "No more mock responses"}}


class TestProposedAdversarialBenchmark(unittest.TestCase):
    # =========================================================================
    # Group 1: Malformed and Edge-Case Response Handling in evaluate_outcome
    # =========================================================================

    def test_adv_result_is_none(self):
        resp = {"jsonrpc": "2.0", "id": 1, "result": None}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS")
        self.assertFalse(success)

    def test_adv_error_is_string(self):
        resp = {"jsonrpc": "2.0", "id": 1, "error": "Internal Server Error"}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)

    def test_adv_error_is_none(self):
        resp = {"jsonrpc": "2.0", "id": 1, "error": None}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)

    def test_adv_content_is_none(self):
        resp = {"result": {"content": None}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS")
        self.assertFalse(success)

    def test_adv_content_is_string(self):
        resp = {"result": {"content": "raw string"}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS")
        self.assertFalse(success)

    def test_adv_content_element_text_is_none(self):
        resp = {"result": {"content": [{"type": "text", "text": None}]}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS")
        self.assertFalse(success)

    def test_adv_missing_result_and_error_fields(self):
        resp = {"jsonrpc": "2.0", "id": 1}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS")
        self.assertFalse(success)

    def test_adv_empty_result_dict(self):
        resp = {"result": {}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS")
        self.assertFalse(success)

    def test_adv_empty_content_list(self):
        resp = {"result": {"isError": False, "content": []}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS")
        self.assertFalse(success)

    # =========================================================================
    # Group 2: Failure Masking in execute_workload (Warm Loop False Positives)
    # =========================================================================

    def test_adv_all_warm_runs_fail_must_not_report_pass(self):
        cold_ok = {"result": {"isError": False, "content": [{"type": "text", "text": '{"success": true}'}]}}
        warm_fail = {"error": {"code": -32000, "message": "Simulated bridge crash"}}
        server = MockServer(cold_ok, [warm_fail] * 5)
        wl = {
            "id": "WL-ADV-01",
            "name": "Stress Workload",
            "tool": "get_dynamo_context",
            "args": {},
            "baseline": {"status": "Baseline"},
        }
        status, rec = execute_workload(server, wl, warm_runs=5, startup_ms=100.0)
        self.assertNotEqual(status, "PASS")
        self.assertNotIn("Verified Live", rec["upgraded"]["status"])
        self.assertGreater(rec["upgraded"].get("failureCount", 0), 0)

    def test_adv_bridge_disconnects_during_warm_runs(self):
        cold_ok = {"result": {"isError": False, "content": [{"type": "text", "text": '{"success": true}'}]}}
        warm_disconnect = {
            "result": {
                "isError": True,
                "content": [{"type": "text", "text": "Dynamo bridge not connected. Open Revit 2026 (pipe hpdynamo-mcp-2026)."}],
            }
        }
        server = MockServer(cold_ok, [warm_disconnect] * 5)
        wl = {
            "id": "WL-ADV-02",
            "name": "Bridge Disconnect Stress",
            "tool": "get_dynamo_context",
            "args": {},
            "baseline": {"status": "Baseline"},
        }
        status, rec = execute_workload(server, wl, warm_runs=5, startup_ms=100.0)
        self.assertNotEqual(status, "PASS")
        self.assertNotIn("Verified Live", rec["upgraded"]["status"])

    def test_adv_partial_warm_failures_tracked(self):
        cold_ok = {"result": {"isError": False, "content": [{"type": "text", "text": '{"success": true}'}]}}
        warm_ok = {"result": {"isError": False, "content": [{"type": "text", "text": '{"success": true}'}]}}
        warm_fail = {"error": {"code": -32000, "message": "Intermittent failure"}}
        responses = [warm_ok, warm_fail, warm_ok, warm_fail, warm_fail]
        server = MockServer(cold_ok, responses)
        wl = {
            "id": "WL-ADV-03",
            "name": "Intermittent Workload",
            "tool": "get_dynamo_context",
            "args": {},
            "baseline": {"status": "Baseline"},
        }
        status, rec = execute_workload(server, wl, warm_runs=5, startup_ms=100.0)
        failures = rec["upgraded"].get("failureCount", 0)
        self.assertEqual(failures, 3)

    # =========================================================================
    # Group 3: Statistical Edge Cases
    # =========================================================================

    def test_adv_percentile_empty(self):
        self.assertEqual(compute_percentile([], 50.0), 0.0)
        self.assertEqual(compute_percentile(None, 50.0), 0.0)

    def test_adv_percentile_single_sample(self):
        self.assertEqual(compute_percentile([77.7], 0.0), 77.7)
        self.assertEqual(compute_percentile([77.7], 50.0), 77.7)
        self.assertEqual(compute_percentile([77.7], 95.0), 77.7)
        self.assertEqual(compute_percentile([77.7], 100.0), 77.7)

    def test_adv_percentile_identical_samples(self):
        samples = [15.0] * 10
        self.assertEqual(compute_percentile(samples, 50.0), 15.0)
        self.assertEqual(compute_percentile(samples, 95.0), 15.0)

    def test_adv_latency_stats_none(self):
        stats = compute_latency_stats(None)
        self.assertEqual(stats["p50"], 0.0)
        self.assertEqual(stats["mean"], 0.0)

    def test_adv_latency_stats_zeros(self):
        stats = compute_latency_stats([0.0, 0.0])
        self.assertEqual(stats["p50"], 0.0)
        self.assertEqual(stats["min"], 0.0)
        self.assertEqual(stats["mean"], 0.0)

    # =========================================================================
    # Group 4: Standard Regression Cases from test_benchmark_session.py
    # =========================================================================

    def test_std_rpc_error(self):
        resp = {"error": {"code": -32601, "message": "Method not found"}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)
        self.assertIn("RPC Error -32601", detail)

    def test_std_bridge_offline(self):
        resp = {
            "result": {
                "isError": True,
                "content": [{"type": "text", "text": "Dynamo bridge not connected. Open Revit 2026..."}],
            }
        }
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "SKIP")
        self.assertFalse(success)
        self.assertTrue(offline)
        self.assertIn("offline mode", detail)

    def test_std_compile_error(self):
        resp = {
            "result": {
                "isError": False,
                "content": [{"type": "text", "text": "The script does not compile: CS0246 type not found"}],
            }
        }
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)
        self.assertIn("Compile Error", detail)

    def test_std_success(self):
        resp = {
            "result": {
                "isError": False,
                "content": [{"type": "text", "text": '{"success": true, "nodeCount": 5}'}],
            }
        }
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "PASS")
        self.assertTrue(success)
        self.assertFalse(offline)
        self.assertEqual(detail, "")


if __name__ == "__main__":
    unittest.main()
