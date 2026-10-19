import os
import hashlib

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

FIXTURE_FILES = [
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\ContinuousBeamRenumbering.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\ContinuousBeamRenumbering.ghx",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\CurveGenerator.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\CurveGenerator.ghx",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\MathSlider.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\MathSlider.ghx",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\RevitDirectShapeSyncMock.gh",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fixtures\RevitDirectShapeSyncMock.ghx",
    r"HPGrasshopper\tools\harness\fixtures\ContinuousBeamRenumbering.gh",
    r"HPGrasshopper\tools\harness\fixtures\ContinuousBeamRenumbering.ghx",
    r"HPGrasshopper\tools\harness\fixtures\CurveGenerator.gh",
    r"HPGrasshopper\tools\harness\fixtures\CurveGenerator.ghx",
    r"HPGrasshopper\tools\harness\fixtures\MathSlider.gh",
    r"HPGrasshopper\tools\harness\fixtures\MathSlider.ghx",
    r"HPGrasshopper\tools\harness\fixtures\RevitDirectShapeSyncMock.gh",
    r"HPGrasshopper\tools\harness\fixtures\RevitDirectShapeSyncMock.ghx",
    r"HPGrasshopper\definitions\ContinuousBeamRenumbering.gh",
    r"HPGrasshopper\definitions\ContinuousBeamRenumbering.ghx",
]

def hash_file(filepath):
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

def snapshot_fixtures():
    results = {}
    print(f"Checking {len(FIXTURE_FILES)} fixture files:")
    for rel_path in FIXTURE_FILES:
        full_path = os.path.join(BASE_DIR, rel_path)
        if not os.path.exists(full_path):
            print(f"MISSING: {rel_path}")
            results[rel_path] = None
            continue
        size = os.path.getsize(full_path)
        mtime = os.path.getmtime(full_path)
        sha = hash_file(full_path)
        results[rel_path] = {"size": size, "mtime": mtime, "sha256": sha}
        print(f"OK: {rel_path} (size={size}, sha256={sha[:16]}...)")
    return results

if __name__ == "__main__":
    snapshot_fixtures()
