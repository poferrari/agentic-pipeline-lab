'use client';

import React, { useState } from 'react';
import { useWatchlist, useDeleteWatchItem } from '../api/use-watchlist';
import { CreateAlertModal } from './create-alert-modal';
import {
  Bell,
  Plus,
  Trash2,
  TrendingUp,
  TrendingDown,
  ShieldCheck,
  Calendar,
  AlertTriangle,
  Loader2,
  CheckCircle,
} from 'lucide-react';

export function WatchlistManager() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const { data, isLoading, isError, error, refetch, isFetching } = useWatchlist({ page: 1, pageSize: 20 });
  const { mutate: deleteAlert } = useDeleteWatchItem();

  const items = data?.items || [];
  const totalCount = data?.totalCount ?? items.length;

  const handleDelete = (id: string) => {
    if (window.confirm('Are you sure you want to delete this currency alert?')) {
      setDeletingId(id);
      deleteAlert(id, {
        onSettled: () => setDeletingId(null),
      });
    }
  };

  return (
    <section id="watchlist" aria-labelledby="watchlist-heading" className="w-full">
      {/* Header & Action Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6">
        <div>
          <div className="flex items-center gap-2">
            <span className="inline-flex h-7 w-7 items-center justify-center rounded-lg bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">
              <Bell className="h-4 w-4" />
            </span>
            <h2 id="watchlist-heading" className="text-xl font-bold tracking-tight text-slate-900 dark:text-white sm:text-2xl">
              Currency Watchlist Alerts
            </h2>
            <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-semibold text-slate-700 dark:bg-slate-800 dark:text-slate-300">
              {totalCount} {totalCount === 1 ? 'Alert' : 'Alerts'}
            </span>
          </div>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Real-time triggers dispatched via RabbitMQ transactional outbox and Supabase tenancy
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => setIsModalOpen(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            aria-label="Create new currency alert"
          >
            <Plus className="h-4 w-4" />
            <span>New Alert</span>
          </button>
        </div>
      </div>

      {/* Error state */}
      {isError && (
        <div
          role="alert"
          className="mb-6 flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-red-800 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300"
        >
          <AlertTriangle className="h-5 w-5 shrink-0 text-red-600 dark:text-red-400 mt-0.5" />
          <div className="flex-1">
            <h3 className="text-sm font-semibold">Failed to load watchlist alerts</h3>
            <p className="mt-1 text-xs">{error?.message || 'Error communicating with watchlist API endpoint.'}</p>
            <button
              onClick={() => refetch()}
              className="mt-2 text-xs font-semibold text-red-700 underline hover:text-red-900 dark:text-red-400"
            >
              Try again
            </button>
          </div>
        </div>
      )}

      {/* Loading Skeleton */}
      {isLoading && (
        <div className="space-y-3" aria-label="Loading watchlist alerts">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-20 animate-pulse rounded-xl bg-slate-200/80 dark:bg-slate-800" />
          ))}
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !isError && items.length === 0 && (
        <div className="rounded-2xl border-2 border-dashed border-slate-300 p-8 text-center dark:border-slate-800">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 text-blue-600 dark:bg-slate-800 dark:text-blue-400">
            <Bell className="h-6 w-6" />
          </div>
          <h3 className="mt-4 text-base font-bold text-slate-900 dark:text-white">
            No active currency alerts
          </h3>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400 max-w-sm mx-auto">
            Set up an alert to monitor foreign exchange rate thresholds with instant Outbox notifications.
          </p>
          <button
            type="button"
            onClick={() => setIsModalOpen(true)}
            className="mt-5 inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-xs font-semibold text-white shadow transition hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <Plus className="h-4 w-4" />
            Create First Alert
          </button>
        </div>
      )}

      {/* Alerts Grid / List */}
      {!isLoading && items.length > 0 && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <div
              key={item.id}
              className="relative flex flex-col justify-between overflow-hidden rounded-xl border border-slate-200/90 bg-white p-5 shadow-sm transition hover:border-slate-300 hover:shadow-md dark:border-slate-800 dark:bg-slate-900"
            >
              <div>
                {/* Currency pair header */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-base font-bold text-slate-900 dark:text-white">
                      {item.baseCurrency} → {item.targetCurrency}
                    </span>
                    <span className="inline-flex items-center gap-1 rounded bg-emerald-50 px-2 py-0.5 text-[11px] font-semibold text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300">
                      <CheckCircle className="h-3 w-3" />
                      Active
                    </span>
                  </div>

                  {/* Delete Button */}
                  <button
                    type="button"
                    onClick={() => handleDelete(item.id)}
                    disabled={deletingId === item.id}
                    className="rounded-lg p-1.5 text-slate-400 transition hover:bg-red-50 hover:text-red-600 focus:outline-none focus:ring-2 focus:ring-red-500 dark:hover:bg-red-950/50 dark:hover:text-red-400 disabled:opacity-50"
                    aria-label={`Delete alert for ${item.baseCurrency} to ${item.targetCurrency}`}
                  >
                    {deletingId === item.id ? (
                      <Loader2 className="h-4 w-4 animate-spin text-red-600" />
                    ) : (
                      <Trash2 className="h-4 w-4" />
                    )}
                  </button>
                </div>

                {/* Target Threshold */}
                <div className="mt-3 flex items-baseline gap-2">
                  <span className="text-2xl font-black text-slate-900 dark:text-white">
                    {item.targetRate.toFixed(4)}
                  </span>
                  <div className="flex items-center gap-1 text-xs font-semibold">
                    {item.notifyWhenAbove ? (
                      <span className="flex items-center gap-0.5 text-emerald-600 dark:text-emerald-400">
                        <TrendingUp className="h-3.5 w-3.5" /> When ≥ Above
                      </span>
                    ) : (
                      <span className="flex items-center gap-0.5 text-amber-600 dark:text-amber-400">
                        <TrendingDown className="h-3.5 w-3.5" /> When ≤ Below
                      </span>
                    )}
                  </div>
                </div>

                {/* Notes */}
                {item.notes && (
                  <p className="mt-2 text-xs italic text-slate-600 dark:text-slate-400 line-clamp-2">
                    &quot;{item.notes}&quot;
                  </p>
                )}
              </div>

              {/* Card Footer: Metadata & Security */}
              <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-3 text-[11px] text-slate-400 dark:border-slate-800">
                <span className="flex items-center gap-1">
                  <Calendar className="h-3 w-3" />
                  {new Date(item.createdAtUtc).toLocaleDateString()}
                </span>
                <span className="flex items-center gap-1 text-slate-500 dark:text-slate-400" title="BOLA / IDOR Protected by UserId Ownership">
                  <ShieldCheck className="h-3 w-3 text-blue-500" />
                  BOLA Verified
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create Alert Modal */}
      <CreateAlertModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} />
    </section>
  );
}

