import { act, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { BridgeError, type BridgeClient } from '../../bridge/bridgeClient';
import { mockDocument } from '../../bridge/mockData';
import { MockBridgeClient } from '../../bridge/mockBridgeClient';
import type { CommandName } from '../../bridge/protocol';
import { ToastProvider } from '../../ui/Toast';
import { DashboardPage } from './DashboardPage';

function renderPage(bridge: BridgeClient) {
  return render(
    <ToastProvider>
      <DashboardPage bridge={bridge} toolIds={['revit.reviewWarnings']} />
    </ToastProvider>,
  );
}

describe('DashboardPage', () => {
  it('shows the document, the view and the statistics once loaded', async () => {
    renderPage(new MockBridgeClient({ latencyMs: 0 }));

    expect(await screen.findByText(mockDocument.title!)).toBeInTheDocument();
    expect(screen.getByText('Tầng 2 - Mặt bằng')).toBeInTheDocument();
    expect(await screen.findByRole('button', { name: /Chọn .* Walls/ })).toBeInTheDocument();
  });

  it('shows the empty state when no document is open', async () => {
    renderPage(new MockBridgeClient({ latencyMs: 0, hasDocument: false }));

    expect(await screen.findByText('Chưa mở tài liệu')).toBeInTheDocument();
  });

  it('selects a category in Revit, updates the selection count and confirms with a toast', async () => {
    const user = userEvent.setup();
    renderPage(new MockBridgeClient({ latencyMs: 0 }));

    await user.click(await screen.findByRole('button', { name: /Chọn .* Doors/ }));

    expect(await screen.findByText('Đã chọn 412 phần tử Doors.')).toBeInTheDocument();
    expect(screen.getByTestId('selection-count')).toHaveTextContent('412');
  });

  it('marks the numbers stale after an edit in Revit', async () => {
    const bridge = new MockBridgeClient({ latencyMs: 0 });
    renderPage(bridge);
    await screen.findByRole('button', { name: /Chọn .* Walls/ });

    act(() => bridge.simulateEdit());

    expect(await screen.findByText(/Dữ liệu cũ/)).toBeInTheDocument();
  });

  it('filters categories by name', async () => {
    const user = userEvent.setup();
    renderPage(new MockBridgeClient({ latencyMs: 0 }));
    const list = (await screen.findByRole('button', { name: /Chọn .* Walls/ })).closest('ul')!;

    await user.type(screen.getByLabelText('Lọc category'), 'struct');

    expect(within(list).getAllByRole('listitem')).toHaveLength(2);
  });

  it('asks before selecting a very large category and selects nothing when declined', async () => {
    const user = userEvent.setup();
    const inner = new MockBridgeClient({ latencyMs: 0 });
    const requests: string[] = [];
    const bridge: BridgeClient = {
      isMock: true,
      on: inner.on,
      request: (async (command: CommandName, payload?: unknown) => {
        requests.push(command);
        const answer = await inner.request(command, payload as never);
        return command === 'dashboard.getStatistics'
          ? { ...(answer as object), categories: [{ categoryId: -2000051, name: 'Lines', count: 19617 }] }
          : answer;
      }) as BridgeClient['request'],
    };
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
    renderPage(bridge);

    await user.click(await screen.findByRole('button', { name: /Chọn .* Lines/ }));

    expect(confirm).toHaveBeenCalledOnce();
    expect(requests).not.toContain('dashboard.showElements');
    confirm.mockRestore();
  });

  it('shows the error state with a retry when statistics fail', async () => {
    const inner = new MockBridgeClient({ latencyMs: 0 });
    const failing: BridgeClient = {
      isMock: true,
      on: inner.on,
      request: ((command: CommandName, payload?: unknown) =>
        command === 'dashboard.getStatistics'
          ? Promise.reject(new BridgeError({ code: 'TIMEOUT', message: 'Revit không phản hồi.' }))
          : inner.request(command, payload as never)) as BridgeClient['request'],
    };

    renderPage(failing);

    expect(await screen.findByText('Không thống kê được')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Thử lại' })).toBeInTheDocument();
  });
});
