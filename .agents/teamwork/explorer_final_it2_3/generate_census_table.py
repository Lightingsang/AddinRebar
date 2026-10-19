import os
import re

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_final_it2_3\methods_ast_utf8.txt", "r", encoding="utf-8") as f:
    text = f.read()

file_sections = text.split("FILE: ")

summary_data = []

for sec in file_sections[1:]:
    lines = sec.strip().split("\n")
    fname = lines[0].strip()
    path_line = [l for l in lines if l.strip().startswith("Path:")][0]
    rel_path = path_line.split("Path:")[1].strip()
    phys_lines = int([l for l in lines if "Physical Lines:" in l][0].split("Physical Lines:")[1].split()[0])
    
    members = []
    in_members = False
    for l in lines:
        if "Members (sorted" in l:
            in_members = True
            continue
        if in_members and l.strip().startswith("["):
            status_match = re.search(r"\[(.*?)\]\s+(Method|Constructor)\s+([\w_]+)\s+\|\s+SpanWithAttr:\s+(\d+)\s+\(L(\d+)-L(\d+)\)\s+\|\s+SpanDecl:\s+(\d+)\s+\(L(\d+)-L(\d+)\)", l)
            if status_match:
                status_raw = status_match.group(1).strip()
                mtype = status_match.group(2).strip()
                mname = status_match.group(3).strip()
                span_attr = int(status_match.group(4))
                l_start_attr = int(status_match.group(5))
                l_end_attr = int(status_match.group(6))
                span_decl = int(status_match.group(7))
                l_start_decl = int(status_match.group(8))
                l_end_decl = int(status_match.group(9))
                is_pass = "VIOLATION" not in status_raw
                members.append({
                    "type": mtype,
                    "name": mname,
                    "span_attr": span_attr,
                    "span_decl": span_decl,
                    "start": l_start_attr,
                    "end": l_end_attr,
                    "pass": is_pass,
                })
    summary_data.append({
        "filename": fname,
        "rel_path": rel_path,
        "phys_lines": phys_lines,
        "members": members,
    })

# Generate Markdown Tables
md_lines = []

# Table 1: File-Level Summary
md_lines.append("### Table 1: Tier 5 Test Files Physical Line Count & Clean Code Compliance Summary")
md_lines.append("")
md_lines.append("| # | File Path (Relative to Repo Root) | Lines | C2 (<=300) | Members | Max Method Name | Max Span | M3 (<=50) | Status |")
md_lines.append("|---|---|:---:|:---:|:---:|---|:---:|:---:|:---:|")

for idx, f in enumerate(summary_data, 1):
    max_m = max(f["members"], key=lambda m: m["span_attr"]) if f["members"] else None
    max_name = max_m["name"] if max_m else "N/A"
    max_span = f"{max_m['span_attr']}L (L{max_m['start']}-L{max_m['end']})" if max_m else "N/A"
    c2_status = "PASS" if f["phys_lines"] <= 300 else "**FAIL**"
    has_m3_violation = any(not m["pass"] for m in f["members"])
    m3_status = "**FAIL (59L)**" if has_m3_violation else "PASS"
    overall_status = "**NON-COMPLIANT**" if has_m3_violation else "COMPLIANT"
    md_lines.append(f"| {idx} | `{f['rel_path']}` | {f['phys_lines']} | {c2_status} | {len(f['members'])} | `{max_name}` | {max_span} | {m3_status} | {overall_status} |")

md_lines.append("")
md_lines.append("---")
md_lines.append("")

# Table 2: Discrepancy Reconciliation Table
md_lines.append("### Table 2: Discrepancy Reconciliation — Reported Attestation vs. Empirical Ground Truth")
md_lines.append("")
md_lines.append("| Deliverable / Scope | Artifact Checked | Claimed Attestation | Empirical Ground Truth | Discrepancy Root Cause | Impact & Severity |")
md_lines.append("|---|---|---|---|---|---|")
md_lines.append("| **HPBim.Client.Tests** | `challenger_tier5_2/report.md`<br>(Section 5, Item 2) &<br>`challenger_tier5_2/handoff.md`<br>(Section 4, Line 100) | *\"Method Length (Rule M3): All test methods strictly <= 50 lines.\"* | Method `ReconcileAllUnknownIntents_InterleavedStatuses_ReconcilesEachAccurately`<br>spans **59 lines** (L38-L96 with `[Fact]`) / **58 lines** (L39-L96 declaration to closing brace). | Omission of automated AST/metric measurement prior to certifying handoff. Self-certifying assumption without verification. | **BLOCKING (Major)**<br>Violates Rule M3 and Golden Rule 0 (Zero Speculation / Authenticity). Triggered Forensic Auditor Integrity Violation. |")
md_lines.append("| **HPBim.Core.Tests** | `challenger_tier5_1/report.md`<br>(Section 4, Line 100) &<br>`challenger_tier5_1/handoff.md`<br>(Section 5, Line 111) | Test command:<br>`dotnet test ... --filter \"FullyQualifiedName~Adversarial\"` | Command crashes with **exit code 5** (`Test run summary: Zero tests ran`, `error: 1`) under Microsoft.Testing.Platform. | Using obsolete VSTest `--filter` syntax instead of MTP-native `--filter-namespace HPBim.Core.Tests.Adversarial`. | **OPERATIONAL (Minor)**<br>Breaks automated test reproducibility and independent verification in MTP environment. |")
md_lines.append("| **HPBim.Core.Tests** | `challenger_tier5_1/report.md`<br>(Section 3, Table) | Line counts reported:<br>- Validator: 245 lines<br>- Resolver: 189 lines<br>- Key: 129 lines<br>- Geometric: 225 lines<br>Total: 788 lines | Exact empirical physical line counts:<br>- Validator: **272 lines** (+27)<br>- Resolver: **187 lines** (-2)<br>- Key: **127 lines** (-2)<br>- Geometric: **256 lines** (+31)<br>Total: **842 lines** (+54) | Reporting draft/intermediate file line counts instead of measuring final on-disk files. | **INFORMATIONAL**<br>All files remain <= 300 lines (Rule C2 PASS), but handoff metrics were factually inaccurate. |")

md_lines.append("")
md_lines.append("---")
md_lines.append("")

# Table 3: Exhaustive Member Census
md_lines.append("### Table 3: Complete Member-by-Member Inventory Across All 10 Test Files (90 Members)")
md_lines.append("")
md_lines.append("| # | File | Member Name | Type | Start Line | End Line | Line Span | Rule M3 (<=50) | Status |")
md_lines.append("|---|---|---|---|:---:|:---:|:---:|:---:|:---:|")

global_count = 1
for f in summary_data:
    for m in f["members"]:
        span_str = f"{m['span_attr']}L"
        m3_str = "PASS" if m["pass"] else "**FAIL (>50)**"
        status_str = "OK" if m["pass"] else "**REJECT**"
        md_lines.append(f"| {global_count} | `{f['filename']}` | `{m['name']}` | {m['type']} | L{m['start']} | L{m['end']} | {span_str} | {m3_str} | {status_str} |")
        global_count += 1

output_md = "\n".join(md_lines)
with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_final_it2_3\census_tables.md", "w", encoding="utf-8") as f:
    f.write(output_md)

print("Wrote census_tables.md successfully.")
