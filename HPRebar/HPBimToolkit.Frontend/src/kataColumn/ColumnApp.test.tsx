import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import { ToastProvider } from '../ui/Toast';
import sampleFixture from './__fixtures__/kata-column-sample.json';
import steppedFixture from './__fixtures__/kata-column-stepped.json';
import { ColumnApp } from './ColumnApp';
import type { KataColumnPreview, KataColumnWorkbook } from './columnContracts';
import { ColumnMockBridge } from './columnMockBridge';

const sample = sampleFixture as KataColumnPreview;
const stepped = steppedFixture as KataColumnPreview;
const NO_SHEET: KataColumnWorkbook = { ready: false, name: 'Book1', error: "Workbook 'Book1' không có sheet 'Cot'" };

beforeEach(() => {
  window.localStorage.clear();
  document.documentElement.classList.remove('dark');
});

function open(preview: KataColumnPreview, workbook?: KataColumnWorkbook) {
  const bridge = new ColumnMockBridge({ preview, workbook, latencyMs: 0 });
  render(
    <ToastProvider>
      <ColumnApp bridge={bridge} />
    </ToastProvider>,
  );
  return bridge;
}

describe('ColumnApp', () => {
  it('lists the storeys top down with the cells Kata will read', async () => {
    open(sample);

    const rows = await screen.findAllByRole('row');
    expect(rows[1]).toHaveTextContent('FLOOR 5');
    expect(rows[1]).toHaveTextContent('hàng đỉnh');
    expect(rows[rows.length - 1]).toHaveTextContent('BASEMENT3.0001000800600tầng đầu');
    expect(screen.getByText('-5.5')).toBeInTheDocument();
    expect(screen.queryByTestId('column-warnings')).toBeNull();
  });

  it('shows the placement Kata will use and the host warnings', async () => {
    open(stepped);

    expect(await screen.findByText('giữ mép trái')).toBeInTheDocument();
    expect(screen.getAllByText('0 (tròn)')).toHaveLength(2); // the round top storey and the row Kata needs above it
    expect(screen.getByTestId('column-warnings')).toHaveTextContent('No pile cap');
  });

  it('writes the name and count the user typed', async () => {
    const bridge = open(sample);
    const user = userEvent.setup();

    const name = await screen.findByLabelText('Tên cột (C3)');
    await user.clear(name);
    await user.type(name, 'C7');
    await user.click(screen.getByTestId('column-write'));

    await waitFor(() => expect(bridge.written).toEqual({ name: 'C7', count: 1 }));
    expect(await screen.findByText(/Đã ghi 6 tầng/)).toBeInTheDocument();
    expect(screen.queryByTestId('column-no-bars')).toBeNull();
  });

  it('names the storeys Kata will refuse because their bars are still empty', async () => {
    const bridge = open(sample);
    bridge.rowsWithoutBars = ['FLOOR 4', 'FLOOR 5'];
    const user = userEvent.setup();

    await user.click(await screen.findByTestId('column-write'));

    expect(await screen.findByTestId('column-no-bars')).toHaveTextContent('FLOOR 4, FLOOR 5');
  });

  it('keeps Write disabled until Excel has a Kata workbook, then probes again', async () => {
    const bridge = open(sample, NO_SHEET);
    const user = userEvent.setup();

    expect(await screen.findByTestId('column-workbook')).toHaveTextContent("không có sheet 'Cot'");
    expect(screen.getByTestId('column-write')).toBeDisabled();

    bridge.activate();
    await user.click(screen.getByRole('button', { name: 'Kiểm tra Excel' }));

    await waitFor(() => expect(screen.getByTestId('column-write')).toBeEnabled());
  });
});
