import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthProvider, useAuth } from '../auth-provider';

const getSessionMock = vi.fn();
const unsubscribeMock = vi.fn();
const onAuthStateChangeMock = vi.fn();
const getClientMock = vi.fn();

vi.mock('@/lib/supabase/client', () => ({
  getSupabaseBrowserClient: () => getClientMock(),
}));

function AuthProbe() {
  const { isLoading, isAuthenticated, user } = useAuth();
  return (
    <div>
      <span data-testid="loading">{String(isLoading)}</span>
      <span data-testid="authenticated">{String(isAuthenticated)}</span>
      <span data-testid="email">{user?.email ?? 'none'}</span>
    </div>
  );
}

const renderProvider = () =>
  render(
    <AuthProvider>
      <AuthProbe />
    </AuthProvider>,
  );

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    onAuthStateChangeMock.mockReturnValue({ data: { subscription: { unsubscribe: unsubscribeMock } } });
    getClientMock.mockReturnValue({
      auth: { getSession: getSessionMock, onAuthStateChange: onAuthStateChangeMock },
    });
  });

  it('exposes the signed-in user once the initial session resolves', async () => {
    getSessionMock.mockResolvedValue({ data: { session: { user: { email: 'ana@example.com' } } } });

    renderProvider();

    await waitFor(() => expect(screen.getByTestId('loading').textContent).toBe('false'));
    expect(screen.getByTestId('authenticated').textContent).toBe('true');
    expect(screen.getByTestId('email').textContent).toBe('ana@example.com');
  });

  it('stays signed out and stops loading when the Supabase client cannot be created', async () => {
    getClientMock.mockImplementation(() => {
      throw new Error('Supabase is not configured');
    });

    renderProvider();

    await waitFor(() => expect(screen.getByTestId('loading').textContent).toBe('false'));
    expect(screen.getByTestId('authenticated').textContent).toBe('false');
  });

  it('stops loading and stays signed out when reading the initial session fails', async () => {
    getSessionMock.mockRejectedValue(new Error('network down'));

    renderProvider();

    await waitFor(() => expect(screen.getByTestId('loading').textContent).toBe('false'));
    expect(screen.getByTestId('authenticated').textContent).toBe('false');
  });

  it('unsubscribes from auth state changes on unmount', async () => {
    getSessionMock.mockResolvedValue({ data: { session: null } });

    const { unmount } = renderProvider();
    await waitFor(() => expect(screen.getByTestId('loading').textContent).toBe('false'));
    unmount();

    expect(unsubscribeMock).toHaveBeenCalledTimes(1);
  });
});
