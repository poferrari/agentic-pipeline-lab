'use client';

import React, { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 1000 * 30, // 30 seconds
            refetchOnWindowFocus: false,
            retry: (failureCount, error) => {
              if (failureCount >= 2) return false;
              // Don't retry on client errors (400, 401, 404, 409)
              const status = (error as { problem?: { status?: number } })?.problem?.status;
              if (status && status >= 400 && status < 500) return false;
              return true;
            },
          },
        },
      })
  );

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

