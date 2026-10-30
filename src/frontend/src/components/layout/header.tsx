'use client';

import React from 'react';
import Link from 'next/link';
import { useAuth } from '@/providers/auth-provider';
import { ArrowLeftRight, Bell, Shield, LogIn, LogOut, User, Activity } from 'lucide-react';

export function Header() {
  const { user, isDemoMode, isLoading, resetDemoSession, signOut } = useAuth();

  return (
    <header className="sticky top-0 z-40 w-full border-b border-slate-200 bg-white/95 backdrop-blur-md supports-[backdrop-filter]:bg-white/80 dark:border-slate-800 dark:bg-slate-950/90">
      <div className="container mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand Logo & Name */}
        <Link
          href="/"
          className="flex items-center gap-2.5 transition-opacity hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded-lg"
          aria-label="Agentic FX Home"
        >
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-500 text-white shadow-md shadow-blue-500/20">
            <ArrowLeftRight className="h-5 w-5" aria-hidden="true" />
          </div>
          <div>
            <div className="flex items-center gap-1.5">
              <span className="text-lg font-bold tracking-tight text-slate-900 dark:text-white">
                Agentic <span className="text-blue-600 dark:text-blue-400">FX</span>
              </span>
              <span className="inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300">
                <span className="mr-1 h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
                Live
              </span>
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400 hidden sm:block">
              Foreign Exchange & Outbox Alerts
            </p>
          </div>
        </Link>

        {/* Navigation Links */}
        <nav className="hidden md:flex items-center gap-6" aria-label="Main Navigation">
          <Link
            href="#rates"
            className="text-sm font-medium text-slate-600 transition-colors hover:text-blue-600 dark:text-slate-300 dark:hover:text-blue-400"
          >
            Live Rates
          </Link>
          <Link
            href="#watchlist"
            className="text-sm font-medium text-slate-600 transition-colors hover:text-blue-600 dark:text-slate-300 dark:hover:text-blue-400 flex items-center gap-1.5"
          >
            <Bell className="h-4 w-4" aria-hidden="true" />
            Watchlist Alerts
          </Link>
          <Link
            href="#architecture"
            className="text-sm font-medium text-slate-600 transition-colors hover:text-blue-600 dark:text-slate-300 dark:hover:text-blue-400 flex items-center gap-1.5"
          >
            <Activity className="h-4 w-4" aria-hidden="true" />
            Hexagonal & EDA
          </Link>
        </nav>

        {/* Auth / Demo Mode Badge */}
        <div className="flex items-center gap-3">
          {isLoading ? (
            <div className="h-8 w-24 animate-pulse rounded-md bg-slate-200 dark:bg-slate-800" />
          ) : user ? (
            <div className="flex items-center gap-3">
              <div className="hidden sm:flex flex-col text-right">
                <span className="text-xs font-medium text-slate-900 dark:text-slate-100 truncate max-w-[140px]">
                  {user.email || 'Supabase User'}
                </span>
                <span className="text-[10px] text-emerald-600 dark:text-emerald-400 flex items-center justify-end gap-1">
                  <Shield className="h-3 w-3" /> Supabase JWT
                </span>
              </div>
              <button
                type="button"
                onClick={signOut}
                className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                aria-label="Sign out of Supabase"
              >
                <LogOut className="h-3.5 w-3.5" aria-hidden="true" />
                <span className="hidden sm:inline">Sign Out</span>
              </button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <div
                className="flex items-center gap-1.5 rounded-full bg-blue-50 px-2.5 py-1 text-xs font-medium text-blue-700 dark:bg-blue-950/50 dark:text-blue-300"
                title="Requests are authenticated via backend demo header and Supabase RLS"
              >
                <User className="h-3.5 w-3.5" aria-hidden="true" />
                <span className="font-semibold">Demo Sandbox</span>
              </div>
              <button
                type="button"
                onClick={resetDemoSession}
                className="inline-flex items-center gap-1.5 rounded-lg bg-blue-600 px-3 py-1.5 text-xs font-semibold text-white shadow transition hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                aria-label="Refresh demo session"
              >
                <LogIn className="h-3.5 w-3.5" aria-hidden="true" />
                <span className="hidden sm:inline">Reset Token</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
