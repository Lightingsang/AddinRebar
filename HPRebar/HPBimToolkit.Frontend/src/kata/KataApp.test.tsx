import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, describe, expect, it } from 'vitest';
import { ToastProvider } from '../ui/Toast';
import { KataApp } from './KataApp';
import { KataMockBridge } from './kataMockBridge';

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
