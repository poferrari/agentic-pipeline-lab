import { createBrowserClient } from '@supabase/ssr';
import type { SupabaseClient } from '@supabase/supabase-js';

const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL || 'https://jrhpbdftohjeymzwkbee.supabase.co';
const supabaseAnonKey =
  process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY ||
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImpyaHBiZGZ0b2hqZXltendrYmVlIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc3Njg3MzIsImV4cCI6MjA5MzM0NDczMn0.xvk0ujSYccemeaC63Ht6mSTp8KnAj85VVyVFGW0h0pg';

let client: SupabaseClient | null = null;

export function getSupabaseBrowserClient(): SupabaseClient {
  if (!client) {
    client = createBrowserClient(supabaseUrl, supabaseAnonKey);
  }
  return client;
}

