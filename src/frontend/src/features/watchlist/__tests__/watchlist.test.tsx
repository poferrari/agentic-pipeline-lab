import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { WatchlistManager } from '../components/watchlist-manager';

const mockDelete = vi.fn();
const mockOpenLoginModal = vi.fn();
let mockIsAuthenticated = true;

vi.mock('@/providers/auth-provider', () => ({
  useAuth: vi.fn(() => ({
    user: { id: 'test-user', email: 'test@example.com' },
    isAuthenticated: mockIsAuthenticated,
    openLoginModal: mockOpenLoginModal,
  })),
}));

vi.mock('../api/use-watchlist', () => ({
  useWatchlist: vi.fn(() => ({
    data: {
      items: [
        {
          id: 'test-item-1',
          userId: 'test-user',
          baseCurrency: 'USD',
          targetCurrency: 'BRL',
          targetRate: 5.5,
          notifyWhenAbove: true,
          isActive: true,
          notes: 'Test alert notes',
          createdAtUtc: '2026-10-29T18:00:00Z',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    },
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
    isFetching: false,
  })),
  useCreateWatchItem: vi.fn(() => ({
    mutate: vi.fn(),
    isPending: false,
    error: null,
    reset: vi.fn(),
  })),
  useDeleteWatchItem: vi.fn(() => ({
    mutate: mockDelete,
    isPending: false,
  })),
}));

describe('WatchlistManager Component', () => {
  beforeEach(() => {
    mockIsAuthenticated = true;
    vi.clearAllMocks();
  });

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  const renderWithProviders = (ui: React.ReactElement) => {
    return render(
      <QueryClientProvider client={queryClient}>
        {ui}
      </QueryClientProvider>
    );
  };

  it('renders watchlist title and active alerts when authenticated', () => {
    renderWithProviders(<WatchlistManager />);

    expect(screen.getByRole('heading', { name: /Currency Watchlist Alerts/i })).toBeInTheDocument();
    expect(screen.getByText('USD → BRL')).toBeInTheDocument();
    expect(screen.getByText('5.5000')).toBeInTheDocument();
    expect(screen.getByText(/When ≥ Above/i)).toBeInTheDocument();
    expect(screen.getByText('"Test alert notes"')).toBeInTheDocument();
  });

  it('opens create alert modal on clicking New Alert button when authenticated', () => {
    renderWithProviders(<WatchlistManager />);

    const newAlertBtn = screen.getByRole('button', { name: /Create new currency alert/i });
    fireEvent.click(newAlertBtn);

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('Create Currency Watch Alert')).toBeInTheDocument();
  });

  it('renders access control notice when unauthenticated', () => {
    mockIsAuthenticated = false;
    renderWithProviders(<WatchlistManager />);

    expect(screen.getByText(/Access Control: Authentication Required/i)).toBeInTheDocument();
    expect(screen.getByText(/The Watchlist route stores personal thresholds isolated by Supabase User tenancy/i)).toBeInTheDocument();
  });
});
