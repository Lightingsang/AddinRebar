import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { ToastProvider } from '../ui/Toast';
import { KataApp } from './KataApp';
import fixture from './__fixtures__/kata-scene-b03.json';
import sectionFixture from './__fixtures__/kata-section-b03-1.json';
import { frameAll, kataFrameOptions } from './elevation/framing';
import { kataPoint } from './elevation/kataElevationPainter';
import { tagBand } from './elevation/kataTagPainter';
import { buildScene } from './elevation/scene';
import { KataMockBridge } from './kataMockBridge';
import type { KataWebScene, KataWebSection } from './kataScene';

beforeAll(() => {
  // jsdom has no layout: the drawing area reports a fixed size once.
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  } as unknown as typeof ResizeObserver;
});

function renderKata(bridge = new KataMockBridge({ latencyMs: 0 })) {
  render(
    <ToastProvider>
      <KataApp bridge={bridge} />
    </ToastProvider>,
  );
  return bridge;
}

describe('KataApp', () => {
  it('shows the header cells, the table and the drawing the host sends', async () => {
    renderKata();

    expect(await screen.findByText('DY7')).toBeInTheDocument();
    expect(screen.getByText('+3.600')).toBeInTheDocument();
    const table = screen.getByRole('table', { name: 'Các ô ghi sang Kata Excel' });
    expect(within(table).getByText('Nhịp 2')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByTestId('kata-elevation').querySelector('svg')).not.toBeNull());
  });

  it('reports the drawing area size so the host paints to fit it', async () => {
    const bridge = renderKata();

    await screen.findByText('DY7');
    expect(bridge.calls.some((call) => call.command === 'kata.viewport')).toBe(true);
  });

  it('a click on a table column selects it through the host', async () => {
    const bridge = renderKata();
    const table = await screen.findByRole('table', { name: 'Các ô ghi sang Kata Excel' });

    await userEvent.click(within(table).getByRole('columnheader', { name: 'F' }));

    expect(bridge.calls).toContainEqual({ command: 'kata.update', payload: { selectedColumn: 3 } });
    await waitFor(() => expect(screen.getByTestId('kata-selection')).toHaveTextContent('Cột F · Nhịp 2'));
  });

  it('the footer buttons run the view model commands and show the status it reports', async () => {
    const bridge = renderKata();
    await screen.findByText('DY7');

    await userEvent.click(screen.getByRole('button', { name: 'Xuất Excel' }));

    expect(bridge.calls).toContainEqual({ command: 'kata.run', payload: { action: 'export' } });
    await waitFor(() => expect(screen.getByRole('status')).toHaveTextContent("Đã ghi 5 cột vào 'KataDam.xlsm'"));
  });

  it('a disabled command stays disabled until the host enables it', async () => {
    renderKata();
    await screen.findByText('DY7');

    expect(screen.getByRole('button', { name: 'Tạo thép Revit' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Đọc thép Excel' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Tạo thép Revit' })).toBeEnabled());
  });

  it('picking another name parameter sends it to the host', async () => {
    const bridge = renderKata();
    await screen.findByText('DY7');

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Tham số Tên dầm' }), 'Mark');

    expect(bridge.calls).toContainEqual({ command: 'kata.update', payload: { nameParameter: 'Mark' } });
  });

  it('Delete on the drawing is sent as a key gesture', async () => {
    const bridge = renderKata();
    await screen.findByText('DY7');

    screen.getByTestId('kata-elevation').focus();
    await userEvent.keyboard('{Delete}');

    expect(bridge.calls).toContainEqual({ command: 'kata.canvas', payload: { kind: 'key', key: 'Delete', ctrl: false } });
  });
});

describe('KataApp drawing the scene itself', () => {
  const scene = fixture as unknown as KataWebScene;
  const section = sectionFixture as unknown as KataWebSection;

  beforeAll(() => {
    // jsdom has no PointerEvent: a MouseEvent carries the same button and coordinates.
    globalThis.PointerEvent ??= MouseEvent as unknown as typeof PointerEvent;
    // jsdom has no layout: give the drawing area a size so the page can frame the run.
    Object.defineProperty(HTMLElement.prototype, 'clientWidth', { configurable: true, get: () => 1200 });
    Object.defineProperty(HTMLElement.prototype, 'clientHeight', { configurable: true, get: () => 500 });
  });

  afterAll(() => {
    delete (HTMLElement.prototype as { clientWidth?: number }).clientWidth;
    delete (HTMLElement.prototype as { clientHeight?: number }).clientHeight;
  });

  it('draws the plain elevation natively, without asking the host for a picture', async () => {
    const bridge = renderKata(new KataMockBridge({ latencyMs: 0, scene }));

    const area = await screen.findByTestId('kata-elevation');
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-renderer', 'native'));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-scale'));
    await waitFor(() => expect(screen.getByTestId('kata-elevation').querySelector('svg text')).not.toBeNull());
    expect(area.ownerDocument.body).toHaveTextContent('Nhịp 1');
    expect(bridge.calls.some((call) => call.command === 'kata.viewport' || call.command === 'kata.canvas')).toBe(false);
  });

  it('a click on a span selects it through the view model', async () => {
    const bridge = renderKata(new KataMockBridge({ latencyMs: 0, scene }));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-renderer', 'native'));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-scale'));
    const area = screen.getByTestId('kata-elevation');
    area.getBoundingClientRect = () => ({ left: 0, top: 0, right: 1200, bottom: 500, width: 1200, height: 500, x: 0, y: 0, toJSON: () => ({}) });

    // Station 5000 (span D) on a 1200 px frame of 0…33600 with 36 px margins.
    const x = 36 + (5000 * (1200 - 72)) / 33600;
    fireEvent.pointerDown(area, { button: 0, clientX: x, clientY: 250, pointerId: 1 });
    fireEvent.pointerUp(area, { button: 0, clientX: x, clientY: 250, pointerId: 1 });

    expect(bridge.calls).toContainEqual({ command: 'kata.update', payload: { selectedColumn: 1 } });
  });

  it('Toàn dải and the span buttons frame the drawing from the focus requests of the view model', async () => {
    const bridge = renderKata(new KataMockBridge({ latencyMs: 0, scene }));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-renderer', 'native'));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-scale'));
    const whole = screen.getByTestId('kata-elevation').getAttribute('data-scale');

    await userEvent.click(screen.getByRole('button', { name: 'Tiếp' }));
    await waitFor(() => expect(screen.getByTestId('kata-elevation').getAttribute('data-scale')).not.toBe(whole));
    await userEvent.click(screen.getByRole('button', { name: 'Toàn dải' }));
    await waitFor(() => expect(screen.getByTestId('kata-elevation').getAttribute('data-scale')).toBe(whole));
    expect(bridge.calls).toContainEqual({ command: 'kata.run', payload: { action: 'frameAll' } });
  });

  /** Screen point of a drawing point of Kata's elevation, framed as the page frames it in a 1200 × 500 area. */
  function kataScreen(x: number, z: number): [number, number] {
    const rebar = scene.rebar!;
    const options = kataFrameOptions(scene.elevation, rebar, scene.stationMap, tagBand(rebar, scene.elevation.topMm - scene.elevation.bottomMm));
    const viewport = frameAll(scene.elevation, 1200, 500, options);
    return kataPoint(buildScene(scene.elevation, viewport, 1200, 500, -1), scene.stationMap)(x, z);
  }

  async function renderKataMode() {
    const bridge = renderKata(new KataMockBridge({ latencyMs: 0, scene, sections: { 1: section }, state: { showRebar: true, showSection: true } }));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-mode', 'kata'));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-scale'));
    const area = screen.getByTestId('kata-elevation');
    area.getBoundingClientRect = () => ({ left: 0, top: 0, right: 1200, bottom: 500, width: 1200, height: 500, x: 0, y: 0, toJSON: () => ({}) });
    const click = ([x, y]: [number, number]) => {
      fireEvent.pointerDown(area, { button: 0, clientX: x, clientY: y, pointerId: 1 });
      fireEvent.pointerUp(area, { button: 0, clientX: x, clientY: y, pointerId: 1 });
    };
    return { bridge, area, click };
  }

  it('a click on a bar selects its group, Delete removes it through the view model, Ctrl+Z undoes', async () => {
    const { bridge, area, click } = await renderKataMode();
    const bar = scene.rebar!.lines.find((l) => l.pen === 'bar' && l.keys.length > 0)!;
    const a = kataScreen(bar.points[0]!.x, bar.points[0]!.z);
    const b = kataScreen(bar.points[1]!.x, bar.points[1]!.z);
    click([(a[0] + b[0]) / 2, (a[1] + b[1]) / 2]);
    await waitFor(() => expect(Number(area.getAttribute('data-selected'))).toBeGreaterThan(0));

    area.focus();
    await userEvent.keyboard('{Delete}');
    expect(bridge.calls.some((call) => call.command === 'kata.removeBars')).toBe(true);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Hoàn tác xóa' })).toBeEnabled());

    await userEvent.keyboard('{Control>}z{/Control}');
    expect(bridge.calls).toContainEqual({ command: 'kata.run', payload: { action: 'undoRemoveBars' } });
  });

  it('a click on a section flag opens Kata’s section in a panel, Esc closes it', async () => {
    const { area, click } = await renderKataMode();
    const flag = scene.rebar!.flags.find((f) => f.number === 1)!;
    const [x, y] = kataScreen(flag.x, flag.z);
    click([x + 1, y + (flag.below ? 1 : -1) * 3]);
    const panel = await screen.findByTestId('kata-section-panel');
    expect(panel).toHaveAttribute('data-section', '1');
    await waitFor(() => expect(panel.querySelector('svg text')).not.toBeNull());

    area.focus();
    await userEvent.keyboard('{Escape}');
    await waitFor(() => expect(screen.queryByTestId('kata-section-panel')).toBeNull());
  });
});

