import { z } from 'zod';

export const watchItemSchema = z.object({
  baseCurrency: z
    .string()
    .min(3, 'Base currency must be 3 characters (e.g. USD)')
    .max(3, 'Base currency must be 3 characters (e.g. USD)')
    .regex(/^[A-Z]{3}$/, 'Must be 3 uppercase letters (e.g. USD, EUR, BRL)'),
  targetCurrency: z
    .string()
    .min(3, 'Target currency must be 3 characters (e.g. BRL)')
    .max(3, 'Target currency must be 3 characters (e.g. BRL)')
    .regex(/^[A-Z]{3}$/, 'Must be 3 uppercase letters (e.g. BRL, EUR, USD)'),
  targetRate: z
    .number({ invalid_type_error: 'Target rate must be a valid number' })
    .positive('Target rate must be greater than zero'),
  notifyWhenAbove: z.boolean().default(true),
  notes: z.string().max(250, 'Notes cannot exceed 250 characters').nullable().optional(),
}).refine((data) => data.baseCurrency !== data.targetCurrency, {
  message: 'Base currency and Target currency must be different',
  path: ['targetCurrency'],
});

export type WatchItemFormData = z.infer<typeof watchItemSchema>;

