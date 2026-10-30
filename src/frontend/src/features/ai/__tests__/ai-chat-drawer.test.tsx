import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AiChatDrawer } from '../components/ai-chat-drawer';

// Mock scrollIntoView
window.HTMLElement.prototype.scrollIntoView = vi.fn();

const mockMutate = vi.fn();

vi.mock('../api/use-ai-chat', () => ({
  useAiAdvisor: () => ({
    mutate: mockMutate,
    isPending: false,
    isError: false,
    error: null,
  }),
}));

describe('AiChatDrawer Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <AiChatDrawer />
      </QueryClientProvider>
    );

  it('renders the floating open button initially', () => {
    renderComponent();

    const triggerBtn = screen.getByRole('button', { name: /Open AI Contextual Advisor/i });
    expect(triggerBtn).toBeInTheDocument();
    expect(screen.getByText('Ask AI Advisor')).toBeInTheDocument();
  });

  it('opens the chat dialog when trigger is clicked', () => {
    renderComponent();

    const triggerBtn = screen.getByRole('button', { name: /Open AI Contextual Advisor/i });
    fireEvent.click(triggerBtn);

    expect(screen.getByRole('dialog', { name: /MarketPulse AI Advisor Chat/i })).toBeInTheDocument();
    expect(screen.getByText('MarketPulse AI Advisor')).toBeInTheDocument();
    expect(screen.getByText(/Hello! I am the MarketPulse AI Advisor/i)).toBeInTheDocument();
  });

  it('displays suggested initial queries in dialog', () => {
    renderComponent();

    fireEvent.click(screen.getByRole('button', { name: /Open AI Contextual Advisor/i }));

    expect(screen.getByRole('button', { name: 'What is the current USD/EUR rate?' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'How do Outbox alerts work?' })).toBeInTheDocument();
  });

  it('closes dialog when close button is clicked', () => {
    renderComponent();

    fireEvent.click(screen.getByRole('button', { name: /Open AI Contextual Advisor/i }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();

    const closeBtn = screen.getByRole('button', { name: /Close AI Assistant/i });
    fireEvent.click(closeBtn);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('submits a prompt and calls mutate', () => {
    renderComponent();

    fireEvent.click(screen.getByRole('button', { name: /Open AI Contextual Advisor/i }));

    const input = screen.getByPlaceholderText(/Ask about FX rates, alerts, architecture.../i);
    fireEvent.change(input, { target: { value: 'Explain Hexagonal Architecture' } });

    const sendBtn = screen.getByRole('button', { name: /Send message to AI Advisor/i });
    fireEvent.click(sendBtn);

    expect(mockMutate).toHaveBeenCalledWith(
      expect.objectContaining({
        message: 'Explain Hexagonal Architecture',
      }),
      expect.any(Object)
    );
  });
});
