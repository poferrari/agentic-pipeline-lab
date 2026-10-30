import React from 'react';
import Link from 'next/link';
import { Github, Cpu, Database, CheckCircle2, Heart } from 'lucide-react';

export function Footer() {
  return (
    <footer className="w-full border-t border-slate-200 bg-slate-50 text-slate-600 dark:border-slate-800 dark:bg-slate-900/50 dark:text-slate-400">
      <div className="container mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-8 md:grid-cols-4">
          {/* Brand & Description */}
          <div className="md:col-span-2">
            <div className="flex items-center gap-2">
              <span className="text-base font-bold text-slate-900 dark:text-white">
                Agentic Enterprise Pipeline
              </span>
              <span className="rounded bg-blue-100 px-1.5 py-0.5 text-[10px] font-semibold text-blue-800 dark:bg-blue-950 dark:text-blue-300">
                MVP v1.0
              </span>
            </div>
            <p className="mt-3 max-w-md text-sm leading-relaxed text-slate-600 dark:text-slate-400">
              High-performance FX quote aggregator and currency alert watcher built with ASP.NET Core 9 Minimal API,
              Hexagonal Architecture, RabbitMQ MassTransit Outbox, Supabase Auth/Postgres, and Next.js 14 App Router.
            </p>
            <div className="mt-4 flex items-center gap-3 text-xs text-slate-500">
              <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium">
                <CheckCircle2 className="h-4 w-4" /> WCAG 2.1 AA Accessible
              </span>
              <span>•</span>
              <span>Mobile-First Responsive</span>
              <span>•</span>
              <span>OpenAPI Type Safe</span>
            </div>
          </div>

          {/* Architecture & Tech Stack */}
          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-slate-900 dark:text-slate-200">
              Backend Architecture
            </h3>
            <ul className="mt-3 space-y-2 text-sm">
              <li className="flex items-center gap-2">
                <Cpu className="h-3.5 w-3.5 text-blue-500" />
                <span>C# Minimal API (.NET 9)</span>
              </li>
              <li className="flex items-center gap-2">
                <Database className="h-3.5 w-3.5 text-indigo-500" />
                <span>PostgreSQL & EF Core 9</span>
              </li>
              <li>MassTransit & RabbitMQ Outbox</li>
              <li>HybridCache (.NET 9 L1/L2)</li>
              <li>OpenTelemetry & RFC 7807</li>
            </ul>
          </div>

          {/* Frontend & Links */}
          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-slate-900 dark:text-slate-200">
              Frontend & Cloud
            </h3>
            <ul className="mt-3 space-y-2 text-sm">
              <li>Next.js 14 (App Router)</li>
              <li>TypeScript & TanStack Query v5</li>
              <li>Tailwind CSS & Zod Form Validation</li>
              <li>
                <a
                  href="https://github.com/poferrari/agentic-pipeline-lab"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 text-blue-600 hover:underline dark:text-blue-400"
                >
                  <Github className="h-4 w-4" /> GitHub Repository
                </a>
              </li>
              <li>
                <a
                  href="https://agentic-pipeline-lab-api.onrender.com/scalar/v1"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 hover:underline dark:text-blue-400"
                >
                  Scalar API Docs
                </a>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-10 border-t border-slate-200 pt-6 text-center text-xs text-slate-500 dark:border-slate-800 dark:text-slate-500 sm:flex sm:items-center sm:justify-between">
          <p>© {new Date().getFullYear()} Agentic Enterprise Pipeline. Released under MIT License.</p>
          <p className="mt-2 sm:mt-0 flex items-center justify-center gap-1">
            Engineered with <Heart className="h-3 w-3 text-red-500 fill-red-500" /> by Autonomous Multi-Agent Pipeline
          </p>
        </div>
      </div>
    </footer>
  );
}

