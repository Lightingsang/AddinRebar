#!/usr/bin/env python3
"""Adversarial stress harness for benchmark_session.py.

Empirically tests edge cases, malformed payloads, failure masking,
and statistical computations to uncover false positives and crashes.
"""

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


class TestAdversarialBenchmark(unittest.TestCase):
    # =========================================================================
    # Group 1: Malformed and Edge-Case Response Handling in evaluate_outcome
    # =========================================================================

    def test_adv_result_is_none(self):
        """resp = {'result': None} must not crash with AttributeError and must return FAIL."""
        resp = {"jsonrpc": "2.0", "id": 1, "result": None}
        try:
            status, success, offline, detail = evaluate_outcome(resp)
            self.assertNotEqual(status, "PASS", "Should not pass when result is None")
            self.assertFalse(success, "Should not succeed when result is None")
        except AttributeError as e:
            self.fail(f"CRASH: evaluate_outcome raised AttributeError on {{'result': None}}: {e}")

    def test_adv_error_is_string(self):
        """resp = {'error': 'Internal Server Error'} must not crash and must return FAIL."""
        resp = {"jsonrpc": "2.0", "id": 1, "error": "Internal Server Error"}
        try:
            status, success, offline, detail = evaluate_outcome(resp)
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)
        except AttributeError as e:
            self.fail(f"CRASH: evaluate_outcome raised AttributeError on error as string: {e}")

    def test_adv_error_is_none(self):
        """resp = {'error': None} must not crash with AttributeError and must return FAIL."""
        resp = {"jsonrpc": "2.0", "id": 1, "error": None}
        try:
            status, success, offline, detail = evaluate_outcome(resp)
            self.assertEqual(status, "FAIL")
            self.assertFalse(success)
        except AttributeError as e:
            self.fail(f"CRASH: evaluate_outcome raised AttributeError on {{'error': None}}: {e}")

    def test_adv_content_is_none(self):
        """resp = {'result': {'content': None}} must not crash with TypeError."""
        resp = {"result": {"content": None}}
        try:
            status, success, offline, detail = evaluate_outcome(resp)
            self.assertNotEqual(status, "PASS", "Should not pass when content is None")
            self.assertFalse(success)
        except TypeError as e:
            self.fail(f"CRASH: evaluate_outcome raised TypeError on content=None: {e}")

    def test_adv_content_is_string(self):
        """resp = {'result': {'content': 'raw string'}} must not crash with AttributeError."""
        resp = {"result": {"content": "raw string"}}
        try:
            status, success, offline, detail = evaluate_outcome(resp)
            self.assertNotEqual(status, "PASS")
            self.assertFalse(success)
        except AttributeError as e:
            self.fail(f"CRASH: evaluate_outcome raised AttributeError on content='raw string': {e}")

    def test_adv_content_element_text_is_none(self):
        """resp = {'result': {'content': [{'type': 'text', 'text': None}]}} must not crash."""
        resp = {"result": {"content": [{"type": "text", "text": None}]}}
        try:
            status, success, offline, detail = evaluate_outcome(resp)
        except TypeError as e:
            self.fail(f"CRASH: evaluate_outcome raised TypeError on text=None: {e}")

    def test_adv_missing_result_and_error_fields(self):
        """resp = {'jsonrpc': '2.0', 'id': 1} (missing result and error) MUST NOT return PASS!"""
        resp = {"jsonrpc": "2.0", "id": 1}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS", "FALSE POSITIVE: missing result/error reported as PASS!")
        self.assertFalse(success, "FALSE POSITIVE: success=True on missing result/error!")

    def test_adv_empty_result_dict(self):
        """resp = {'result': {}} (missing content) MUST NOT return PASS!"""
        resp = {"result": {}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS", "FALSE POSITIVE: empty result {} reported as PASS!")
        self.assertFalse(success, "FALSE POSITIVE: success=True on empty result {}!")

    def test_adv_empty_content_list(self):
        """resp = {'result': {'content': []}} MUST NOT return PASS without content!"""
        resp = {"result": {"isError": False, "content": []}}
        status, success, offline, detail = evaluate_outcome(resp)
        self.assertNotEqual(status, "PASS", "FALSE POSITIVE: empty content [] reported as PASS!")
        self.assertFalse(success, "FALSE POSITIVE: success=True on empty content []!")

    # =========================================================================
    # Group 2: Failure Masking in execute_workload (Warm Loop False Positives)
    # =========================================================================

    def test_adv_all_warm_runs_fail_must_not_report_pass(self):
        """If cold passes but 100% warm runs fail, workload MUST NOT return PASS / Verified Live!"""
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

        # Adversarial assertion:
        self.assertNotEqual(
            status,
            "PASS",
            f"CRITICAL FALSE POSITIVE: execute_workload returned PASS when 5/5 warm runs failed! Record status: {rec['upgraded']['status']}",
        )
        self.assertNotIn(
            "Verified Live",
            rec["upgraded"]["status"],
            f"CRITICAL FALSE POSITIVE: Status marked 'Verified Live' when 0 warm samples succeeded!",
        )
        self.assertGreater(
            rec["upgraded"].get("failureCount", 0),
            0,
            "failureCount must be > 0 when warm runs failed!",
        )

    def test_adv_bridge_disconnects_during_warm_runs(self):
        """If cold passes but bridge disconnects on warm runs, workload MUST NOT report PASS / Verified Live!"""
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

        self.assertNotEqual(
            status,
            "PASS",
            f"CRITICAL FALSE POSITIVE: Returned PASS when bridge disconnected during warm runs! Record: {rec['upgraded']['status']}",
        )
        self.assertNotIn(
            "Verified Live",
            rec["upgraded"]["status"],
            "CRITICAL FALSE POSITIVE: Status marked 'Verified Live' when bridge was offline!",
        )

    def test_adv_partial_warm_failures_tracked(self):
        """If 3 of 5 warm runs fail, failureCount MUST reflect the failures, not be 0."""
        cold_ok = {"result": {"isError": False, "content": [{"type": "text", "text": '{"success": true}'}]}}
        warm_ok = {"result": {"isError": False, "content": [{"type": "text", "text": '{"success": true}'}]}}
        warm_fail = {"error": {"code": -32000, "message": "Intermittent failure"}}
        # 2 succeed, 3 fail
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
        self.assertEqual(
            failures,
            3,
            f"Intermittent failure masking: expected 3 failures, got failureCount={failures}",
        )

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


if __name__ == "__main__":
    unittest.main()
