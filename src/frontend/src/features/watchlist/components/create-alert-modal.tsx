'use client';

import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { watchItemSchema, type WatchItemFormData } from '../schemas/watch-item-schema';
import { useCreateWatchItem } from '../api/use-watchlist';
import { X, BellPlus, Loader2, AlertCircle } from 'lucide-react';

interface CreateAlertModalProps {
  isOpen: boolean;
  onClose: () => void;
  defaultBase?: string;
}

export function CreateAlertModal({ isOpen, onClose, defaultBase = 'USD' }: CreateAlertModalProps) {
  const { mutate: createAlert, isPending, error, reset: resetMutation } = useCreateWatchItem();

  const {
    register,
    handleSubmit,
    reset: resetForm,
    formState: { errors },
  } = useForm<WatchItemFormData>({
    resolver: zodResolver(watchItemSchema),
    defaultValues: {
      baseCurrency: defaultBase,
      targetCurrency: 'BRL',
      targetRate: 5.5,
      notifyWhenAbove: true,
      notes: '',
    },
  });

  useEffect(() => {
    if (isOpen) {
      resetMutation();
    }
  }, [isOpen, resetMutation]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const onSubmit = (data: WatchItemFormData) => {
    createAlert(
      {
        baseCurrency: data.baseCurrency.toUpperCase(),
        targetCurrency: data.targetCurrency.toUpperCase(),
        targetRate: data.targetRate,
        notifyWhenAbove: data.notifyWhenAbove,
        notes: data.notes || null,
      },
      {
        onSuccess: () => {
          resetForm();
          onClose();
        },
      }
    );
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200"
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-title"
    >
      <div className="relative w-full max-w-lg rounded-2xl border border-slate-200 bg-white p-6 shadow-2xl dark:border-slate-800 dark:bg-slate-900 sm:p-8">
        {/* Close Button */}
        <button
          type="button"
          onClick={onClose}
          className="absolute right-4 top-4 rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
          aria-label="Close modal"
        >
          <X className="h-5 w-5" />
        </button>

        {/* Modal Header */}
        <div className="flex items-center gap-3 mb-6">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-100 text-blue-600 dark:bg-blue-950 dark:text-blue-400">
            <BellPlus className="h-5 w-5" />
          </div>
          <div>
            <h2 id="modal-title" className="text-lg font-bold text-slate-900 dark:text-white">
              Create Currency Watch Alert
            </h2>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              Dispatches an Outbox event to RabbitMQ when rate crosses threshold
            </p>
          </div>
        </div>

        {/* Form Error Display */}
        {error && (
          <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 p-3 text-xs text-red-700 dark:bg-red-950/40 dark:text-red-300">
            <AlertCircle className="h-4 w-4 shrink-0" />
            <span>{error.message || 'Failed to create alert.'}</span>
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            {/* Base Currency */}
            <div>
              <label htmlFor="baseCurrency" className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                Base Currency
              </label>
              <input
                id="baseCurrency"
                type="text"
                maxLength={3}
                placeholder="USD"
                {...register('baseCurrency')}
                className="w-full uppercase rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 dark:border-slate-700 dark:bg-slate-800 dark:text-white"
                aria-invalid={Boolean(errors.baseCurrency)}
                aria-describedby={errors.baseCurrency ? 'baseCurrency-error' : undefined}
              />
              {errors.baseCurrency && (
                <p id="baseCurrency-error" className="mt-1 text-[11px] text-red-600 dark:text-red-400">
                  {errors.baseCurrency.message}
                </p>
              )}
            </div>

            {/* Target Currency */}
            <div>
              <label htmlFor="targetCurrency" className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                Target Currency
              </label>
              <input
                id="targetCurrency"
                type="text"
                maxLength={3}
                placeholder="BRL"
                {...register('targetCurrency')}
                className="w-full uppercase rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 dark:border-slate-700 dark:bg-slate-800 dark:text-white"
                aria-invalid={Boolean(errors.targetCurrency)}
                aria-describedby={errors.targetCurrency ? 'targetCurrency-error' : undefined}
              />
              {errors.targetCurrency && (
                <p id="targetCurrency-error" className="mt-1 text-[11px] text-red-600 dark:text-red-400">
                  {errors.targetCurrency.message}
                </p>
              )}
            </div>
          </div>

          {/* Target Rate */}
          <div>
            <label htmlFor="targetRate" className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              Trigger Rate Threshold
            </label>
            <input
              id="targetRate"
              type="number"
              step="any"
              placeholder="5.50"
              {...register('targetRate', { valueAsNumber: true })}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 dark:border-slate-700 dark:bg-slate-800 dark:text-white"
              aria-invalid={Boolean(errors.targetRate)}
              aria-describedby={errors.targetRate ? 'targetRate-error' : undefined}
            />
            {errors.targetRate && (
              <p id="targetRate-error" className="mt-1 text-[11px] text-red-600 dark:text-red-400">
                {errors.targetRate.message}
              </p>
            )}
          </div>

          {/* Condition: Notify when above / below */}
          <div className="flex items-center gap-3 rounded-xl border border-slate-200/80 bg-slate-50 p-3 dark:border-slate-800 dark:bg-slate-800/50">
            <input
              id="notifyWhenAbove"
              type="checkbox"
              {...register('notifyWhenAbove')}
              className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500 dark:border-slate-600 dark:bg-slate-700"
            />
            <label htmlFor="notifyWhenAbove" className="text-xs font-medium text-slate-700 dark:text-slate-300 cursor-pointer">
              Notify when rate is <strong>greater or equal</strong> (&gt;=) to threshold
            </label>
          </div>

          {/* Notes */}
          <div>
            <label htmlFor="notes" className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              Notes (Optional)
            </label>
            <textarea
              id="notes"
              rows={2}
              placeholder="e.g. Alert for quarterly budget purchase"
              {...register('notes')}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 dark:border-slate-700 dark:bg-slate-800 dark:text-white"
            />
            {errors.notes && (
              <p className="mt-1 text-[11px] text-red-600 dark:text-red-400">{errors.notes.message}</p>
            )}
          </div>

          {/* Action buttons */}
          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              disabled={isPending}
              className="rounded-lg border border-slate-300 px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 disabled:opacity-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-5 py-2 text-xs font-semibold text-white shadow transition hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
            >
              {isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Creating...
                </>
              ) : (
                'Create Alert'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

