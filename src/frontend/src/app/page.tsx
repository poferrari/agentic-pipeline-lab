import React from 'react';
import Link from 'next/link';
import { RatesTicker } from '@/features/rates/components/rates-ticker';
import { WatchlistManager } from '@/features/watchlist/components/watchlist-manager';
import { ArchitectureOverview } from '@/features/architecture/components/architecture-overview';
import { ArrowRight, Bell, Sparkles, Shield, Cpu, ExternalLink } from 'lucide-react';

export default function HomePage() {
  return (
    <div className="flex flex-col gap-12 sm:gap-16 py-8 sm:py-12 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto w-full">
      {/* Hero Section */}
      <section aria-labelledby="hero-title" className="relative overflow-hidden text-center sm:text-left pt-2 pb-4">
        <div className="flex flex-col lg:flex-row items-center justify-between gap-8">
          <div className="max-w-2xl">
            {/* Pill Badge */}
            <div className="inline-flex items-center gap-2 rounded-full border border-blue-200/80 bg-blue-50/80 px-3 py-1 text-xs font-semibold text-blue-700 shadow-sm backdrop-blur dark:border-blue-900/60 dark:bg-blue-950/40 dark:text-blue-300">
              <Sparkles className="h-3.5 w-3.5" />
              <span>Full-Stack Monorepo • End-to-End Type Safety</span>
            </div>

            {/* Headline */}
            <h1
              id="hero-title"
              className="mt-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white sm:text-5xl lg:text-5xl"
            >
              Real-Time FX Rates &amp;{' '}
              <span className="bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
                Transactional Outbox
              </span>{' '}
              Alerts
            </h1>

            <p className="mt-4 text-base sm:text-lg text-slate-600 dark:text-slate-300 leading-relaxed">
              Seamlessly monitor foreign exchange rates backed by the European Central Bank. Create custom threshold
              triggers dispatched asynchronously to RabbitMQ via MassTransit Outbox with Supabase tenancy.
            </p>

            {/* Quick Action Buttons */}
            <div className="mt-6 flex flex-wrap items-center justify-center sm:justify-start gap-3">
              <Link
                href="#rates"
                className="inline-flex items-center gap-2 rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-md shadow-blue-500/25 transition hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              >
                <span>Explore Live Rates</span>
                <ArrowRight className="h-4 w-4" />
              </Link>
              <Link
                href="#watchlist"
                className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-sm font-semibold text-slate-800 shadow-sm transition hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-slate-400 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                <Bell className="h-4 w-4 text-blue-500" />
                <span>Configure Alerts</span>
              </Link>
              <a
                href="https://agentic-pipeline-lab-api.onrender.com/scalar/v1"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-slate-500 hover:text-blue-600 dark:text-slate-400 dark:hover:text-blue-400"
              >
                <span>Interactive OpenAPI (Scalar)</span>
                <ExternalLink className="h-3.5 w-3.5" />
              </a>
            </div>
          </div>

          {/* Quick Stats Highlights */}
          <div className="grid grid-cols-2 gap-4 w-full lg:w-auto shrink-0">
            <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
              <div className="flex items-center gap-2 text-blue-600 dark:text-blue-400">
                <Cpu className="h-5 w-5" />
                <span className="text-xs font-semibold uppercase tracking-wider">C# Minimal API</span>
              </div>
              <div className="mt-2 text-2xl font-black text-slate-900 dark:text-white">.NET 9</div>
              <div className="text-xs text-slate-500 dark:text-slate-400">AOT &amp; HybridCache L1/L2</div>
            </div>

            <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
              <div className="flex items-center gap-2 text-indigo-600 dark:text-indigo-400">
                <Shield className="h-5 w-5" />
                <span className="text-xs font-semibold uppercase tracking-wider">Auth &amp; RLS</span>
              </div>
              <div className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Supabase</div>
              <div className="text-xs text-slate-500 dark:text-slate-400">Asymmetric JWKS JWT</div>
            </div>
          </div>
        </div>
      </section>

      {/* Feature 1: Live Rates Ticker */}
      <RatesTicker />

      {/* Feature 2: Watchlist Alerts Manager */}
      <WatchlistManager />

      {/* Feature 3: Architecture & System Design Overview */}
      <ArchitectureOverview />
    </div>
  );
}

