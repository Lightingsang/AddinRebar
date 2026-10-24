# Phase 6 — Cài đặt Kata

**Status:** Planned · **Type:** AFK

- Lệnh `kata.settings.get` / `kata.settings.save` đọc/ghi `%AppData%\HPRebar\KataSettings.json` qua `KataSettingsJson` + `KataSettingsSanitizer` (HPRebar.Core) — cùng file với tool WPF, ghi nguyên tử (temp + rename), giữ khoá lạ.
- Trang 3 tab như WPF: Detail thép, Thông số đặc thù (bảng nối thép, bảng khối lượng), Thép mặc định nút; validate bằng sanitizer, hiện lỗi theo trường.
- Cổng: test round trip (đọc file WPF ghi → lưu → WPF đọc lại giống), live: đổi một thông số trong trang → tool WPF thấy giá trị mới.
