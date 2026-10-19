using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Controls
{
    /// <summary>
    /// Live 2D CAD-style vector preview canvas for continuous beam elevation and cross-section cut.
    /// </summary>
    public sealed class KataBeamPreviewCanvas : Canvas
    {
        private static readonly Brush BgBrush = new SolidColorBrush(Color.FromRgb(20, 24, 32));
        private static readonly Brush ConcreteBrush = new SolidColorBrush(Color.FromRgb(220, 225, 235));
        private static readonly Brush SupportBrush = new SolidColorBrush(Color.FromRgb(110, 120, 140));
        private static readonly Brush MainRebarBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        private static readonly Brush StirrupBrush = new SolidColorBrush(Color.FromRgb(0, 220, 255));
        private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(110, 240, 140));

        private BeamDetailingViewModel? _vm;

        public KataBeamPreviewCanvas()
        {
            Background = BgBrush;
            ClipToBounds = true;
            DataContextChanged += OnCanvasDataContextChanged;
            SizeChanged += (_, _) => Redraw();
        }

        private void OnCanvasDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
            {
                _vm.PropertyChanged -= OnVmPropertyChanged;
                _vm.Spans.CollectionChanged -= OnSpansCollectionChanged;
                foreach (var span in _vm.Spans) span.PropertyChanged -= OnVmPropertyChanged;
            }

            _vm = e.NewValue as BeamDetailingViewModel;
            if (_vm != null)
            {
                _vm.PropertyChanged += OnVmPropertyChanged;
                _vm.Spans.CollectionChanged += OnSpansCollectionChanged;
                foreach (var span in _vm.Spans) span.PropertyChanged += OnVmPropertyChanged;
            }
            Redraw();
        }

        private void OnSpansCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                    if (item is INotifyPropertyChanged npc) npc.PropertyChanged -= OnVmPropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                    if (item is INotifyPropertyChanged npc) npc.PropertyChanged += OnVmPropertyChanged;
            }
            Redraw();
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e) => Redraw();

        public void Redraw()
        {
            Children.Clear();
            double w = ActualWidth > 20 ? ActualWidth : 800;
            double h = ActualHeight > 20 ? ActualHeight : 120;
            if (_vm == null || _vm.Spans.Count == 0) return;

            double elevAreaW = w * 0.72;
            DrawElevation(elevAreaW, h);
            DrawCrossSection(elevAreaW + 16, w - elevAreaW - 24, h);
        }

        private void DrawElevation(double areaW, double areaH)
        {
            if (_vm == null) return;
            int n = _vm.Spans.Count;
            double padX = 24;
            double usableW = Math.Max(100, areaW - (padX * 2));
            double spanW = usableW / n;
            double topY = 26;
            double beamH = Math.Min(42, Math.Max(26, areaH * 0.36));
            double botY = topY + beamH;

            AddLabel(padX, 6, $"MẶT CẮT DỌC DẦM {_vm.BeamMark} ({_vm.StoryName}) — {n} NHỊP", 11);

            for (int i = 0; i < n; i++)
            {
                var s = _vm.Spans[i];
                double x1 = padX + (i * spanW);
                double x2 = x1 + spanW;

                AddRect(x1, botY, 10, 18, SupportBrush, 1.0);
                if (i == n - 1) AddRect(x2 - 10, botY, 10, 18, SupportBrush, 1.0);
                AddRect(x1, topY, spanW, beamH, ConcreteBrush, 1.4);

                DrawStirrupZoneTicks(x1 + 10, x2 - 10, topY + 4, botY - 4);
                AddLabel(x1 + (spanW * 0.25), botY + 4, $"{s.SpanName}: Ln={s.ClearLengthMm:F0} ({s.WidthMm:F0}x{s.HeightMm:F0})", 9.5);
            }

            double barX1 = padX + 4;
            double barX2 = padX + usableW - 4;
            AddHookedBar(barX1, barX2, topY + 5, hookDown: true, MainRebarBrush);
            AddHookedBar(barX1, barX2, botY - 5, hookDown: false, MainRebarBrush);
            if (_vm.HasSideBars)
            {
                AddLine(barX1, (topY + botY) / 2.0, barX2, (topY + botY) / 2.0, StirrupBrush, 1.0);
            }
        }

        private void DrawStirrupZoneTicks(double xStart, double xEnd, double yTop, double yBot)
        {
            double len = xEnd - xStart;
            if (len <= 20) return;
            double zLen = len * 0.25;
            for (double x = xStart + 4; x <= xEnd - 4; x += (x < xStart + zLen || x > xEnd - zLen) ? 6 : 12)
            {
                AddLine(x, yTop, x, yBot, StirrupBrush, 0.9);
            }
        }

        private void DrawCrossSection(double startX, double secW, double areaH)
        {
            if (_vm == null || _vm.Spans.Count == 0) return;
            var s0 = _vm.Spans[0];
            double boxW = Math.Min(64, Math.Max(36, secW * 0.34));
            double boxH = Math.Min(72, Math.Max(44, areaH * 0.58));
            double bx = startX + 8;
            double by = 24;

            AddLabel(startX, 6, $"MẶT CẮT 1-1 ({s0.WidthMm:F0}x{s0.HeightMm:F0})", 10.5);
            AddRect(bx, by, boxW, boxH, ConcreteBrush, 1.4);
            AddRect(bx + 5, by + 5, boxW - 10, boxH - 10, StirrupBrush, 1.1);

            DrawBarRow(bx + 8, bx + boxW - 8, by + 8, Math.Max(2, _vm.TopContinuousCount));
            DrawBarRow(bx + 8, bx + boxW - 8, by + boxH - 8, Math.Max(2, _vm.BotContinuousCount));
            if (_vm.HasSideBars)
            {
                AddDot(bx + 8, by + (boxH / 2.0));
                AddDot(bx + boxW - 8, by + (boxH / 2.0));
            }

            double tx = bx + boxW + 10;
            AddLabel(tx, by + 2, $"Trên: {_vm.TopContinuousCount}Ø{_vm.TopContinuousDiameter}", 9.5);
            AddLabel(tx, by + 20, $"Dưới: {_vm.BotContinuousCount}Ø{_vm.BotContinuousDiameter}", 9.5);
            AddLabel(tx, by + 38, $"Đai: Ø{_vm.StirrupDiameter}a{_vm.DenseSpacing:F0}/{_vm.SparseSpacing:F0}", 9.5);
            if (_vm.HasSideBars)
                AddLabel(tx, by + 56, $"Giá: {_vm.SideBarCount}Ø{_vm.SideBarDiameter}", 9.5);
        }

        private void DrawBarRow(double x1, double x2, double y, int count)
        {
            int n = Math.Min(6, Math.Max(2, count));
            for (int i = 0; i < n; i++)
            {
                double x = x1 + ((x2 - x1) * i / (n - 1));
                AddDot(x, y);
            }
        }

        private void AddHookedBar(double x1, double x2, double y, bool hookDown, Brush brush)
        {
            double hy = hookDown ? y + 10 : y - 10;
            AddLine(x1, hy, x1, y, brush, 1.8);
            AddLine(x1, y, x2, y, brush, 1.8);
            AddLine(x2, y, x2, hy, brush, 1.8);
        }

        private void AddRect(double x, double y, double w, double h, Brush stroke, double thickness)
        {
            var r = new Rectangle { Width = w, Height = h, Stroke = stroke, StrokeThickness = thickness };
            SetLeft(r, x);
            SetTop(r, y);
            Children.Add(r);
        }

        private void AddLine(double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        {
            Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thickness });
        }

        private void AddDot(double cx, double cy)
        {
            var el = new Ellipse { Width = 5, Height = 5, Fill = MainRebarBrush };
            SetLeft(el, cx - 2.5);
            SetTop(el, cy - 2.5);
            Children.Add(el);
        }

        private void AddLabel(double x, double y, string text, double fontSize)
        {
            var tb = new TextBlock { Text = text, Foreground = TextBrush, FontSize = fontSize, FontWeight = FontWeights.SemiBold };
            SetLeft(tb, x);
            SetTop(tb, y);
            Children.Add(tb);
        }
    }
}
