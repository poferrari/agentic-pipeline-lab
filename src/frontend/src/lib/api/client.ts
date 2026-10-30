import createClient from 'openapi-fetch';
import type { paths } from './schema';
import type { ProblemDetails } from './types';
import { getSupabaseBrowserClient } from '@/lib/supabase/client';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'https://agentic-pipeline-lab-api.onrender.com';

export class ApiError extends Error {
  constructor(public readonly problem: ProblemDetails) {
    super(problem.detail || problem.title || 'API Error');
    this.name = 'ApiError';
  }
}

/**
 * Low-level typed openapi-fetch client synchronized with C# Minimal API.
 */
export const apiClient = createClient<paths>({
  baseUrl: API_BASE_URL,
});

/**
 * High-level fetch wrapper injecting Supabase Bearer JWT / Demo Header
 * and parsing RFC 7807 ProblemDetails.
 */
export async function apiFetch<TResponse>(
  endpoint: string,
  options: RequestInit = {}
): Promise<TResponse> {
  const headers = new Headers(options.headers);
  headers.set('Content-Type', 'application/json');

  try {
    const supabase = getSupabaseBrowserClient();
    const { data: { session } } = await supabase.auth.getSession();
    if (session?.access_token) {
      headers.set('Authorization', `Bearer ${session.access_token}`);
    } else {
      // Graceful fallback to demo identity supported by backend Auth:AllowDemoHeader
      headers.set('X-Demo-User-Id', 'demo-user-web');
    }
  } catch {
    headers.set('X-Demo-User-Id', 'demo-user-web');
  }

  const url = endpoint.startsWith('http') ? endpoint : `${API_BASE_URL}${endpoint}`;
  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    let problem: ProblemDetails;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = {
        title: 'Network / Server Error',
        status: response.status,
        detail: response.statusText || 'Failed to process request',
      };
    }
    throw new ApiError(problem);
  }

  if (response.status === 204) {
    return undefined as unknown as TResponse;
  }

  return (await response.json()) as TResponse;
}

