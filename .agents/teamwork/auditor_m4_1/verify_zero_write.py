import os
import hashlib

dirs = [
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures",
    r"HPGrasshopper/tools/harness/fixtures",
    r"HPGrasshopper/definitions"
]

def hash_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

# Expected initial mtime and sha256
initial_snapshot = {
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\ContinuousBeamRenumbering.gh": (3560, "14c86b0ff93ca145", 1791806324.701597),
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\ContinuousBeamRenumbering.ghx": (26406, "75e54a486bd73e0f", 1791806324.6996286),
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\CurveGenerator.gh": (522, "46d7b45ee08220ab", 1791806324.6824036),
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\CurveGenerator.ghx": (4387, "51a32955ba25f791", 1791806324.6813762),
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\MathSlider.gh": (449, "1df7c0dbb6b33b73", 1791806324.67539),
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\MathSlider.ghx": (3727, "392533e14091046a", 1791806324.6743927),
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\RevitDirectShapeSyncMock.gh": (661, "c64e3c3dc2fc14fb", 1791806324.6893547),
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures\RevitDirectShapeSyncMock.ghx": (6230, "91c69de3b3c44db3", 1791806324.6883574),
    r"HPGrasshopper/tools/harness/fixtures\ContinuousBeamRenumbering.gh": (3560, "14c86b0ff93ca145", 1791806324.7035918),
    r"HPGrasshopper/tools/harness/fixtures\ContinuousBeamRenumbering.ghx": (26406, "75e54a486bd73e0f", 1791806324.7025943),
    r"HPGrasshopper/tools/harness/fixtures\CurveGenerator.gh": (522, "46d7b45ee08220ab", 1791806324.6843693),
    r"HPGrasshopper/tools/harness/fixtures\CurveGenerator.ghx": (4387, "51a32955ba25f791", 1791806324.683394),
    r"HPGrasshopper/tools/harness/fixtures\MathSlider.gh": (449, "1df7c0dbb6b33b73", 1791806324.6774006),
    r"HPGrasshopper/tools/harness/fixtures\MathSlider.ghx": (3727, "392533e14091046a", 1791806324.6763873),
    r"HPGrasshopper/tools/harness/fixtures\RevitDirectShapeSyncMock.gh": (661, "c64e3c3dc2fc14fb", 1791806324.691857),
    r"HPGrasshopper/tools/harness/fixtures\RevitDirectShapeSyncMock.ghx": (6230, "91c69de3b3c44db3", 1791806324.690352),
    r"HPGrasshopper/definitions\ContinuousBeamRenumbering.gh": (3560, "14c86b0ff93ca145", 1791806324.6956115),
    r"HPGrasshopper/definitions\ContinuousBeamRenumbering.ghx": (26406, "75e54a486bd73e0f", 1791806324.6944954),
}

all_match = True
for path, (exp_size, exp_sha, exp_mtime) in initial_snapshot.items():
    stat = os.stat(path)
    sha = hash_file(path)
    mtime_match = (stat.st_mtime == exp_mtime)
    sha_match = sha.startswith(exp_sha)
    size_match = (stat.st_size == exp_size)
    if not (mtime_match and sha_match and size_match):
        all_match = False
        print(f"MUTATION DETECTED: {path} (Mtime match: {mtime_match}, SHA match: {sha_match}, Size match: {size_match})")
    else:
        print(f"VERIFIED UNTOUCHED: {path}")

print(f"\nFinal Zero-Write Verdict: {'100% UNTOUCHED (ZERO WRITES)' if all_match else 'MUTATION DETECTED'}")
