'use client';

import React, { useState } from 'react';
import { useExchangeRates } from '../api/use-exchange-rates';
import { RefreshCw, TrendingUp, DollarSign, Clock, AlertTriangle, ArrowRight } from 'lucide-react';

const COMMON_CURRENCIES = ['USD', 'EUR', 'BRL', 'GBP', 'JPY', 'CAD', 'CHF', 'AUD'];

export function RatesTicker() {
  const [baseCurrency, setBaseCurrency] = useState<string>('USD');
  const [calcAmount, setCalcAmount] = useState<number>(100);
  const [calcTarget, setCalcTarget] = useState<string>('BRL');

  const { data, isLoading, isError, error, refetch, isFetching } = useExchangeRates(baseCurrency);

  const rates = data?.rates || {};
  const targetRate = rates[calcTarget] ?? 0;
  const convertedTotal = calcAmount * targetRate;

  return (
    <section id="rates" aria-labelledby="rates-heading" className="w-full">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6">
        <div>
          <div className="flex items-center gap-2">
            <span className="inline-flex h-7 w-7 items-center justify-center rounded-lg bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300">
              <TrendingUp className="h-4 w-4" />
            </span>
            <h2 id="rates-heading" className="text-xl font-bold tracking-tight text-slate-900 dark:text-white sm:text-2xl">
              Live Currency Exchange Rates
            </h2>
          </div>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Powered by European Central Bank (ECB) via Frankfurter &amp; .NET 9 HybridCache
          </p>
        </div>

        {/* Base Currency Selector & Refresh Button */}
        <div className="flex items-center gap-3">
          <label htmlFor="base-currency-select" className="text-xs font-medium text-slate-600 dark:text-slate-300">
            Base:
          </label>
          <select
            id="base-currency-select"
            value={baseCurrency}
            onChange={(e) => setBaseCurrency(e.target.value)}
            className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-semibold text-slate-800 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            aria-label="Select base currency for exchange rates"
          >
            {COMMON_CURRENCIES.map((curr) => (
              <option key={curr} value={curr}>
                {curr}
              </option>
            ))}
          </select>

          <button
            type="button"
            onClick={() => refetch()}
            disabled={isFetching}
            className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:opacity-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800"
            aria-label="Refresh exchange rates"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
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
            <h3 className="text-sm font-semibold">Failed to fetch exchange rates</h3>
            <p className="mt-1 text-xs">{error?.message || 'Could not connect to the Backend Minimal API.'}</p>
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
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6" aria-label="Loading rates">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-24 animate-pulse rounded-xl bg-slate-200/80 dark:bg-slate-800" />
          ))}
        </div>
      )}

      {/* Rates Cards Grid */}
      {data && !isLoading && (
        <>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
            {COMMON_CURRENCIES.filter((c) => c !== baseCurrency).map((currency) => {
              const rate = rates[currency];
              return (
                <div
                  key={currency}
                  className="group relative overflow-hidden rounded-xl border border-slate-200/80 bg-white p-4 shadow-sm transition hover:border-blue-400 hover:shadow-md dark:border-slate-800 dark:bg-slate-900"
                >
                  <div className="flex items-center justify-between text-xs text-slate-500 dark:text-slate-400 font-mono">
                    <span>
                      {baseCurrency}/{currency}
                    </span>
                    <span className="text-[10px] text-slate-400 uppercase">ECB</span>
                  </div>
                  <div className="mt-2">
                    <span className="text-lg font-bold text-slate-900 dark:text-white">
                      {rate !== undefined ? rate.toFixed(4) : '—'}
                    </span>
                  </div>
                  <div className="mt-1 text-[11px] text-slate-400">
                    1 {baseCurrency} = {rate !== undefined ? `${rate.toFixed(4)} ${currency}` : 'N/A'}
                  </div>
                </div>
              );
            })}
          </div>

          {/* Timestamp and Metadata Badge */}
          <div className="mt-3 flex flex-wrap items-center justify-between gap-2 text-xs text-slate-500 dark:text-slate-400">
            <span className="flex items-center gap-1.5">
              <Clock className="h-3.5 w-3.5 text-slate-400" />
              ECB Reference Date: <strong className="text-slate-700 dark:text-slate-300">{data.referenceDate}</strong>
            </span>
            <span>
              Provider: <strong className="text-slate-700 dark:text-slate-300">{data.provider}</strong> • Cached UTC:{' '}
              {new Date(data.fetchedAtUtc).toLocaleTimeString()}
            </span>
          </div>

          {/* Quick Calculator / Converter Widget */}
          <div className="mt-6 rounded-2xl border border-blue-100 bg-gradient-to-r from-blue-50/70 via-indigo-50/50 to-slate-50 p-5 shadow-sm dark:border-slate-800 dark:from-slate-900 dark:via-blue-950/20 dark:to-slate-900">
            <div className="flex items-center gap-2 mb-3">
              <DollarSign className="h-4 w-4 text-blue-600 dark:text-blue-400" />
              <h3 className="text-sm font-bold text-slate-800 dark:text-slate-200">
                Instant FX Rate Calculator
              </h3>
            </div>
            <div className="flex flex-col sm:flex-row items-center gap-4">
              {/* Amount input */}
              <div className="w-full sm:w-1/3">
                <label htmlFor="calc-amount" className="block text-xs font-medium text-slate-600 dark:text-slate-300 mb-1">
                  Amount ({baseCurrency})
                </label>
                <input
                  id="calc-amount"
                  type="number"
                  min="0.01"
                  step="any"
                  value={calcAmount}
                  onChange={(e) => setCalcAmount(Number(e.target.value) || 0)}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 dark:border-slate-700 dark:bg-slate-800 dark:text-white"
                />
              </div>

              <div className="hidden sm:flex items-center pt-5 text-slate-400">
                <ArrowRight className="h-5 w-5" />
              </div>

              {/* Target currency select */}
              <div className="w-full sm:w-1/4">
                <label htmlFor="calc-target" className="block text-xs font-medium text-slate-600 dark:text-slate-300 mb-1">
                  Convert To
                </label>
                <select
                  id="calc-target"
                  value={calcTarget}
                  onChange={(e) => setCalcTarget(e.target.value)}
                  className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 dark:border-slate-700 dark:bg-slate-800 dark:text-white"
                >
                  {Object.keys(rates).sort().map((curr) => (
                    <option key={curr} value={curr}>
                      {curr}
                    </option>
                  ))}
                </select>
              </div>

              {/* Conversion Output */}
              <div className="w-full sm:flex-1 rounded-xl bg-white p-3 shadow-inner border border-slate-200/80 dark:bg-slate-800/80 dark:border-slate-700/80 sm:mt-4">
                <div className="text-[11px] font-medium text-slate-500 dark:text-slate-400">
                  Estimated Total ({calcTarget})
                </div>
                <div className="text-xl font-extrabold text-blue-600 dark:text-blue-400">
                  {targetRate > 0 ? `${convertedTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 4 })} ${calcTarget}` : '—'}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </section>
  );
}

