import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, describe, expect, it } from 'vitest';
import { ToastProvider } from '../ui/Toast';
import defaultsFixture from './__fixtures__/kata-settings-default.json';
import editedFixture from './__fixtures__/kata-settings-edited.json';
import noticeFixture from './__fixtures__/kata-settings-notice.json';
import { SettingsApp } from './SettingsApp';
import type { KataSettingsError, KataSettingsSnapshot } from './settingsContracts';
import { SettingsMockBridge } from './settingsMockBridge';

const defaults = defaultsFixture as KataSettingsSnapshot;
const edited = editedFixture as KataSettingsSnapshot;
const notice = noticeFixture as KataSettingsSnapshot;

beforeAll(() => {
  // jsdom has no layout: nothing to scroll into view.
  Element.prototype.scrollIntoView ??= () => {};
});

beforeEach(() => {
  // the page keeps the viewer's language and theme; every test starts from the defaults
  window.localStorage.clear();
  document.documentElement.classList.remove('dark');
});

function open(options: { snapshot?: KataSettingsSnapshot; refuse?: KataSettingsError[]; theme?: 'light' | 'dark' } = {}) {
  const bridge = new SettingsMockBridge({ snapshot: options.snapshot ?? edited, defaults, latencyMs: 0, refuse: options.refuse, theme: options.theme });
  render(
    <ToastProvider>
      <SettingsApp bridge={bridge} />
    </ToastProvider>,
  );
  return bridge;
}

const commands = (bridge: SettingsMockBridge) => bridge.calls.map((c) => c.command);
const rowOf = (element: HTMLElement) => element.closest<HTMLElement>('[data-row]')!;

describe('SettingsApp', () => {
  it("opens on the file's values as one page, Kata's three tabs as groups with an outline", async () => {
    open();

    expect(await screen.findByLabelText('Chiều dài thép tối đa')).toHaveValue('11000');
    for (const group of ['Detail thép', 'Thông số đặc thù', 'Thép mặc định']) expect(screen.getByRole('region', { name: group })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Shop thép' })).toHaveTextContent('chưa áp dụng');
    const outline = screen.getByRole('navigation', { name: 'Mục lục' });
    expect(within(outline).getByRole('button', { name: 'Thép dọc & móc' })).toHaveAttribute('aria-current', 'location');
    expect(screen.queryByText(/mục đã sửa/)).toBeNull();
    expect(screen.queryByTestId('settings-notice')).toBeNull();
  });

  it('shows why a saved choice is shown as its default', async () => {
    open({ snapshot: notice });

    expect(await screen.findByTestId('settings-notice')).toHaveTextContent('tỷ lệ cổ chai 1/5 ngoài danh sách');
  });

  it('asks the host after each change and puts its errors under the field, in the outline and in the footer', async () => {
    const user = userEvent.setup();
    const bridge = open();
    const box = await screen.findByLabelText('Chiều dài thép tối đa');

    await user.clear(box);

    await waitFor(() => expect(commands(bridge)).toContain('settings.validate'));
    expect(await screen.findByText('Nhập một số.')).toBeInTheDocument();
    expect(box).toHaveAttribute('aria-invalid', 'true');
    expect(within(screen.getByRole('navigation', { name: 'Mục lục' })).getByLabelText('1 ô cần sửa')).toHaveTextContent('1');
    expect(screen.getByText('1 ô cần sửa')).toBeInTheDocument();
    expect(screen.getByText('1 mục đã sửa')).toBeInTheDocument();
    expect(rowOf(box).querySelector('[data-changed]')).not.toBeNull();

    await user.type(box, '12000');
    await waitFor(() => expect(screen.queryByText('Nhập một số.')).toBeNull());
    expect(box).toHaveValue('12000');
  });

  it('short lists are buttons: a hook angle is one click', async () => {
    const user = userEvent.setup();
    const bridge = open();
    await screen.findByLabelText('Chiều dài thép tối đa');

    await user.click(screen.getByRole('radio', { name: 'Đai □ bẻ móc — Góc: 180°' }));
    await user.click(screen.getByRole('radio', { name: 'Vùng nối thép trên — L từ: mép' }));
    await user.click(screen.getByRole('button', { name: /Chấp nhận/ }));

    await waitFor(() => expect(bridge.closed).toBe(true));
    expect(bridge.saved?.detail.closedStirrupHookAngle).toBe(180);
    expect(bridge.saved?.detail.topLapZoneFromCentre).toBe(false);
  });

  it('the search keeps only the rows it names, without Vietnamese marks, and says when nothing matches', async () => {
    const user = userEvent.setup();
    open();
    const maxLength = await screen.findByLabelText('Chiều dài thép tối đa');

    await user.type(screen.getByRole('searchbox', { name: 'Tìm thông số theo tên' }), 'vai bo');
    expect(rowOf(maxLength)).toHaveAttribute('hidden');
    expect(rowOf(screen.getAllByLabelText('Bẻ ngang đai vai bò (mm)')[0]!)).not.toHaveAttribute('hidden');

    await user.clear(screen.getByRole('searchbox'));
    expect(rowOf(maxLength)).not.toHaveAttribute('hidden');
    await user.type(screen.getByRole('searchbox'), 'zzz');
    expect(screen.getByText('Không có thông số nào khớp “zzz”.')).toBeInTheDocument();
  });

  it('accept saves the form as edited and closes the window', async () => {
    const user = userEvent.setup();
    const bridge = open();
    const box = await screen.findByLabelText('Chiều dài thép tối đa');
    await user.clear(box);
    await user.type(box, '12000');

    await user.click(screen.getByRole('button', { name: /Chấp nhận/ }));

    await waitFor(() => expect(bridge.closed).toBe(true));
    expect(bridge.saved?.detail.maxBarLengthMm).toBe(12000);
    expect(bridge.saved?.special.lapRows).toEqual(edited.form.special.lapRows);
  });

  it('a refused save stays open and puts the cursor in the first wrong field', async () => {
    const user = userEvent.setup();
    const bridge = open({ refuse: [{ field: 'joints.beam.stirrups', message: 'Dạng số lượng f Ø a bước, ví dụ 5f8a50.' }] });
    await screen.findByLabelText('Chiều dài thép tối đa');

    await user.click(screen.getByRole('button', { name: /Chấp nhận/ }));

    await waitFor(() => expect(document.activeElement?.id).toBe('field-joints-beam-stirrups'));
    expect(screen.getByText('Dạng số lượng f Ø a bước, ví dụ 5f8a50.')).toBeInTheDocument();
    expect(bridge.saved).toBeNull();
    expect(bridge.closed).toBe(false);
  });

  it('exit closes at once when nothing changed', async () => {
    const user = userEvent.setup();
    const clean = open();
    await screen.findByLabelText('Chiều dài thép tối đa');
    await user.click(screen.getByRole('button', { name: 'Thoát' }));
    await waitFor(() => expect(clean.closed).toBe(true));
  });

  it('exit after a change asks; staying keeps the window, discarding closes it unsaved', async () => {
    const user = userEvent.setup();
    const bridge = open();
    await user.click(await screen.findByRole('switch', { name: 'Thể hiện móc cắt kết thúc thép' }));

    await user.click(screen.getByRole('button', { name: 'Thoát' }));
    const dialog = await screen.findByRole('alertdialog');
    await user.click(within(dialog).getByRole('button', { name: 'Ở lại' }));
    expect(bridge.closed).toBe(false);

    await user.click(screen.getByLabelText('Chiều dài thép tối đa'));
    await user.keyboard('{Escape}');
    await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Bỏ thay đổi' }));
    await waitFor(() => expect(bridge.closed).toBe(true));
    expect(bridge.saved).toBeNull();
  });

  it('defaults replaces every value and the notice, and leaves the form to be saved', async () => {
    const user = userEvent.setup();
    const bridge = open({ snapshot: notice });
    await screen.findByTestId('settings-notice');
    await user.clear(screen.getByLabelText('Chiều dài thép tối đa'));

    await user.click(screen.getByRole('button', { name: 'Mặc định' }));

    await waitFor(() => expect(screen.getByLabelText('Chiều dài thép tối đa')).toHaveValue('11700'));
    expect(screen.queryByTestId('settings-notice')).toBeNull();
    expect(screen.getByText('Đã nạp thiết lập mặc định — bấm Chấp nhận để lưu.')).toBeInTheDocument();
    expect(commands(bridge)).toContain('settings.defaults');
    expect(bridge.saved).toBeNull();
  });

  it('the tables add and remove rows, and an empty row is not refused', async () => {
    const user = userEvent.setup();
    const bridge = open();
    await screen.findByLabelText('Chiều dài thép tối đa');
    const card = screen.getByRole('region', { name: 'Bảng neo nối thép' });
    const table = within(card).getByRole('table', { name: 'Bảng neo nối thép' });
    expect(within(table).getAllByRole('row')).toHaveLength(3);

    await user.click(within(card).getByRole('button', { name: 'Thêm dòng' }));
    expect(within(table).getAllByRole('row')).toHaveLength(4);
    await user.click(within(table).getByRole('button', { name: 'Xoá dòng 1' }));
    expect(within(table).getAllByRole('row')).toHaveLength(3);
    expect(within(table).getByLabelText('Ø (mm) — 1')).toHaveValue('20');

    await user.click(screen.getByRole('button', { name: /Chấp nhận/ }));
    await waitFor(() => expect(bridge.closed).toBe(true));
    expect(bridge.saved?.special.lapRows).toHaveLength(2);
  });

  it("follows Revit's theme until the viewer picks one, and switches to English", async () => {
    const user = userEvent.setup();
    const bridge = open({ theme: 'dark' });
    await screen.findByLabelText('Chiều dài thép tối đa');
    expect(document.documentElement).toHaveClass('dark');

    act(() => bridge.emitTheme('light'));
    expect(document.documentElement).not.toHaveClass('dark');

    await user.click(screen.getByTestId('settings-theme-toggle'));
    expect(document.documentElement).toHaveClass('dark');
    act(() => bridge.emitTheme('light'));
    expect(document.documentElement).toHaveClass('dark');

    await user.click(screen.getByRole('button', { name: 'EN' }));
    expect(screen.getByLabelText('Maximum bar length')).toHaveValue('11000');
    expect(window.localStorage.getItem('hp.kata.lang')).toBe('en');
  });
});
