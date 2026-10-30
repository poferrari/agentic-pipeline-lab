import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RatesTicker } from '../components/rates-ticker';

// Mock hook
vi.mock('../api/use-exchange-rates', () => ({
  useExchangeRates: vi.fn((baseCurrency: string) => ({
    data: {
      baseCurrency,
      referenceDate: '2026-10-29',
      provider: 'Frankfurter (ECB)',
      rates: {
        BRL: 5.4321,
        EUR: 0.9254,
        GBP: 0.7712,
        JPY: 153.25,
      },
      fetchedAtUtc: '2026-10-29T20:00:00Z',
    },
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
    isFetching: false,
  })),
}));

describe('RatesTicker Component', () => {
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

  it('renders heading and key rate cards', () => {
    renderWithProviders(<RatesTicker />);

    expect(screen.getByRole('heading', { name: /Live Currency Exchange Rates/i })).toBeInTheDocument();
    expect(screen.getByText('USD/BRL')).toBeInTheDocument();
    expect(screen.getByText('5.4321')).toBeInTheDocument();
    expect(screen.getByText('USD/EUR')).toBeInTheDocument();
    expect(screen.getByText('0.9254')).toBeInTheDocument();
  });

  it('displays instant converter widget', () => {
    renderWithProviders(<RatesTicker />);

    expect(screen.getByText(/Instant FX Rate Calculator/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Amount/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Convert To/i)).toBeInTheDocument();
  });
});

