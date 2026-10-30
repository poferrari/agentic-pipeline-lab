import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '@/lib/api/client';
import type { ExchangeRatesQuote } from '@/lib/api/types';

export const ratesKeys = {
  all: ['rates'] as const,
  byBase: (baseCurrency: string) => [...ratesKeys.all, baseCurrency.toUpperCase()] as const,
};

export function useExchangeRates(baseCurrency: string = 'USD') {
  const normalized = (baseCurrency || 'USD').toUpperCase();

  return useQuery<ExchangeRatesQuote, Error>({
    queryKey: ratesKeys.byBase(normalized),
    queryFn: () => apiFetch<ExchangeRatesQuote>(`/api/v1/rates/${normalized}`),
    staleTime: 60 * 1000, // 1 minute fresh cache (aligned with backend HybridCache)
    refetchInterval: 5 * 60 * 1000, // auto refetch every 5 minutes
    retry: 2,
    enabled: Boolean(normalized && normalized.length === 3),
  });
}

