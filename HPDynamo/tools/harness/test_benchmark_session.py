#!/usr/bin/env python3
"""Unit tests for HPDynamo Persistent Session Benchmark Engine (benchmark_session.py)."""

import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from benchmark_session import (  # noqa: E402
    compute_percentile,
    compute_latency_stats,
    evaluate_outcome,
    calculate_payload_bytes,
    build_workload_definitions,
)


class TestBenchmarkSession(unittest.TestCase):
    def test_compute_percentile_empty(self):
        self.assertEqual(compute_percentile([], 50), 0.0)

    def test_compute_percentile_single(self):
        self.assertEqual(compute_percentile([42.0], 50), 42.0)
        self.assertEqual(compute_percentile([42.0], 95), 42.0)

    def test_compute_percentile_type7_linear_interpolation(self):
        # 10 values: 10, 20, 30, 40, 50, 60, 70, 80, 90, 100
        samples = [10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0]
        # p50: k = 0.5 * 9 = 4.5 -> index 4 is 50, index 5 is 60 -> midpoint = 55.0
        p50 = compute_percentile(samples, 50.0)
        self.assertEqual(p50, 55.0)

        # p95: k = 0.95 * 9 = 8.55 -> index 8 is 90, index 9 is 100 -> 90 + 0.55 * 10 = 95.5
        p95 = compute_percentile(samples, 95.0)
        self.assertEqual(p95, 95.5)

    def test_compute_latency_stats(self):
        samples = [1.0, 2.0, 3.0, 4.0, 5.0]
        stats = compute_latency_stats(samples)
        self.assertEqual(stats["min"], 1.0)
        self.assertEqual(stats["max"], 5.0)
        self.assertEqual(stats["mean"], 3.0)
        self.assertEqual(stats["p50"], 3.0)

    def test_evaluate_outcome_tier1_rpc_error(self):
        resp = {"error": {"code": -32601, "message": "Method not found"}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)
        self.assertFalse(offline)
        self.assertIn("RPC Error -32601", detail)

    def test_evaluate_outcome_tier2_bridge_offline(self):
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

    def test_evaluate_outcome_tier2_generic_error(self):
        resp = {
            "result": {
                "isError": True,
                "content": [{"type": "text", "text": "System.NullReferenceException: Object ref not set"}],
            }
        }
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertEqual(status, "FAIL")
        self.assertFalse(success)
        self.assertFalse(offline)
        self.assertIn("NullReferenceException", detail)

    def test_evaluate_outcome_tier3_compile_error(self):
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

    def test_evaluate_outcome_tier3_success(self):
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

    def test_calculate_payload_bytes(self):
        tool_name = "test_tool"
        tool_args = {"key": "value"}
        resp = {"result": {"content": [{"type": "text", "text": "hello"}]}}
        req_b, resp_b = calculate_payload_bytes(tool_name, tool_args, resp)
        self.assertGreater(req_b, 10)
        self.assertGreater(resp_b, 10)

    def test_build_workload_definitions(self):
        defs = build_workload_definitions()
        self.assertEqual(len(defs), 5)
        ids = [d["id"] for d in defs]
        self.assertEqual(ids, ["WL-01", "WL-02", "WL-03", "WL-04", "WL-05"])


if __name__ == "__main__":
    unittest.main()
