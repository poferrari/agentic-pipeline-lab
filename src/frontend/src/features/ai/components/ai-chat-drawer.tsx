'use client';

import React, { useState, useRef, useEffect } from 'react';
import { Sparkles, Bot, User, Send, X, RotateCcw, AlertCircle, ChevronDown } from 'lucide-react';
import { useAiAdvisor } from '../api/use-ai-chat';
import type { AiChatMessage } from '../api/types';

interface DisplayMessage extends AiChatMessage {
  id: string;
  suggestedActions?: string[];
  modelUsed?: string;
  isError?: boolean;
}

const INITIAL_SUGGESTIONS = [
  'What is the current USD/EUR rate?',
  'How do Outbox alerts work?',
  'Explain the Hexagonal Architecture',
  'Is my data protected under LGPD?',
];

export function AiChatDrawer() {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState<DisplayMessage[]>([
    {
      id: 'welcome',
      role: 'assistant',
      content:
        'Hello! I am the MarketPulse AI Advisor. Ask me anything about live foreign exchange rates, currency alerts (Transactional Outbox), or our Hexagonal Architecture. (Note: questions outside this domain will be politely declined).',
      suggestedActions: INITIAL_SUGGESTIONS,
      modelUsed: 'MarketPulse Domain Guard',
    },
  ]);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const mutation = useAiAdvisor();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
      setTimeout(() => inputRef.current?.focus(), 150);
    }
  }, [isOpen, messages]);

  const handleSend = (textToSend?: string) => {
    const raw = textToSend ?? input;
    const trimmed = raw.trim();
    if (!trimmed || mutation.isPending) return;

    if (trimmed.length > 500) {
      return;
    }

    const userMsgId = `user-${Date.now()}`;
    const newMessages: DisplayMessage[] = [
      ...messages,
      { id: userMsgId, role: 'user', content: trimmed },
    ];
    setMessages(newMessages);
    setInput('');

    // Prepare history payload for API (excluding initial greeting to save tokens)
    const historyPayload: AiChatMessage[] = newMessages
      .filter((m) => m.id !== 'welcome' && !m.isError)
      .map((m) => ({ role: m.role, content: m.content }));

    mutation.mutate(
      {
        message: trimmed,
        history: historyPayload.slice(0, -1), // send previous messages as history
      },
      {
        onSuccess: (data) => {
          setMessages((prev) => [
            ...prev,
            {
              id: `assistant-${Date.now()}`,
              role: 'assistant',
              content: data.reply,
              suggestedActions: data.suggestedActions,
              modelUsed: data.modelUsed,
            },
          ]);
        },
        onError: (err) => {
          setMessages((prev) => [
            ...prev,
            {
              id: `err-${Date.now()}`,
              role: 'assistant',
              content: `Error: ${err.message || 'Unable to connect to AI Advisor.'}`,
              isError: true,
              suggestedActions: ['Try again', 'Check USD/BRL rate'],
            },
          ]);
        },
      }
    );
  };

  const handleClearHistory = () => {
    setMessages([
      {
        id: 'welcome',
        role: 'assistant',
        content:
          'Conversation reset. How can I assist you with FX rates, currency alerts, or architecture today?',
        suggestedActions: INITIAL_SUGGESTIONS,
        modelUsed: 'MarketPulse Domain Guard',
      },
    ]);
  };

  return (
    <>
      {/* Floating Trigger Button */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="fixed bottom-6 right-6 z-50 flex items-center gap-2.5 px-4 py-3 bg-gradient-to-r from-blue-600 via-indigo-600 to-purple-600 text-white rounded-full shadow-2xl hover:scale-105 active:scale-95 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-indigo-400 group"
          aria-label="Open AI Contextual Advisor"
        >
          <div className="relative">
            <Sparkles className="w-5 h-5 animate-pulse text-amber-300" />
            <span className="absolute -top-1 -right-1 flex h-2.5 w-2.5">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500"></span>
            </span>
          </div>
          <span className="font-semibold text-sm tracking-wide hidden sm:inline">
            Ask AI Advisor
          </span>
        </button>
      )}

      {/* Floating Chat Drawer / Popover */}
      {isOpen && (
        <div
          role="dialog"
          aria-label="MarketPulse AI Advisor Chat"
          className="fixed bottom-4 right-4 z-50 w-[95vw] sm:w-[440px] max-w-[460px] h-[600px] max-h-[85vh] bg-slate-900 border border-slate-700/80 rounded-2xl shadow-2xl flex flex-col overflow-hidden text-slate-100 backdrop-blur-xl animate-in fade-in slide-in-from-bottom-4 duration-200"
        >
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 bg-slate-800/90 border-b border-slate-700">
            <div className="flex items-center gap-2.5">
              <div className="p-2 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-xl text-white shadow-inner">
                <Bot className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-sm font-bold text-white flex items-center gap-1.5">
                  MarketPulse AI Advisor
                  <span className="text-[10px] px-1.5 py-0.5 font-normal rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                    Bounded
                  </span>
                </h2>
                <p className="text-[11px] text-slate-400">FX Rates & Hexagonal Architecture</p>
              </div>
            </div>

            <div className="flex items-center gap-1">
              <button
                onClick={handleClearHistory}
                title="Reset conversation"
                className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-700/60 rounded-lg transition-colors"
                aria-label="Reset conversation"
              >
                <RotateCcw className="w-4 h-4" />
              </button>
              <button
                onClick={() => setIsOpen(false)}
                title="Close chat"
                className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-700/60 rounded-lg transition-colors"
                aria-label="Close AI Assistant"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Messages Area */}
          <div className="flex-1 overflow-y-auto p-4 space-y-3.5 scrollbar-thin scrollbar-thumb-slate-700">
            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`flex gap-2.5 ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
              >
                {msg.role === 'assistant' && (
                  <div className="w-7 h-7 rounded-lg bg-indigo-600/30 border border-indigo-500/40 flex items-center justify-center shrink-0 mt-0.5 text-indigo-300">
                    <Bot className="w-4 h-4" />
                  </div>
                )}

                <div
                  className={`max-w-[85%] rounded-2xl px-3.5 py-2.5 text-sm leading-relaxed ${
                    msg.role === 'user'
                      ? 'bg-blue-600 text-white rounded-tr-xs'
                      : msg.isError
                      ? 'bg-rose-950/60 border border-rose-800 text-rose-200 rounded-tl-xs'
                      : 'bg-slate-800/90 border border-slate-700/70 text-slate-200 rounded-tl-xs'
                  }`}
                >
                  <div className="whitespace-pre-wrap">{msg.content}</div>

                  {msg.modelUsed && (
                    <div className="mt-1.5 pt-1.5 border-t border-slate-700/50 flex items-center justify-between text-[10px] text-slate-400">
                      <span>Model: {msg.modelUsed}</span>
                    </div>
                  )}

                  {/* Suggested action chips */}
                  {msg.suggestedActions && msg.suggestedActions.length > 0 && (
                    <div className="mt-2.5 pt-2 border-t border-slate-700/60 space-y-1">
                      <p className="text-[11px] font-semibold text-slate-400">Suggested queries:</p>
                      <div className="flex flex-wrap gap-1.5">
                        {msg.suggestedActions.map((action, idx) => (
                          <button
                            key={idx}
                            onClick={() => handleSend(action)}
                            disabled={mutation.isPending}
                            className="text-left text-[11px] px-2 py-1 bg-slate-700/60 hover:bg-indigo-600/40 hover:text-indigo-200 text-slate-300 border border-slate-600/40 rounded-lg transition-colors disabled:opacity-50"
                          >
                            {action}
                          </button>
                        ))}
                      </div>
                    </div>
                  )}
                </div>

                {msg.role === 'user' && (
                  <div className="w-7 h-7 rounded-lg bg-blue-500/20 border border-blue-400/30 flex items-center justify-center shrink-0 mt-0.5 text-blue-300">
                    <User className="w-4 h-4" />
                  </div>
                )}
              </div>
            ))}

            {mutation.isPending && (
              <div className="flex gap-2.5 justify-start">
                <div className="w-7 h-7 rounded-lg bg-indigo-600/30 border border-indigo-500/40 flex items-center justify-center shrink-0 mt-0.5 text-indigo-300">
                  <Bot className="w-4 h-4" />
                </div>
                <div className="bg-slate-800 border border-slate-700 rounded-2xl px-4 py-2.5 text-xs text-slate-400 flex items-center gap-2">
                  <span className="flex gap-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-indigo-400 animate-bounce"></span>
                    <span className="w-1.5 h-1.5 rounded-full bg-indigo-400 animate-bounce [animation-delay:0.2s]"></span>
                    <span className="w-1.5 h-1.5 rounded-full bg-indigo-400 animate-bounce [animation-delay:0.4s]"></span>
                  </span>
                  <span>Analyzing FX market & architecture...</span>
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* Input Footer */}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
            className="p-3 bg-slate-800/90 border-t border-slate-700"
          >
            <div className="flex items-center gap-2">
              <input
                ref={inputRef}
                type="text"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                maxLength={500}
                placeholder="Ask about FX rates, alerts, architecture..."
                disabled={mutation.isPending}
                className="flex-1 bg-slate-900 border border-slate-700 rounded-xl px-3.5 py-2.5 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50"
              />
              <button
                type="submit"
                disabled={!input.trim() || mutation.isPending}
                className="p-2.5 bg-indigo-600 hover:bg-indigo-500 active:scale-95 text-white rounded-xl transition-all disabled:opacity-40 disabled:cursor-not-allowed"
                aria-label="Send message to AI Advisor"
              >
                <Send className="w-4 h-4" />
              </button>
            </div>
            <div className="flex justify-between items-center mt-1.5 px-1 text-[10px] text-slate-500">
              <span>Strict domain guardrails active</span>
              <span>{input.length}/500</span>
            </div>
          </form>
        </div>
      )}
    </>
  );
}

