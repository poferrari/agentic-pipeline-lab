import React from 'react';
import type { Metadata, Viewport } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { QueryProvider } from '@/providers/query-provider';
import { AuthProvider } from '@/providers/auth-provider';
import { Header } from '@/components/layout/header';
import { Footer } from '@/components/layout/footer';
import dynamic from 'next/dynamic';

const AiChatDrawer = dynamic(
  () => import('@/features/ai/components/ai-chat-drawer').then((mod) => mod.AiChatDrawer),
  { ssr: false }
);

const inter = Inter({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-inter',
});

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || 'https://agentic-pipeline-lab-web.onrender.com';

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: 'Agentic FX | Real-Time Exchange Rates & Outbox Watchlist Alerts',
    template: '%s | Agentic FX',
  },
  description:
    'High-performance foreign exchange rates and automated threshold alerts powered by C# Minimal API (.NET 9), Hexagonal Architecture, RabbitMQ MassTransit Outbox, and Next.js 14.',
  keywords: [
    'Exchange Rates',
    'Forex',
    'Currency Alerts',
    'Frankfurter API',
    'ECB Rates',
    'MassTransit',
    'RabbitMQ',
    'Outbox Pattern',
    'C# Minimal API',
    'Next.js 14',
    'Supabase Auth',
  ],
  authors: [{ name: 'Agentic Enterprise Pipeline Team' }],
  creator: 'poferrari',
  publisher: 'Agentic Enterprise Pipeline',
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  openGraph: {
    type: 'website',
    locale: 'en_US',
    url: siteUrl,
    title: 'Agentic FX | Real-Time Exchange Rates & Outbox Alerts',
    description:
      'Monitor real-time currency pairs with European Central Bank rates and trigger asynchronous RabbitMQ Outbox events on rate thresholds.',
    siteName: 'Agentic FX',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'Agentic FX | Real-Time Exchange Rates & Outbox Alerts',
    description:
      'Real-time forex rates and automated currency threshold alerts with RabbitMQ and C# .NET 9.',
  },
};

export const viewport: Viewport = {
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#ffffff' },
    { media: '(prefers-color-scheme: dark)', color: '#020617' },
  ],
  width: 'device-width',
  initialScale: 1,
  maximumScale: 5,
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className={inter.variable}>
      <head>
        <link rel="icon" href="/favicon.ico" sizes="any" />
      </head>
      <body className="flex min-h-screen flex-col font-sans antialiased text-slate-900 dark:text-slate-100 bg-slate-50/50 dark:bg-slate-950">
        <QueryProvider>
          <AuthProvider>
            <Header />
            <main className="flex-1 w-full" id="main-content">
              {children}
            </main>
            <AiChatDrawer />
            <Footer />
          </AuthProvider>
        </QueryProvider>
      </body>
    </html>
  );
}

