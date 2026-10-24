import { BridgeError } from './bridgeClient';
import type { ErrorCode } from './protocol';

// The host answers with English text for its log; the page shows a Vietnamese sentence per code and keeps the host's
// text only where it carries the specifics (bad input).
const sentences: Record<ErrorCode, string> = {
  BAD_REQUEST: 'Yêu cầu không hợp lệ',
  UNKNOWN_COMMAND: 'Add-in không hỗ trợ lệnh này (cần cập nhật add-in)',
  NO_DOCUMENT: 'Chưa mở tài liệu Revit nào',
  BUSY: 'Revit đang thống kê, vui lòng đợi',
  TIMEOUT: 'Revit không phản hồi kịp — đóng hộp thoại đang mở trong Revit rồi thử lại',
  CANCELLED: 'Yêu cầu đã bị hủy',
  REVIT_API_ERROR: 'Revit từ chối thao tác',
  INTERNAL: 'Lỗi nội bộ của add-in (xem log %LocalAppData%\\HPBimToolkit\\logs)',
};

export function errorText(error: unknown): string {
  if (!(error instanceof BridgeError)) {
    return error instanceof Error ? error.message : String(error);
  }

  const sentence = sentences[error.code] ?? error.message;
  return error.code === 'BAD_REQUEST' || error.code === 'REVIT_API_ERROR' ? `${sentence}: ${error.message}` : sentence;
}
