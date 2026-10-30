import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '@/lib/api/client';
import type { CreateWatchItemRequest, PagedWatchlist, WatchItem } from '@/lib/api/types';

export interface WatchlistQueryParams {
  page?: number;
  pageSize?: number;
  isActive?: boolean;
}

export const watchlistKeys = {
  all: ['watchlist'] as const,
  list: (params: WatchlistQueryParams) => [...watchlistKeys.all, 'list', params] as const,
  detail: (id: string) => [...watchlistKeys.all, 'detail', id] as const,
};

export function useWatchlist(params: WatchlistQueryParams = { page: 1, pageSize: 20 }) {
  const query = new URLSearchParams();
  if (params.page !== undefined) query.set('page', params.page.toString());
  if (params.pageSize !== undefined) query.set('pageSize', params.pageSize.toString());
  if (params.isActive !== undefined) query.set('isActive', params.isActive.toString());

  const queryString = query.toString() ? `?${query.toString()}` : '';

  return useQuery<PagedWatchlist, Error>({
    queryKey: watchlistKeys.list(params),
    queryFn: () => apiFetch<PagedWatchlist>(`/api/v1/watchlist${queryString}`),
    staleTime: 30 * 1000,
  });
}

export function useCreateWatchItem() {
  const queryClient = useQueryClient();

  return useMutation<WatchItem, Error, CreateWatchItemRequest>({
    mutationFn: (newAlert) =>
      apiFetch<WatchItem>('/api/v1/watchlist', {
        method: 'POST',
        body: JSON.stringify(newAlert),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: watchlistKeys.all });
    },
  });
}

export function useDeleteWatchItem() {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: (id) =>
      apiFetch<void>(`/api/v1/watchlist/${id}`, {
        method: 'DELETE',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: watchlistKeys.all });
    },
  });
}

