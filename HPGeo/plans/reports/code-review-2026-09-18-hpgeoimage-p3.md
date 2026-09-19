# Code review — HPGeo satellite imagery P3 (`-HPGEOIMAGE`, RasterInserter, ImageryPipeline)

Date 2026-09-18 · scope: the 11 files in the brief (new/changed) · findings only, nothing edited.
Baseline measured now: `dotnet build HPGeo.slnx -c Debug -p:DeployBundle=false` → 0 warnings / 0 errors; `dotnet test HPGeo.Tests` → 134 pass, 1 skip (live tiles). Plan numbers, argument edge cases and the k0 shift below were probed with a scratch console against `HPGeo.Core`; the AutoCAD.NET 25.1 member claims (`RasterImageDef.ActiveFileName` setter, `RasterImageDef.SuggestName`, `HostApplicationServices.UserBreak()`) were read from `AcDbMgd.dll` metadata.

**Verification state:** the C# is Implemented + Built + unit-tested. It has **not run in AutoCAD**: `%LocalAppData%\HPGeo\logs\hpgeo-20260918.log` has no `HPGEOIMAGE` line and `output/acceptance/summary.json` (21:18) has 33 checks, i.e. the pre-imagery script. The P1 network gate did run (`spike: tiles 4/4 fetched (z19)`, 22:49). Treat everything from `RasterInserter.Insert` on as unverified.

## What is solid (not re-litigated)

- `RasterImage.Orientation` = `(lowerLeft, (WidthM/f, 0, 0), (0, HeightM/f, 0))` is the documented contract (origin lower-left, U = full width vector, V = full height vector in drawing units); `OutputRaster.LowerLeft` = `(OriginE, OriginN − HeightM)` is right, and the harness corner check reproduces it (probe: origin 600072.308 / 1231638.428 = ring bbox ± 30 m exactly).
- Placement is a warp (`RasterWarper.Warp` over the 64-px control grid); `AffineFit` is report-only; no rotation anywhere.
- Thread discipline holds: `TileFetcher` never touches AutoCAD, every `await` is `ConfigureAwait(false)`, the progress callback only exchanges an int, `Stitch`/`Decode`/`StampAttribution`/`Insert` all run on the command thread. `Task.Run` never posts back to the main thread, so the blocking wait cannot deadlock.
- Undo: `-HPGEOIMAGE` keeps its marker (`HPGeoCommands.cs:29`), the read transactions abort harmlessly, `Insert` + `DocumentSettingsStore.Write` sit in the same command → one `U` group. `AssociateRasterDef` after `AppendEntity`/`AddNewlyCreatedDBObject`, `EnableReactors(true)`, `DrawOrderTable.MoveToBottom`, layer lock/freeze refused before any write — all in the right order.
- Refusal codes and the harness regexes agree: `cm=120` → `OUTSIDE_VIETNAM` (Lon 120.92), `margin=2000` → `TOO_MANY_TILES` (3 190 tiles, 13 940 × 14 436 px), `layer=NOSUCHLAYER` → `NO_BOUNDARY`; the success run is z19, 15 tiles (3×5), 491 × 987 px @ 0.293 m/px, so `<= 400` and `ps <= 0.5` pass. Log/console strings match the patterns character for character (`fetched N tiles z=Z … warped WxH inserted <HEX> on HPGEO-IMAGE`, `Imagery: N RasterImage on HPGEO-IMAGE`, `HPGeo: OK — RasterImage`).
- Caps unchanged (400 / 2048 / 0.30 / 30 / `HPGEO-IMAGE`); no new NuGet (csproj diff = `InternalsVisibleTo` only); `GeoSettings`/Xrecord additions are additive and read leniently.
- PowerShell: no em dash in any new string (only in one comment), `New-Object HPGeoNative+RECT` + `[ref]` out-struct, `PrintWindow` flag 2, profile-sysvar restore and the pid-guarded kill are untouched.

## Findings

### High

**H1 — The download blocks AutoCAD's main thread with no cancellation and no upper bound.**
`HPGeo.AutoCad/Imagery/ImageryPipeline.cs:114-126` (`Task.Run` + `WaitOne(250)` loop), called from `HPGeoImageScriptCommand.cs:101` with the default token — nothing ever cancels `ct`.
Worst case per tile is 3 attempts × 15 s + 1.5 s back-off = 46.5 s; 400 tiles / 4 in flight = **77 min** with acad.exe "Not Responding", Escape dead (the thread is in `WaitOne`, not at a prompt), the only exit = kill acad.exe and lose unsaved work. Realistic trigger: a captive-portal or black-holing proxy where every request runs to the 15 s timeout — 100 tiles = 19 min.
Fix: (1) poll `HostApplicationServices.Current.UserBreak()` (verified: `bool UserBreak()`, main thread, in `AcDbMgd`) in the loop and cancel a `CancellationTokenSource`; (2) give the run a deadline (`CancelAfter(min(120 s, tiles × 2 s))`) and report `CANCELLED` / `TILE_FETCH_FAILED` as a refusal, not as `[ERR] lỗi`; (3) in `TileFetcher`, stop after N consecutive network failures (not 404s) instead of retrying every remaining tile. Add a stub-handler test where `SendAsync` never completes and assert the pipeline returns within the deadline.

**H2 — Zone precedence is only applied to `cm`; `k0`/`fe`/`fn` and `unit` fall back to the defaults, and the drawing's stored values are then overwritten with those defaults.**
`HPGeoImageScriptCommand.cs:65` resolves `cm` arg → DWG record → user; `:100` builds `args.TmFor(cm)` with `args.ScaleFactor/FalseEasting/FalseNorthing`, which `ImageArguments.cs:74-76` already replaced with 0.9999 / 500 000 / 0 (the record cannot tell "absent" from "default"); `:71` takes `unit` from INSUNITS only, ignoring `stored.Unit`; `:108-109` writes `ScaleFactor = args.ScaleFactor …` back into the Xrecord.
Scenario A: a drawing exported through the dialog with k0 = 0.9996 (stored). `-HPGEOIMAGE layer=RANH` (no cm) picks cm from the record, k0 from the default → the raster lands **372 m south / 31 m west** of the parcel (probe at E 600 125 N 1 231 608: latitude of origin 0 makes k0 scale the whole 1 231 km), the `IMAGE_DISTORTION` check cannot see it, and the record now says k0 = 0.9999, so the next KMZ export is also wrong. Scenario B (more common): INSUNITS 0 drawing whose record stores `unit=Meters` from the dialog → `-HPGEOIMAGE` refuses `UNKNOWN_UNIT` although the drawing knows its unit.
Fix: make `ScaleFactor/FalseEasting/FalseNorthing` nullable in `ImageArguments` (presence = given), resolve each through the same arg → record → user → default chain as `cm`, same for `unit` (arg → record → INSUNITS), and write back only the resolved values. Pin with a test on the resolution function (extract it from the command so it is testable without AutoCAD).

### Medium

**M1 — An existing image definition with the same key is hijacked.**
`RasterInserter.cs:63-69`: `dict.Contains(definitionName)` (key = PNG name without extension) → `Unload`, `SourceFileName = ours`, `Load`. That repoints *any* definition with that key — a user's IMAGEATTACHed `site.jpg` (key `site`) when `out=site.png`, or the previous run's own definition when a script reuses `out=` for a second parcel: the old RasterImage keeps its frame and now shows the new pixels, i.e. wrong imagery under the first parcel with no message. Fix: never adopt a definition whose `SourceFileName` is not ours; use `RasterImageDef.SuggestName(dict, imagePath)` (verified static, 2 params) for a unique key, or refuse with `IMAGE_IN_USE` when the `out=` file is already attached in this drawing.

**M2 — The relative `SourceFileName` branch almost certainly falls back to the absolute path on the first insert, and the harness cannot see it.**
`RasterInserter.cs:72-73`: the new definition is `Load()`ed **before** `dict.SetAt`, so it has no database and AutoCAD cannot resolve a bare `image.png` against the DWG folder (it searches the process CWD and support paths); `Load` at `:141` fails, `:148-152` stores the full path with a warning — the "path follows the DWG" property is lost exactly when it was designed for. The acceptance run cannot tell: `-HPGEOIMAGE` runs on the unsaved `Drawing1`, so `SourceNameFor` (`:116-134`) always returns the absolute path and the relative branch is never executed. Fix: set `def.SourceFileName = "image.png"` and `def.ActiveFileName = fullPath` (verified settable) and drop the retry, or `SetAt` + `AddNewlyCreatedDBObject` before `Load()`. Verify with the harness change in M4.

**M3 — Cache poisoning: any 200 response that is not an image, or a truncated cache file, is cached forever and every later run over that tile dies with a generic error.**
`TileFetcher.cs:111-114` (context file) accepts `200 + Length > 0` without a content-type or magic check; `:62` trusts any non-empty cache file; `ImageryPipeline.cs:71` → `TileStitcher.Decode` (`:37`) throws out of the pipeline → the command prints `lỗi — …` and logs `[ERR]`, nothing evicts the file. Scenario: hotel Wi-Fi captive portal answers HTML with 200 → 15 HTML "tiles" cached → after login the same parcel still fails until the user deletes `%LocalAppData%\HPGeo\tiles` by hand; same after acad.exe is killed mid-write (partial `.tile`). Fix (pipeline side, in scope): catch decode failures per tile in `Stitch`, evict the cache file and re-fetch once, and surface the rest as `TILE_FETCH_FAILED` naming the file; (fetcher side) write to `.tmp` + `File.Move`, and reject bodies whose first bytes are not JPEG/PNG.

**M4 — The P-IMG harness block has never run, and its `U` step is likely to undo `DELAY`, not `-HPGEOIMAGE`.**
`tools/acceptance.ps1:148-152`: `-HPGEOIMAGE` → `DELAY 6000` → `HPGEOINFO` (NoUndoMarker, transparent) → `_.U`. `U` steps back one command boundary and, for an operation that cannot be undone, "the command name is displayed but no action is performed" (Autodesk U help) — so `_.U` most likely prints `DELAY` and the RasterImage stays; check `:369` then fails as `0,0,1,1,1` for a reason unrelated to the undo group. Fix: put `_.U` immediately after the insert (`-HPGEOIMAGE` → `HPGEOINFO` → `_.U` → `HPGEOINFO`) and take the screenshot from a second insert that stays: `_.SAVEAS` first, then `-HPGEOIMAGE … out=$work\stored_hpgeo.png` (now the drawing is saved beside the PNG → exercises the relative branch of M2), `DELAY 6000`, `_.QSAVE`, `_.CLOSE`, `_.OPEN`, `HPGEOINFO` → expected counts `0,0,1,0,1`, plus a check that the reopened drawing's `HPGEOIMAGE` definition resolved (no `did not load by relative name` warning in `$hp`). Also assert what `U` undid from the text log (`(?m)^-?HPGEOIMAGE\s*$` right after `Command: _.U`). Then run it: the whole block is unverified, including PrintWindow while AutoCAD sits in `DELAY`.

**M5 — Non-resident DBObjects are not disposed on the failure paths.**
`RasterInserter.cs:72` (`new RasterImageDef`) and `:85` (`new RasterImage`): if `Load` throws (`:141/:151`), `SetAt` rejects the key (a PNG name with `=`/`;`/`<` is not a valid dictionary key), or `AppendEntity` throws, the transaction aborts but the wrapper is never disposed; AutoCAD's `DisposableWrapper` finalizer then deletes the native object on the finalizer thread — the classic acad.exe crash on exit or at the next GC. Fix: create-add-`AddNewlyCreatedDBObject` in a `try`, `Dispose()` the object in the `catch` when `ObjectId.IsNull`, rethrow.

### Low

**L1 — Orphan files and late refusals.** `RasterInserter.cs:48-50` writes the `.pgw` and `:155-163` checks the layer only after the download and the PNG (`ImageryPipeline.cs:86-89`); a locked `HPGEO-IMAGE`, an unwritable `out=` folder or a `Load` failure leaves `image.png` + `image.pgw` on disk with nothing inserted and a `[ERR]` line for a user condition. Check the layer and the output folder before fetching; delete the pair when `Insert` throws.

**L2 — Argument edge cases with misleading downstream codes.** `ImageArguments.cs:59-66,74`: `zoom=22` is silently clamped to 19 (`ImageryPipeline.cs:101`, no `RESOLUTION_LIMITED` because the target is nudged); `margin=1e400` parses as ∞ and surfaces as `OUTSIDE_VIETNAM (Lat NaN)`; `k0=0` parses and surfaces as `NO_CENTRAL_MERIDIAN`. Refuse `zoom > provider.MaxZoom`, require `IsFinite` on every number, `k0 > 0`.

**L3 — Per-run `HttpClient` never disposed.** `ImageryPipeline.cs:62` creates a `TileFetcher` (and `TileFetcher.cs:36` an `HttpClient`) per run and never disposes it; pooled sockets and the task's `AsyncWaitHandle` (`:118`) leak until GC. Keep one static fetcher or make it `IDisposable` and `using` it.

**L4 — HPGEOINFO now walks the whole tile cache on every call.** `HPGeoInfoCommand.cs:51` → `TileFetcher.CacheSizeBytes()` enumerates `%LocalAppData%\HPGeo\tiles` recursively (thousands of files after a few runs) inside the same `try`, so an I/O failure there kills the rest of the printout, including the log path the user needs. Wrap it, and consider a cheap directory count.

**L5 — Harness hygiene.** `acceptance.ps1:78/392`: `$screenshot` lives in `$evidence`, which is not cleaned → a stale file from a previous run passes the check; `:56` reuses an already-compiled `HPGeoNative` without `RECT`/`GetWindowRect`/`PrintWindow` if an older script version ran in the same console (the screenshot then fails inside the `try`); the block needs the network with no `-SkipImagery` switch. Delete `$screenshot` at start, version the type name, add the switch.

**L6 — Stored imagery settings are written but never read back.** `HPGeoImageScriptCommand.cs:104-116` stores the *achieved* resolution (0.293) as `imgRes`, and neither `imgRes` nor `imgMargin` is used as a default by the next run (`TargetResolution` falls back to 0.30), although the comment at `:103` says the drawing "remembers … for the next run". Either consult them (same chain as H2) or drop the comment; store the requested value, not the native one.

## Recommended order

1. H2 (small, data-correctness) and M1 + M2 + M5 together in `RasterInserter` (same 30 lines).
2. M4: reorder the script, run `tools/acceptance.ps1` once, keep the screenshot as evidence — this is what turns "Built" into "Verified".
3. H1 (Esc + deadline) and M3 (decode failures as refusals + eviction), each with a stub-handler test.
4. Lows alongside.

## Score: 6.5 / 10

The geometry and the AutoCAD API usage are right — orientation, warp-not-affine, transaction and undo hygiene, thread discipline — and the refusal matrix lines up with the harness regexes to the character. What holds the score down is that nothing after `SavePng` has executed in AutoCAD yet (the P-IMG block never ran, and its `U` step is aimed at the wrong command), the download can freeze AutoCAD for over an hour with no way out, the zone precedence that was the point of making `cm=` optional stops at `cm` and silently rewrites the drawing's k0/fe/fn, and two `RasterInserter` details (definition reuse, relative-name load before the def is resident) will bite the first time a script reuses `out=` or the drawing is saved beside its PNG.

## Follow-up 2026-09-18 (same session) — findings applied

- H2 → `ImageZoneResolver` (Core, tested): cm/k0/fe/fn arg → record → user → default value by value; unit arg → record → INSUNITS; resolved values printed + written back.
- H1 → `ImageryPipeline.Fetch`: CTS deadline `Clamp(tiles × 3 s, 60, 300)`, Escape via `HostApplicationServices.Current.UserBreak()` polled every 250 ms, `CANCELLED` / `TILE_FETCH_FAILED` refusals (never `[ERR]`).
- M1 → `RasterImageDef.SuggestName(dict, path)`; M2 → `SourceFileName` relative + `ActiveFileName` full before `Load()` (verified live: `stored_hpgeo.png (relative source)`, no fallback warning); M5 → non-resident `Dispose()` in `catch` when `ObjectId.IsNull`.
- M3 → `TileFetcher.LooksLikeImage` magic sniff on download and on cache read, `.tmp` + `File.Move`, poisoned file evicted; pinned by 3 tests.
- M4 → harness reordered (`-HPGEOIMAGE` → `HPGEOINFO` → `_.U` → `HPGEOINFO`; second insert after SAVEAS beside the DWG; counts `0,0,1,0,1`; `U` echo asserted); ran live: 49/49.
- L1 pre-flight layer + folder, PNG/PGW deleted when Insert throws; L2 zoom > MaxZoom / non-finite / k0 ≤ 0 refused; L3 shared `Lazy<TileFetcher>`, `IDisposable`; L4 cache walk wrapped; L5 stale screenshot removed, Add-Type guard on `PrintWindow`; L6 requested res stored, comment fixed.
- Found on the way: acad.exe is denied the network by the firewall rule `Autocad2026` (outbound Block) → harness prefetches the tiles from a dotnet process and prints the rule state; `SendCommand` blocks the harness for the whole script → moved to `Start-Job` so the screenshot can be taken during `DELAY`.
