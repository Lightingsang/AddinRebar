# Phase 5 — Web-only features (scope set by user)

**User chose (2026-10-25):** hover + tooltip, smooth zoom, minimap + find bar, export SVG/PNG/print PDF, measure + layer panel — all in MVP; 3D view is phase 7, cross sections phase 4. Not chosen: side-by-side pinned sections, schedule hover link.

Candidates (WPF canvas has none of these):

| Feature | What | Cost |
|---|---|---|
| ★ Hover + tooltip | bar/stirrup info under the cursor | in phase 4 |
| ★ Smooth animated zoom | eased frame-span / frame-all, inertia pan | S |
| ★ Minimap | whole run overview, drag the view rectangle | S |
| ★ Export | SVG / PNG (2×, 4×) of the current view or whole run, print to PDF via browser | S |
| ★ Find bar | type a bar number → highlight + zoom to every occurrence | S |
| Measure | two clicks snapping to vertices, shows Δx/Δz/length mm | M |
| Layer panel | per-pen visibility, isolate a bar layer | S |
| Side-by-side sections | several sections pinned next to the elevation | M |
| Bar schedule hover link | hover a row in a schedule table ↔ highlight on drawing | M (needs schedule UI) |

## Success
Each chosen feature has a vitest + one live check; none adds a bridge round trip during zoom/pan.
