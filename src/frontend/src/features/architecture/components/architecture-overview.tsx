import React from 'react';
import { Layers, Network, ShieldCheck, Zap, Server, Activity } from 'lucide-react';

export function ArchitectureOverview() {
  return (
    <section id="architecture" aria-labelledby="arch-heading" className="w-full">
      <div className="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900 sm:p-8">
        <div className="flex items-center gap-2 mb-2">
          <span className="inline-flex h-7 w-7 items-center justify-center rounded-lg bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300">
            <Layers className="h-4 w-4" />
          </span>
          <h2 id="arch-heading" className="text-xl font-bold tracking-tight text-slate-900 dark:text-white sm:text-2xl">
            Hexagonal &amp; Event-Driven Architecture
          </h2>
        </div>
        <p className="text-sm text-slate-500 dark:text-slate-400 max-w-2xl">
          Clean architecture decoupled from external frameworks, delivering resilient asynchronous messaging and real-time observability.
        </p>

        <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div className="rounded-xl border border-slate-100 bg-slate-50/70 p-4 dark:border-slate-800 dark:bg-slate-800/40">
            <div className="flex items-center gap-2 text-blue-600 dark:text-blue-400 font-semibold text-sm">
              <Zap className="h-4 w-4" />
              <span>Minimal API (.NET 9)</span>
            </div>
            <p className="mt-2 text-xs text-slate-600 dark:text-slate-400">
              Native AOT-ready ASP.NET Core Minimal APIs with TypedResults, RFC 7807 ProblemDetails, and Scalar OpenAPI.
            </p>
          </div>

          <div className="rounded-xl border border-slate-100 bg-slate-50/70 p-4 dark:border-slate-800 dark:bg-slate-800/40">
            <div className="flex items-center gap-2 text-indigo-600 dark:text-indigo-400 font-semibold text-sm">
              <Network className="h-4 w-4" />
              <span>RabbitMQ &amp; Outbox</span>
            </div>
            <p className="mt-2 text-xs text-slate-600 dark:text-slate-400">
              Guaranteed at-least-once delivery with MassTransit Transactional Outbox, preventing dual-write inconsistencies.
            </p>
          </div>

          <div className="rounded-xl border border-slate-100 bg-slate-50/70 p-4 dark:border-slate-800 dark:bg-slate-800/40">
            <div className="flex items-center gap-2 text-emerald-700 dark:text-emerald-400 font-semibold text-sm">
              <ShieldCheck className="h-4 w-4" />
              <span>Supabase Auth &amp; RLS</span>
            </div>
            <p className="mt-2 text-xs text-slate-600 dark:text-slate-400">
              Asymmetric JWKS Bearer token verification with OWASP BOLA mitigation and row-level tenant protection.
            </p>
          </div>

          <div className="rounded-xl border border-slate-100 bg-slate-50/70 p-4 dark:border-slate-800 dark:bg-slate-800/40">
            <div className="flex items-center gap-2 text-purple-600 dark:text-purple-400 font-semibold text-sm">
              <Activity className="h-4 w-4" />
              <span>SRE Observability</span>
            </div>
            <p className="mt-2 text-xs text-slate-600 dark:text-slate-400">
              OpenTelemetry traces, Prometheus metrics, and Grafana Cloud Synthetics browser monitoring.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

