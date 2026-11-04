import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import { ToastProvider } from '../ui/Toast';
import errorsFixture from './__fixtures__/kata-column-rebar-errors.json';
import sampleFixture from './__fixtures__/kata-column-rebar-sample.json';
import stackFixture from './__fixtures__/kata-column-sample.json';
import { ColumnApp } from './ColumnApp';
import type { KataColumnPreview, KataColumnRebarSummary } from './columnContracts';
import { ColumnMockBridge } from './columnMockBridge';

const planned = sampleFixture as KataColumnRebarSummary;
const stack = stackFixture as KataColumnPreview;
const refused = errorsFixture as KataColumnRebarSummary;

beforeEach(() => {
  window.localStorage.clear();
  document.documentElement.classList.remove('dark');
});

function open(rebar: KataColumnRebarSummary) {
  const bridge = new ColumnMockBridge({ rebar, latencyMs: 0 });
  render(
    <ToastProvider>
      <ColumnApp bridge={bridge} />
    </ToastProvider>,
  );
  return bridge;
}

describe('Kata Column Rebar page', () => {
  it('lists the plan per storey, top down, and creates it on request', async () => {
    const bridge = open(planned);
    const user = userEvent.setup();

    expect(await screen.findByText('Kata Column Rebar')).toBeInTheDocument();
    const rows = screen.getAllByRole('row');
    expect(rows[1]).toHaveTextContent('FLOOR 4');
    expect(screen.getByText('24Ø25 + 16Ø20')).toBeInTheDocument();

    await user.click(screen.getByTestId('rebar-generate'));

    await waitFor(() => expect(bridge.generated).toBe(1));
    expect(await screen.findByText(/Đã tạo 240 phần tử thép/)).toBeInTheDocument();
    // The sheet is read again after every creation, so the page shows what the next click would create.
    await waitFor(() => expect(bridge.calls.at(-1)?.command).toBe('column.reload'));
    expect(screen.queryByTestId('rebar-warnings')).toBeNull();
  });

  it('draws the column drawing (elevation + sections) in place of the section grid', async () => {
    open(planned);

    const drawing = await screen.findByTestId('column-drawing');
    expect(drawing).toHaveTextContent('Bản vẽ thép cột');
    expect(screen.getByTestId('drawing-canvas')).toBeInTheDocument();
  });

  it('keeps the warnings of the run on the page', async () => {
    const bridge = open(planned);
    bridge.generateWarnings = ['Model không có kiểu móc 135°: đai bao và đai trong được vẽ không móc.'];
    const user = userEvent.setup();

    await user.click(await screen.findByTestId('rebar-generate'));

    expect(await screen.findByTestId('rebar-warnings')).toHaveTextContent('không móc');
  });

  it('blocks every action while a request is pending', async () => {
    const bridge = new ColumnMockBridge({ preview: stack, rebar: planned, latencyMs: 200 });
    render(
      <ToastProvider>
        <ColumnApp bridge={bridge} />
      </ToastProvider>,
    );
    const user = userEvent.setup();

    await user.click(await screen.findByTestId('step-rebar'));

    expect(screen.getByTestId('step-export')).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Đóng' })).toBeDisabled();
    await waitFor(() => expect(screen.getByTestId('rebar-generate')).toBeEnabled());
  });

  it('opens on step 1 and reads sheet Cot again when the user goes to step 2', async () => {
    const bridge = new ColumnMockBridge({ preview: stack, rebar: planned, latencyMs: 0 });
    render(
      <ToastProvider>
        <ColumnApp bridge={bridge} />
      </ToastProvider>,
    );
    const user = userEvent.setup();

    expect(await screen.findByTestId('column-write')).toBeInTheDocument();
    expect(screen.queryByTestId('rebar-generate')).toBeNull();

    await user.click(screen.getByTestId('step-rebar'));

    expect(await screen.findByTestId('rebar-generate')).toBeEnabled();
    expect(bridge.calls.map((c) => c.command)).toEqual(['column.load', 'column.reload']);
    expect(screen.getByTestId('step-rebar')).toHaveAttribute('aria-selected', 'true');
  });

  it('refuses to create while the sheet has errors and names the cells', async () => {
    open(refused);

    expect(await screen.findByTestId('rebar-errors')).toHaveTextContent('J10');
    expect(screen.queryByTestId('column-drawing')).toBeNull();
    expect(screen.getByTestId('rebar-generate')).toBeDisabled();
  });
});
