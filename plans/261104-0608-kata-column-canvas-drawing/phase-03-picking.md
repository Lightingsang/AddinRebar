# Phase 3 — Bar picking + highlight + chip

Status: pending.
- Hit test in screen px (segment distance ≤ 6 px elevation; circle radius + 4 px sections).
- Hover → highlight; click → pin; Esc / empty click clears. Same `Number` highlighted in elevation + every section.
- Chip: số hiệu, Ø, chiều dài (mm, from `LengthMm`), nhóm A/B, lớp, tầng; VI/EN strings in `i18n.ts`.
- vitest: hit test, highlight shapes, chip text.
