# -*- coding: utf-8 -*-
"""Noi thu vien vncad voi AutoCAD dang chay.

Ba duong dan, chon theo hoan canh:

1. Qua AutoCAD MCP (khuyen dung khi lam viec trong phien Claude Code):
   dung `lisp_open`, `lisp_save_dwg`, `lisp_plot_pdf` de lay chuoi LISP,
   roi goi tool `system` voi operation `execute_lisp`.

2. Qua file IPC (thuan Python, khong can MCP): `send_command`.
   AutoCAD phai dang mo va da nap mcp_dispatch.lsp.

3. Chay ngam khong can mo AutoCAD: `write_script` tao file .scr cho accoreconsole.
"""

import json
import os
import time
import uuid

IPC_DIR = "C:/temp"


def _p(path):
    """AutoCAD chi hieu dau gach cheo xuoi."""
    return os.path.abspath(path).replace("\\", "/")


# ---------------------------------------------------------------------------
# 1. SINH LISP CHO AUTOCAD MCP
# ---------------------------------------------------------------------------
def lisp_open(dxf_path, zoom=True):
    """Mo file DXF trong AutoCAD dang chay va zoom vua man hinh."""
    z = '(vla-zoomextents (vlax-get-acad-object))' if zoom else ''
    return ('(vl-load-com)'
            '(vla-open (vla-get-documents (vlax-get-acad-object)) "%s")%s'
            '(princ "OPENED")' % (_p(dxf_path), z))


def lisp_save_dwg(dwg_path):
    """Luu ban ve dang mo thanh file DWG (dinh dang AutoCAD 2018)."""
    return ('(vl-load-com)'
            '(vla-SaveAs (vla-get-ActiveDocument (vlax-get-acad-object)) "%s" 60)'
            '(princ "SAVED")' % _p(dwg_path))


def lisp_open_and_convert(dxf_path, dwg_path):
    """Mo DXF roi luu ngay thanh DWG. Dung de doi ca bo ho so sang DWG.

    Truoc khi luu, doan LISP nha khoa file dich neu no dang mo trong AutoCAD:
      - tai lieu khac giu ten do: dong lai, khong luu;
      - chinh tai lieu dang chay LISP giu ten do (khong tu dong duoc): cho no
        "luu thanh" mot file giu cho trong IPC_DIR, ten cu duoc nha ra.
    Khong co buoc nay thi lan chay thu hai bao "Error saving the document".
    """
    return ('(vl-load-com)'
            '(setq acad (vlax-get-acad-object) docs (vla-get-documents acad)'
            ' dich "%s" hien (vla-get-ActiveDocument acad) ket "CONVERTED")'
            '%s'
            '(if (= ket "BUSY") (progn (vla-SaveAs hien "%s" 60)'
            ' (setq ket "CONVERTED")))'
            '(if (= ket "CONVERTED")'
            ' (progn (setq d (vla-open docs "%s"))'
            '  (vla-zoomextents acad) (vla-SaveAs d dich 60)))'
            '(princ ket)' % (_p(dwg_path), _LISP_DONG_FILE_TRUNG,
                             IPC_DIR + "/_giu_cho_ten_file.dwg", _p(dxf_path)))


# Duyet moi tai lieu dang mo, dong cai nao trung ten file dich.
# AutoCAD tra ten file bang dau gach NGUOC con ta luon dung gach XUOI, nen phai
# doi ve cung mot kieu roi moi so; so sanh bang chu HOA vi Windows khong phan
# biet hoa thuong trong duong dan.
_LISP_DONG_FILE_TRUNG = (
    '(vlax-for d docs'
    ' (setq ten (vl-catch-all-apply (function vla-get-FullName) (list d)))'
    ' (if (= (type ten) (quote STR))'
    '  (if (= (strcase (vl-string-translate "\\\\" "/" ten)) (strcase dich))'
    '   (if (equal d hien) (setq ket "BUSY") (vla-Close d :vlax-false)))))')


def lisp_plot_pdf(pdf_path, sheet="A3", layout="Model"):
    """In ban ve dang mo ra PDF theo kho giay.

    GIOI HAN DA KIEM CHUNG 10/09/2026: khong dung duoc qua AutoCAD MCP.
    Lenh in tac dong len TAI LIEU DANG HOAT DONG, ma doan LISP goi qua MCP
    luon chay trong ngu canh tai lieu da goi no; `vla-open` mo duoc file moi
    nhung khong doi duoc tai lieu hoat dong giua chung, va `vla-Activate`
    bao loi "Drawing is busy".

    Hai cach dung duoc:
      1. Nguoi dung tu mo file trong AutoCAD roi moi goi ham nay.
      2. Dung `render.save_pdf` ben Python, khong can AutoCAD. Day la cach
         dang dung trong `build_ho_so.py` va da chay on dinh.
    """
    size = {"A3": "ISO_full_bleed_A3_(420.00_x_297.00_MM)",
            "A2": "ISO_full_bleed_A2_(594.00_x_420.00_MM)",
            "A1": "ISO_full_bleed_A1_(841.00_x_594.00_MM)"}[sheet]
    return ('(command "_.-PLOT" "Y" "%s" "DWG To PDF.pc3" "%s" "M" "L" "N" '
            '"E" "F" "C" "Y" "monochrome.ctb" "Y" "N" "N" "N" "%s" "N" "Y")'
            '(princ "PLOTTED")' % (layout, size, _p(pdf_path)))


def lisp_convert_batch(names, dxf_dir, dwg_dir):
    """Doi ca bo DXF sang DWG trong 1 lan goi, roi dong bot ban ve de khong khoa file.

    Doc cuoi cung giu lai lam doc dang hoat dong, vi AutoCAD khong cho dong
    chinh ban ve dang hoat dong tu trong LISP.
    """
    body = " ".join('"%s"' % n for n in names)
    return ('(vl-load-com)(setq app (vlax-get-acad-object))'
            '(setq docs (vla-get-documents app))(setq opened nil)'
            '(foreach n (list %s)'
            '(setq d (vla-open docs (strcat "%s/" n ".dxf")))'
            '(vla-zoomextents app)'
            '(vla-SaveAs d (strcat "%s/" n ".dwg") 60)'
            '(setq opened (cons d opened)))'
            '(foreach d (cdr opened) (vl-catch-all-apply (function vla-Close) '
            '(list d :vlax-false)))'
            '(princ (strcat "CONVERTED " (itoa (length opened))))'
            % (body, _p(dxf_dir), _p(dwg_dir)))


def lisp_purge_and_audit():
    """Don rac va sua loi ban ve truoc khi ban giao."""
    return '(command "_.-PURGE" "A" "*" "N")(command "_.AUDIT" "Y")(princ "CLEANED")'


# ---------------------------------------------------------------------------
# 2. FILE IPC (thuan Python)
# ---------------------------------------------------------------------------
def send_command(command, params=None, timeout=30.0, ipc_dir=IPC_DIR):
    """Gui 1 lenh cho AutoCAD qua thu muc IPC va cho ket qua.

    AutoCAD phai dang mo va da nap mcp_dispatch.lsp. Neu AutoCAD khong tu poll,
    phai go `(c:mcp-dispatch)` trong dong lenh AutoCAD de no doc lenh.
    """
    rid = uuid.uuid4().hex[:12]
    payload = {"request_id": rid, "command": command}
    payload.update(params or {})
    cmd_path = os.path.join(ipc_dir, "autocad_mcp_cmd_%s.json" % rid)
    res_path = os.path.join(ipc_dir, "autocad_mcp_result_%s.json" % rid)
    os.makedirs(ipc_dir, exist_ok=True)
    with open(cmd_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False)

    deadline = time.time() + timeout
    while time.time() < deadline:
        if os.path.exists(res_path):
            with open(res_path, encoding="utf-8", errors="replace") as f:
                data = json.load(f)
            os.remove(res_path)
            return data
        time.sleep(0.25)
    raise TimeoutError("AutoCAD khong tra loi sau %.0fs. Kiem tra AutoCAD da mo "
                       "va da nap mcp_dispatch.lsp chua." % timeout)


# ---------------------------------------------------------------------------
# 3. CHAY NGAM BANG accoreconsole
# ---------------------------------------------------------------------------
def write_script(scr_path, dxf_path, dwg_path=None, pdf_path=None):
    """Tao file .scr de chay `accoreconsole.exe /i <dxf> /s <scr>`."""
    lines = ["FILEDIA", "0", "_.ZOOM", "E"]
    if dwg_path:
        lines += ["_.SAVEAS", "2018", _p(dwg_path)]
    if pdf_path:
        lines += ["_.-EXPORT", "P", _p(pdf_path)]
    lines += ["_.QUIT"]
    with open(scr_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    return scr_path


def convert_batch(dxf_paths, out_dir=None):
    """Sinh cap (duong dan DXF, duong dan DWG) cho ca bo ho so."""
    pairs = []
    for p in dxf_paths:
        d = out_dir or os.path.dirname(p)
        name = os.path.splitext(os.path.basename(p))[0] + ".dwg"
        pairs.append((_p(p), _p(os.path.join(d, name))))
    return pairs
