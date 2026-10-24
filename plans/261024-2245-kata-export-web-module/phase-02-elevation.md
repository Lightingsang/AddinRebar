# Phase 2 — Mặt đứng Kata trong trang (SVG)

**Status:** Planned · **Type:** AFK

- Dùng `KataElevationBuilder` + `KataElevationViewport` (HPRebar.Core) tạo model mặt đứng → DTO `KataElevation` (đường, kích thước, nhãn, vùng nhịp) qua `kata.buildSheet`.
- React vẽ SVG: pan/zoom, focus nhịp (`KataViewFocus`), chú thích kích thước (thay `View/Controls/KataCad*` WPF), theme sáng/tối theo token.
- Không port code canvas WPF (WPF-only); chỉ port logic còn nằm trong ViewModel nếu có → đưa vào Domain toolkit hoặc gọi Core.
- Cổng: vitest (render từ fixture), ảnh CDP trong Revit so với ảnh tool WPF cùng dầm (kiểm bằng mắt, ghi report).
