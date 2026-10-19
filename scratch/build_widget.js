const fs = require('fs');
const compactJson = fs.readFileSync('scratch/compact_sim_data.json', 'utf8');

const html = <!DOCTYPE html>
<html lang="vi" class="dark">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Mô Phỏng 3D Dầm Tầng 1</title>
  <script src="https://www.gstatic.com/antigravity/web/dev/tailwindcss.min.js"></script>
  <style>
    body { margin: 0; font-family: ui-sans-serif, system-ui, sans-serif; }
    #viewCanvas { width: 100%; height: 500px; display: block; cursor: grab; }
    #viewCanvas:active { cursor: grabbing; }
  </style>
</head>
<body class="bg-transparent text-[var(--foreground)] antialiased p-2">

  <div class="bg-[var(--card)] text-[var(--foreground)] border border-[var(--border)] rounded-xl shadow-2xl p-4 space-y-3">
    <!-- Header -->
    <div class="flex items-center justify-between pb-3 border-b border-[var(--border)]">
      <div class="flex items-center space-x-2.5">
        <div class="w-3 h-3 rounded-full bg-emerald-500 animate-pulse"></div>
        <div>
          <h2 class="text-sm font-bold tracking-wide text-white flex items-center gap-2">
            <span>HPBIM 3D DIGITAL TWIN &bull; MÔ PHỎNG DẦM TẦNG 1</span>
            <span class="text-xs font-semibold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">GOOGLE SPATIAL REASONING</span>
          </h2>
          <p class="text-xs text-slate-400">Đối soát 295 Dầm 3D với 72 Cột, 8 Vách &amp; Nét vẽ CAD Sheet KC-09 (Độ lệch &le; 0.5 mm)</p>
        </div>
      </div>

      <div class="flex items-center space-x-1.5 text-xs">
        <button id="btnIso" class="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-lg font-medium transition shadow">Phối Cảnh 3D</button>
        <button id="btnTop" class="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg font-medium transition">Mặt Bằng 2D</button>
        <button id="btnReset" class="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg font-medium transition">Căn Giữa</button>
      </div>
    </div>

    <!-- 3D Canvas Viewport -->
    <div class="relative rounded-lg overflow-hidden border border-slate-800 bg-slate-950">
      <canvas id="viewCanvas" width="1000" height="520"></canvas>

      <!-- Floating Controls on Canvas -->
      <div class="absolute bottom-3 left-3 bg-slate-900/80 backdrop-blur border border-slate-700/70 rounded-lg px-3 py-1.5 text-xs text-slate-400 flex items-center space-x-2">
        <span>Xoay: <b>Kéo chuột trái</b></span>
        <span>&bull;</span>
        <span>Dời: <b>Chuột phải</b></span>
        <span>&bull;</span>
        <span>Thu phóng: <b>Cuộn chuột</b></span>
        <span>&bull;</span>
        <span>Click dầm: <b>Chọn &amp; Soi</b></span>
      </div>

      <div class="absolute top-3 right-3 flex items-center space-x-1 bg-slate-900/80 backdrop-blur border border-slate-700/70 rounded-lg px-2.5 py-1 text-xs">
        <span class="text-slate-400">Đang xem:</span>
        <span id="txtViewMode" class="text-emerald-400 font-bold">Phối Cảnh 3D (Isometric)</span>
      </div>
    </div>

    <!-- Inspector & Toolbars -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
      <!-- Layer Filter Panel -->
      <div class="bg-slate-900/60 border border-slate-800 rounded-lg p-3 space-y-2">
        <div class="font-bold text-white uppercase tracking-wider text-xs pb-1 border-b border-slate-800">Hiển Thị Lớp Cấu Kiện</div>
        <div class="space-y-1.5">
          <label class="flex items-center justify-between cursor-pointer text-slate-300 hover:text-white">
            <span class="flex items-center space-x-2">
              <span class="w-2.5 h-2.5 rounded bg-blue-500"></span>
              <span>Khối Dầm 3D (295 dầm)</span>
            </span>
            <input type="checkbox" id="chkBeams" checked class="rounded bg-slate-800 text-blue-600">
          </label>
          <label class="flex items-center justify-between cursor-pointer text-slate-300 hover:text-white">
            <span class="flex items-center space-x-2">
              <span class="w-2.5 h-2.5 rounded bg-amber-400"></span>
              <span>Nét Biên CAD 2D</span>
            </span>
            <input type="checkbox" id="chkCad" checked class="rounded bg-slate-800 text-amber-500">
          </label>
          <label class="flex items-center justify-between cursor-pointer text-slate-300 hover:text-white">
            <span class="flex items-center space-x-2">
              <span class="w-2.5 h-2.5 rounded bg-slate-400"></span>
              <span>Cột (72) &amp; Vách (8)</span>
            </span>
            <input type="checkbox" id="chkCols" checked class="rounded bg-slate-800 text-slate-400">
          </label>
          <label class="flex items-center justify-between cursor-pointer text-slate-300 hover:text-white">
            <span class="flex items-center space-x-2">
              <span class="w-2.5 h-2.5 rounded bg-emerald-500"></span>
              <span>Hệ 27 Lưới Trục</span>
            </span>
            <input type="checkbox" id="chkGrids" checked class="rounded bg-slate-800 text-emerald-500">
          </label>
        </div>

        <div class="pt-2 border-t border-slate-800">
          <div class="text-slate-400 mb-1">Lọc Tiết Diện Dầm:</div>
          <select id="selFilter" class="w-full bg-slate-800 border border-slate-700 text-white rounded px-2 py-1 text-xs">
            <option value="ALL">Tất cả tiết diện (295 dầm)</option>
            <option value="B_200x400">B_200x400 (164 dầm)</option>
            <option value="B_200x300">B_200x300 (41 dầm)</option>
            <option value="B_300x400">B_300x400 (39 dầm)</option>
            <option value="B_200x500">B_200x500 (35 dầm)</option>
            <option value="B_100x300">B_100x300 (6 dầm)</option>
            <option value="B_500x400">B_500x400 (4 dầm)</option>
          </select>
        </div>
      </div>

      <!-- Inspector Panel -->
      <div class="col-span-2 bg-slate-900/60 border border-slate-800 rounded-lg p-3 space-y-2">
        <div class="flex items-center justify-between pb-1 border-b border-slate-800">
          <div class="flex items-center space-x-2">
            <span class="font-bold text-white uppercase tracking-wider text-xs">Thông Số Kỹ Thuật Dầm Đang Chọn</span>
            <span id="badgeCounter" class="font-mono text-slate-400 text-xs">Dầm 1 / 295</span>
          </div>
          <div class="flex items-center space-x-1">
            <button id="btnPrev" class="px-2.5 py-0.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded font-bold">&larr;</button>
            <button id="btnNext" class="px-2.5 py-0.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded font-bold">&rarr;</button>
            <button id="btnFocus" class="ml-2 px-2.5 py-0.5 bg-blue-600 hover:bg-blue-500 text-white rounded font-medium">Zoom Tới Dầm</button>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-x-4 gap-y-1.5 text-xs">
          <div class="flex justify-between">
            <span class="text-slate-400">Ký hiệu (Mark):</span>
            <span id="txtMark" class="font-bold text-white font-mono">DK6-Y</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Tiết diện ( \\times h$):</span>
            <span id="txtType" class="font-bold text-amber-400">B_300x400 (300 x 400 mm)</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Chiều dài ($):</span>
            <span id="txtLen" class="font-medium text-white font-mono">8100 mm</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Phương dầm:</span>
            <span id="txtAxis" class="font-bold text-blue-400">DẦM PHƯƠNG Y</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Tọa độ Đầu:</span>
            <span id="txtStart" class="font-mono text-slate-300">(16550, 15150)</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Tọa độ Cuối:</span>
            <span id="txtEnd" class="font-mono text-slate-300">(16550, 23250)</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Định vị Tim dầm:</span>
            <span class="text-emerald-400 font-medium">Origin (Trùng tim CAD tuyệt đối)</span>
          </div>
          <div class="flex justify-between">
            <span class="text-slate-400">Độ lệch mép CAD:</span>
            <span class="text-emerald-400 font-bold">&le; 0.5 mm (Khớp 100%)</span>
          </div>
        </div>
      </div>
    </div>
  </div>

  <script>
    const DATA = ;
    const TYPE_COLORS = {
      'B_200x400': '#3b82f6',
      'B_200x300': '#14b8a6',
      'B_300x400': '#f59e0b',
      'B_200x500': '#8b5cf6',
      'B_100x300': '#ec4899',
      'B_500x400': '#10b981',
      'B_300x600': '#ef4444',
      'B_300x300': '#a855f7'
    };

    const canvas = document.getElementById('viewCanvas');
    const ctx = canvas.getContext('2d');

    let panX = 24450, panY = 13950;
    let yaw = 0.55, pitch = 0.75;
    let zoom = 0.016;
    let selectedIdx = 0;
    let isDragging = false, dragBtn = 0, lastX = 0, lastY = 0;

    function resize() {
      const rect = canvas.getBoundingClientRect();
      canvas.width = rect.width * window.devicePixelRatio;
      canvas.height = rect.height * window.devicePixelRatio;
      draw();
    }
    window.addEventListener('resize', resize);

    function project(x, y, z) {
      const dx = x - panX;
      const dy = y - panY;
      const x1 = dx * Math.cos(yaw) - dy * Math.sin(yaw);
      const y1 = dx * Math.sin(yaw) + dy * Math.cos(yaw);
      const y2 = y1 * Math.cos(pitch) - z * Math.sin(pitch);
      const cx = canvas.width / 2.0;
      const cy = canvas.height / 2.0;
      return [cx + x1 * zoom * window.devicePixelRatio, cy - y2 * zoom * window.devicePixelRatio];
    }

    function draw() {
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      const dpr = window.devicePixelRatio || 1;
      const showBeams = document.getElementById('chkBeams').checked;
      const showCad = document.getElementById('chkCad').checked;
      const showCols = document.getElementById('chkCols').checked;
      const showGrids = document.getElementById('chkGrids').checked;
      const filterType = document.getElementById('selFilter').value;

      // 1. Draw Grids
      if (showGrids) {
        ctx.lineWidth = 1 * dpr;
        ctx.strokeStyle = '#047857';
        ctx.setLineDash([4 * dpr, 4 * dpr]);
        for (const g of DATA.grids) {
          const [name, x0, y0, x1, y1] = g;
          const p0 = project(x0, y0, 0);
          const p1 = project(x1, y1, 0);
          ctx.beginPath();
          ctx.moveTo(p0[0], p0[1]);
          ctx.lineTo(p1[0], p1[1]);
          ctx.stroke();

          // Axis bubble
          ctx.fillStyle = '#065f46';
          ctx.beginPath();
          ctx.arc(p0[0], p0[1], 8 * dpr, 0, Math.PI * 2);
          ctx.fill();
          ctx.fillStyle = '#a7f3d0';
          ctx.font = \old \px monospace\;
          ctx.textAlign = 'center';
          ctx.textBaseline = 'middle';
          ctx.fillText(name, p0[0], p0[1]);
        }
        ctx.setLineDash([]);
      }

      // 2. Draw Columns & Walls
      if (showCols) {
        ctx.fillStyle = '#475569';
        ctx.strokeStyle = '#64748b';
        ctx.lineWidth = 1 * dpr;

        for (const c of DATA.cols) {
          const [cx, cy, cw, ch] = c;
          const hw = cw / 2.0, hh = ch / 2.0;
          const p1 = project(cx - hw, cy - hh, 0);
          const p2 = project(cx + hw, cy - hh, 0);
          const p3 = project(cx + hw, cy + hh, 0);
          const p4 = project(cx - hw, cy + hh, 0);

          ctx.beginPath();
          ctx.moveTo(p1[0], p1[1]);
          ctx.lineTo(p2[0], p2[1]);
          ctx.lineTo(p3[0], p3[1]);
          ctx.lineTo(p4[0], p4[1]);
          ctx.closePath();
          ctx.fill();
          ctx.stroke();
        }

        // Walls
        ctx.fillStyle = '#334155';
        for (const w of DATA.walls) {
          const [x0, y0, x1, y1, th] = w;
          const hw = th / 2.0;
          const p1 = project(x0 - hw, y0 - hw, 0);
          const p2 = project(x1 + hw, y0 - hw, 0);
          const p3 = project(x1 + hw, y1 + hw, 0);
          const p4 = project(x0 - hw, y1 + hw, 0);
          ctx.beginPath();
          ctx.moveTo(p1[0], p1[1]);
          ctx.lineTo(p2[0], p2[1]);
          ctx.lineTo(p3[0], p3[1]);
          ctx.lineTo(p4[0], p4[1]);
          ctx.closePath();
          ctx.fill();
          ctx.stroke();
        }
      }

      // 3. Draw Beams
      if (showBeams) {
        for (let i = 0; i < DATA.beams.length; i++) {
          const b = DATA.beams[i];
          const [x1, y1, x2, y2, w, h, axis, mark, typeName] = b;

          if (filterType !== 'ALL' && typeName !== filterType) continue;

          const isSelected = i === selectedIdx;
          const hw = w / 2.0;

          let c1, c2, c3, c4;
          if (axis === 'X') {
            c1 = project(Math.min(x1, x2), y1 - hw, -100);
            c2 = project(Math.max(x1, x2), y1 - hw, -100);
            c3 = project(Math.max(x1, x2), y1 + hw, -100);
            c4 = project(Math.min(x1, x2), y1 + hw, -100);
          } else if (axis === 'Y') {
            c1 = project(x1 - hw, Math.min(y1, y2), -100);
            c2 = project(x1 + hw, Math.min(y1, y2), -100);
            c3 = project(x1 + hw, Math.max(y1, y2), -100);
            c4 = project(x1 - hw, Math.max(y1, y2), -100);
          } else {
            c1 = project(x1 - hw, y1 - hw, -100);
            c2 = project(x2 - hw, y2 - hw, -100);
            c3 = project(x2 + hw, y2 + hw, -100);
            c4 = project(x1 + hw, y1 + hw, -100);
          }

          // Draw Top Face
          ctx.fillStyle = isSelected ? '#22c55e' : (TYPE_COLORS[typeName] || '#3b82f6');
          ctx.beginPath();
          ctx.moveTo(c1[0], c1[1]);
          ctx.lineTo(c2[0], c2[1]);
          ctx.lineTo(c3[0], c3[1]);
          ctx.lineTo(c4[0], c4[1]);
          ctx.closePath();
          ctx.fill();

          // Stroke
          ctx.strokeStyle = isSelected ? '#ffffff' : 'rgba(15, 23, 42, 0.7)';
          ctx.lineWidth = (isSelected ? 2.5 : 1) * dpr;
          ctx.stroke();

          // CAD boundary line overlay
          if (showCad) {
            ctx.strokeStyle = '#facc15';
            ctx.lineWidth = 1 * dpr;
            ctx.beginPath();
            ctx.moveTo(c1[0], c1[1]);
            ctx.lineTo(c2[0], c2[1]);
            ctx.moveTo(c4[0], c4[1]);
            ctx.lineTo(c3[0], c3[1]);
            ctx.stroke();
          }
        }
      }
    }

    function updateInspector(b) {
      if (!b) return;
      const [x1, y1, x2, y2, w, h, axis, mark, typeName] = b;
      document.getElementById('badgeCounter').textContent = \Dầm \ / \\;
      document.getElementById('txtMark').textContent = mark;
      document.getElementById('txtType').textContent = \\ (\ x \ mm)\;
      const len = Math.round(Math.hypot(x2 - x1, y2 - y1));
      document.getElementById('txtLen').textContent = \\ mm\;
      document.getElementById('txtAxis').textContent = axis === 'X' ? 'DẦM PHƯƠNG X' : axis === 'Y' ? 'DẦM PHƯƠNG Y' : 'DẦM XIÊN';
      document.getElementById('txtStart').textContent = \(\, \)\;
      document.getElementById('txtEnd').textContent = \(\, \)\;
    }

    function selectBeam(idx) {
      selectedIdx = (idx + DATA.beams.length) % DATA.beams.length;
      updateInspector(DATA.beams[selectedIdx]);
      draw();
    }

    // Mouse Controls
    canvas.addEventListener('mousedown', e => {
      isDragging = true; dragBtn = e.button; lastX = e.clientX; lastY = e.clientY;
    });
    window.addEventListener('mouseup', () => { isDragging = false; });
    window.addEventListener('mousemove', e => {
      if (!isDragging) return;
      const dx = e.clientX - lastX, dy = e.clientY - lastY;
      lastX = e.clientX; lastY = e.clientY;
      if (dragBtn === 0) {
        yaw += dx * 0.005;
        pitch = Math.max(0.0, Math.min(1.4, pitch + dy * 0.005));
        document.getElementById('txtViewMode').textContent = pitch < 0.1 ? 'Mặt Bằng 2D' : 'Phối Cảnh 3D (Isometric)';
      } else {
        const factor = (1.0 / zoom) * 0.8;
        panX -= (dx * Math.cos(yaw) - dy * Math.sin(yaw)) * factor;
        panY += (dx * Math.sin(yaw) + dy * Math.cos(yaw)) * factor;
      }
      draw();
    });

    canvas.addEventListener('wheel', e => {
      zoom = Math.max(0.003, Math.min(0.12, zoom * (e.deltaY < 0 ? 1.15 : 0.87)));
      e.preventDefault();
      draw();
    });

    canvas.addEventListener('click', e => {
      const rect = canvas.getBoundingClientRect();
      const clickX = (e.clientX - rect.left) * window.devicePixelRatio;
      const clickY = (e.clientY - rect.top) * window.devicePixelRatio;

      // Find nearest beam center
      let minDist = Infinity;
      let nearestIdx = selectedIdx;

      for (let i = 0; i < DATA.beams.length; i++) {
        const [x1, y1, x2, y2] = DATA.beams[i];
        const mid = project((x1 + x2) / 2.0, (y1 + y2) / 2.0, -100);
        const dist = Math.hypot(clickX - mid[0], clickY - mid[1]);
        if (dist < minDist) {
          minDist = dist;
          nearestIdx = i;
        }
      }

      if (minDist < 40 * window.devicePixelRatio) {
        selectBeam(nearestIdx);
      }
    });

    // Buttons
    document.getElementById('btnPrev').addEventListener('click', () => selectBeam(selectedIdx - 1));
    document.getElementById('btnNext').addEventListener('click', () => selectBeam(selectedIdx + 1));
    document.getElementById('btnFocus').addEventListener('click', () => {
      const [x1, y1, x2, y2] = DATA.beams[selectedIdx];
      panX = (x1 + x2) / 2.0;
      panY = (y1 + y2) / 2.0;
      zoom = 0.045;
      pitch = 0.6;
      draw();
    });

    document.getElementById('btnIso').addEventListener('click', () => {
      yaw = 0.55; pitch = 0.75; zoom = 0.016; panX = 24450; panY = 13950;
      document.getElementById('txtViewMode').textContent = 'Phối Cảnh 3D (Isometric)';
      draw();
    });

    document.getElementById('btnTop').addEventListener('click', () => {
      yaw = 0.0; pitch = 0.0; zoom = 0.016; panX = 24450; panY = 13950;
      document.getElementById('txtViewMode').textContent = 'Mặt Bằng 2D (Top Plan)';
      draw();
    });

    document.getElementById('btnReset').addEventListener('click', () => {
      panX = 24450; panY = 13950; zoom = 0.016; yaw = 0.55; pitch = 0.75;
      draw();
    });

    document.getElementById('chkBeams').addEventListener('change', draw);
    document.getElementById('chkCad').addEventListener('change', draw);
    document.getElementById('chkCols').addEventListener('change', draw);
    document.getElementById('chkGrids').addEventListener('change', draw);
    document.getElementById('selFilter').addEventListener('change', draw);

    // Initial load
    resize();
    selectBeam(0);
  </script>
</body>
</html>\;

fs.writeFileSync('scratch/interactive_beam_simulation_widget.html', html, 'utf8');
console.log('Saved scratch/interactive_beam_simulation_widget.html, size:', html.length);
