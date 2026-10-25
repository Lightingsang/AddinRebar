import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterAll, beforeAll, beforeEach, describe, expect, it } from 'vitest';
import { ToastProvider } from '../ui/Toast';
import { KataApp } from './KataApp';
import fixture from './__fixtures__/kata-scene-b03.json';
import model3dFixture from './__fixtures__/kata-model3d-b03.json';
import sectionFixture from './__fixtures__/kata-section-b03-1.json';
import { frameAll, kataFrameOptions } from './elevation/framing';
import { kataPoint } from './elevation/kataElevationPainter';
import { tagBand } from './elevation/kataTagPainter';
import { buildScene } from './elevation/scene';
import { KataMockBridge } from './kataMockBridge';
import type { KataWeb3dModel, KataWebScene, KataWebSection } from './kataScene';

beforeAll(() => {
  // jsdom has no layout: the drawing area reports a fixed size once.
  globalThis.ResizeObserver ??= class {
    observe() {}
    unobserve() {}
    disconnect() {}
  } as unknown as typeof ResizeObserver;
});

beforeEach(() => {
  // the page keeps the viewer's language and theme; every test starts from the defaults
  window.localStorage.clear();
  document.documentElement.classList.remove('dark');
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
  it('shows the cells B3:B10 and the workbook, no table of sheet cells, and waits for the scene before drawing', async () => {
    renderKata(new KataMockBridge({ latencyMs: 0 }).withSceneDelay(60_000));

    expect(await screen.findByText('DY7')).toBeInTheDocument();
    const cells = screen.getByRole('region', { name: 'Thông số Kata' });
    expect(cells).toHaveTextContent('Cao trình dầm+3.600');
    expect(screen.getByTestId('kata-workbook')).toHaveTextContent('KataDam.xlsm');
    expect(screen.queryByRole('table')).toBeNull();
    expect(screen.getByTestId('kata-elevation-waiting')).toHaveTextContent('Đang dựng mặt đứng');
    expect(screen.queryByTestId('kata-elevation')).toBeNull();
  });

  it('a run without an elevation says so instead of waiting', async () => {
    renderKata();

    await waitFor(() => expect(screen.getByTestId('kata-elevation-waiting')).toHaveTextContent('Chưa có mặt đứng'));
    expect(screen.getByRole('alert')).toBe(screen.getByTestId('kata-elevation-waiting'));
  });

  it('a scene the host could not build shows why', async () => {
    renderKata(new KataMockBridge({ latencyMs: 0, sceneError: 'Không dựng được hình vẽ: boom' }));

    await waitFor(() => expect(screen.getByTestId('kata-elevation-waiting')).toHaveTextContent('Không dựng được hình vẽ: boom'));
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

  it('the footer reads Xuất Excel, Đọc thép Excel, Tạo thép Revit from left to right, with no Đóng and no product badge', async () => {
    renderKata();
    await screen.findByText('DY7');

    const names = Array.from(screen.getByTestId('kata-actions').querySelectorAll('button')).map((b) => b.textContent);
    expect(names).toEqual(['Xuất Excel', 'Đọc thép Excel', 'Tạo thép Revit']);
    expect(screen.queryByRole('button', { name: 'Đóng' })).toBeNull();
    expect(screen.queryByText('HPRebar Kata Pro')).toBeNull();
  });

  it('the language switch turns the page into English and back, and is kept', async () => {
    renderKata();
    await screen.findByText('DY7');

    await userEvent.click(screen.getByRole('button', { name: 'EN' }));
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('BEAM EXPORT TO KATA EXCEL');
    expect(screen.getByRole('button', { name: 'Export to Excel' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Kata parameters' })).toHaveTextContent('Beam level');
    expect(screen.getByRole('button', { name: 'EN' })).toHaveAttribute('aria-pressed', 'true');
    expect(window.localStorage.getItem('hp.kata.lang')).toBe('en');

    await userEvent.click(screen.getByRole('button', { name: 'VI' }));
    expect(screen.getByRole('button', { name: 'Xuất Excel' })).toBeInTheDocument();
  });

  it('the theme switch overrides Revit theme and is kept', async () => {
    renderKata();
    await screen.findByText('DY7');
    expect(document.documentElement).not.toHaveClass('dark');

    await userEvent.click(screen.getByRole('button', { name: 'Chuyển nền tối' }));
    expect(document.documentElement).toHaveClass('dark');
    expect(window.localStorage.getItem('hp.kata.theme')).toBe('dark');

    await userEvent.click(screen.getByRole('button', { name: 'Chuyển nền sáng' }));
    expect(document.documentElement).not.toHaveClass('dark');
  });

  it('picking another name parameter sends it to the host', async () => {
    const bridge = renderKata();
    await screen.findByText('DY7');

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Tham số Tên dầm' }), 'Mark');

    expect(bridge.calls).toContainEqual({ command: 'kata.update', payload: { nameParameter: 'Mark' } });
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

  it('draws the plain elevation from the scene the host sends after the first state', async () => {
    const bridge = renderKata(new KataMockBridge({ latencyMs: 0, scene }));

    const area = await screen.findByTestId('kata-elevation');
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-renderer', 'native'));
    await waitFor(() => expect(screen.getByTestId('kata-elevation')).toHaveAttribute('data-scale'));
    await waitFor(() => expect(screen.getByTestId('kata-elevation').querySelector('svg text')).not.toBeNull());
    expect(area.ownerDocument.body).toHaveTextContent('Nhịp 1');
    expect(bridge.calls.map((call) => call.command)).toEqual(['kata.getState', 'kata.ready']);
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
    const model3d = model3dFixture as unknown as KataWeb3dModel;
    const bridge = renderKata(new KataMockBridge({ latencyMs: 0, scene, sections: { 1: section }, model3d, state: { showRebar: true, showSection: true } }));
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

  it('the layer panel hides the bars, the find box frames each tag of a bar number in turn', async () => {
    const { area } = await renderKataMode();
    const bars = () => area.querySelectorAll('[data-id^=line-]').length;
    const before = bars();
    expect(before).toBeGreaterThan(0);

    await userEvent.click(screen.getByRole('button', { name: /^Lớp/ }));
    await userEvent.click(screen.getByRole('checkbox', { name: 'Thép chủ / gia cường' }));
    await waitFor(() => expect(bars()).toBeLessThan(before));

    const number = scene.rebar!.tags[0]!.numbers[0]!;
    const count = scene.rebar!.tags.filter((t) => t.numbers.includes(number)).length;
    await userEvent.type(screen.getByRole('textbox', { name: 'Tìm số hiệu thanh' }), `${number}{Enter}`);
    expect(screen.getByTestId('kata-find-result')).toHaveTextContent(`1/${count}`);
    await userEvent.type(screen.getByRole('textbox', { name: 'Tìm số hiệu thanh' }), '{Enter}');
    expect(screen.getByTestId('kata-find-result')).toHaveTextContent(count > 1 ? `2/${count}` : `1/${count}`);
  });

  it('the measure tool measures between two clicks and Esc leaves it', async () => {
    const { area, click } = await renderKataMode();
    await userEvent.click(screen.getByRole('button', { name: 'Đo' }));
    expect(area).toHaveAttribute('data-measuring', 'true');
    click(kataScreen(1000, 0));
    click(kataScreen(4000, 0));
    await waitFor(() => expect(area.querySelector('svg')!.textContent).toMatch(/L [\d.]+ mm/));

    area.focus();
    await userEvent.keyboard('{Escape}');
    await userEvent.keyboard('{Escape}');
    expect(area).not.toHaveAttribute('data-measuring');
  });

  it('Esc that closes the ViewCube menu keeps the window open', async () => {
    const { bridge } = await renderKataMode();
    await userEvent.click(screen.getByRole('tab', { name: '3D' }));
    await screen.findByTestId('kata-3d');

    await userEvent.click(screen.getByRole('button', { name: 'Tuỳ chọn ViewCube' }));
    expect(await screen.findByRole('menuitem', { name: 'Về Home' })).toBeInTheDocument();
    await userEvent.keyboard('{Escape}');

    await waitFor(() => expect(screen.queryByRole('menuitem', { name: 'Về Home' })).toBeNull());
    expect(bridge.calls).not.toContainEqual({ command: 'kata.run', payload: { action: 'close' } });
  });

  it('the 3D tab shows the plan with the bar picked on the elevation, Delete removes it from there too', async () => {
    const { bridge, area, click } = await renderKataMode();
    const bar = scene.rebar!.lines.find((l) => l.pen === 'bar' && l.keys.length > 0)!;
    const a = kataScreen(bar.points[0]!.x, bar.points[0]!.z);
    const b = kataScreen(bar.points[1]!.x, bar.points[1]!.z);
    click([(a[0] + b[0]) / 2, (a[1] + b[1]) / 2]);
    await waitFor(() => expect(Number(area.getAttribute('data-selected'))).toBeGreaterThan(0));
    const picked = Number(area.getAttribute('data-selected'));

    await userEvent.click(screen.getByRole('tab', { name: '3D' }));
    const view = await screen.findByTestId('kata-3d');
    expect(bridge.calls.some((call) => call.command === 'kata.model3d')).toBe(true);
    expect(view).toHaveAttribute('data-selected', String(picked));
    expect(view).toHaveAttribute('data-instances', String(model3dFixture.barCount));
    expect(screen.getByTestId('kata-3d-legend')).not.toHaveTextContent(/thanh|Lăn: zoom/);
    expect(screen.queryByRole('button', { name: 'Tiếp' })).toBeNull();

    view.focus();
    await userEvent.keyboard('{Delete}');
    const removal = bridge.calls.find((call) => call.command === 'kata.removeBars');
    expect(removal?.payload).toEqual({ keys: bar.keys });

    // The new plan's model replaces the old one in the same view: focus and WebGL context stay, Ctrl+Z works there.
    await waitFor(() => expect(bridge.calls.filter((call) => call.command === 'kata.model3d')).toHaveLength(2));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Hoàn tác xóa' })).toBeEnabled());
    expect(screen.getByTestId('kata-3d')).toBe(view);
    await userEvent.keyboard('{Control>}z{/Control}');
    expect(bridge.calls).toContainEqual({ command: 'kata.run', payload: { action: 'undoRemoveBars' } });

    await userEvent.click(screen.getByRole('tab', { name: '2D' }));
    expect(await screen.findByTestId('kata-elevation')).toBeInTheDocument();
  });
});
