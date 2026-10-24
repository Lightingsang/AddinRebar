import { useEffect, useRef, useState, type KeyboardEvent, type PointerEvent, type WheelEvent } from 'react';
import { endPress, panOffset, startPress, wheelNotches, type Press } from './elevationGestures';
import type { KataCanvasInput, KataDrawingFrame } from './kataContracts';

interface ElevationViewProps {
  drawing: KataDrawingFrame | null;
  gesture(input: KataCanvasInput): Promise<boolean>;
  setViewport(width: number, height: number): void;
  /** Esc that the drawing did not use (no section open, nothing selected): the WPF window closes on it. */
  onUnhandledEscape(): void;
}

const RESIZE_SETTLE_MS = 120;

/**
 * Shows the elevation the host painted (SVG written from the WPF canvas) and sends the user's gestures back to it.
 * A pan moves the picture at once and is sent whole on release; wheel notches that arrive while a zoom is on its way
 * are added up and sent next, so a fast wheel never queues dozens of repaints.
 */
export function ElevationView({ drawing, gesture, setViewport, onUnhandledEscape }: ElevationViewProps) {
  const box = useRef<HTMLDivElement>(null);
  const press = useRef<Press | null>(null);
  const zoom = useRef({ busy: false, notches: 0, x: 0, y: 0 });
  const [offset, setOffset] = useState({ dx: 0, dy: 0 });

  // The host paints at the area's size: tell it when the window is resized (once the size settles).
  useEffect(() => {
    const element = box.current;
    if (!element) return;
    let timer: ReturnType<typeof setTimeout> | undefined;
    const report = () => setViewport(element.clientWidth, element.clientHeight);
    const observer = new ResizeObserver(() => {
      clearTimeout(timer);
      timer = setTimeout(report, RESIZE_SETTLE_MS);
    });
    observer.observe(element);
    report();
    return () => {
      clearTimeout(timer);
      observer.disconnect();
    };
  }, [setViewport]);

  // A new picture already includes the pan that was being previewed.
  useEffect(() => setOffset({ dx: 0, dy: 0 }), [drawing?.revision]);

  // Drawing coordinates start inside the border, where the SVG is placed.
  const at = (e: { clientX: number; clientY: number }) => {
    const element = box.current!;
    const rect = element.getBoundingClientRect();
    return { x: e.clientX - rect.left - element.clientLeft, y: e.clientY - rect.top - element.clientTop };
  };

  const onPointerDown = (e: PointerEvent<HTMLDivElement>) => {
    const p = at(e);
    press.current = startPress(p.x, p.y, e.button, e.shiftKey);
    if (press.current) {
      e.currentTarget.setPointerCapture(e.pointerId);
      e.currentTarget.focus();
      e.preventDefault();
    }
  };

  const onPointerMove = (e: PointerEvent<HTMLDivElement>) => {
    if (!press.current?.panning) return;
    const p = at(e);
    setOffset(panOffset(press.current, p.x, p.y));
  };

  const onPointerUp = (e: PointerEvent<HTMLDivElement>) => {
    const started = press.current;
    press.current = null;
    if (!started) return;
    const p = at(e);
    const release = endPress(started, p.x, p.y);
    if (!release) {
      setOffset({ dx: 0, dy: 0 });
      return;
    }

    if (release.kind === 'click') {
      void gesture({ kind: 'click', x: release.x, y: release.y });
      return;
    }

    // A pan that changes nothing on the picture brings no new drawing: drop the preview offset once it is answered.
    const shown = drawing?.revision;
    void gesture({ kind: 'pan', x: release.x, y: release.y, dx: release.dx, dy: release.dy }).then(() =>
      setTimeout(() => {
        if (box.current?.dataset.revision === String(shown ?? 0)) setOffset({ dx: 0, dy: 0 });
      }, 500),
    );
  };

  const onWheel = (e: WheelEvent<HTMLDivElement>) => {
    const p = at(e);
    const pending = zoom.current;
    pending.notches += wheelNotches(e.deltaY, e.deltaMode);
    pending.x = p.x;
    pending.y = p.y;
    if (pending.busy) return;
    const send = () => {
      if (Math.abs(pending.notches) < 1e-3) {
        pending.busy = false;
        return;
      }
      const notches = pending.notches;
      pending.notches = 0;
      pending.busy = true;
      void gesture({ kind: 'zoom', x: pending.x, y: pending.y, notches }).finally(send);
    };
    send();
  };

  const onAuxClick = (e: { button: number; detail: number; clientX: number; clientY: number }) => {
    if (e.button === 1 && e.detail === 2) void gesture({ kind: 'zoomExtents', ...at(e) });
  };

  const onKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    const key = e.key === 'Delete' || e.key === 'Escape' ? e.key : e.ctrlKey && (e.key === 'z' || e.key === 'Z') ? 'z' : null;
    if (!key) return;
    e.preventDefault();
    void gesture({ kind: 'key', key, ctrl: e.ctrlKey }).then((handled) => {
      if (!handled && key === 'Escape') onUnhandledEscape();
    });
  };

  return (
    <div
      ref={box}
      tabIndex={0}
      role="img"
      aria-label="Mặt đứng dải dầm"
      data-testid="kata-elevation"
      data-revision={drawing?.revision ?? 0}
      className="relative h-full min-h-0 w-full cursor-crosshair overflow-hidden rounded border border-line bg-panel-muted outline-none select-none"
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={onPointerUp}
      onPointerCancel={() => {
        press.current = null;
        setOffset({ dx: 0, dy: 0 });
      }}
      onWheel={onWheel}
      onAuxClick={onAuxClick}
      onKeyDown={onKeyDown}
    >
      {drawing ? (
        <div
          className="pointer-events-none absolute left-0 top-0"
          style={{ transform: `translate(${offset.dx}px, ${offset.dy}px)` }}
          // The SVG is written by the add-in from its own WPF drawing (text escaped there); it carries no script.
          dangerouslySetInnerHTML={{ __html: drawing.svg }}
        />
      ) : (
        <div className="flex h-full items-center justify-center text-[12px] text-muted">Đang vẽ mặt đứng…</div>
      )}
    </div>
  );
}
