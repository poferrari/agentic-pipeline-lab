'use client';

import dynamic from 'next/dynamic';

// `ssr: false` is only allowed inside a Client Component (Next.js 15+), so the lazy boundary lives here.
export const AiChatDrawerLazy = dynamic(
  () => import('./ai-chat-drawer').then((mod) => mod.AiChatDrawer),
  { ssr: false },
);
