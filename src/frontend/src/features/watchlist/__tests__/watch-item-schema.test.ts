import { describe, it, expect } from 'vitest';
import { watchItemSchema } from '../schemas/watch-item-schema';

describe('watchItemSchema validation', () => {
  it('should accept valid watch item input', () => {
    const input = {
      baseCurrency: 'USD',
      targetCurrency: 'BRL',
      targetRate: 5.45,
      notifyWhenAbove: true,
      notes: 'Test budget trigger',
    };

    const result = watchItemSchema.safeParse(input);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.baseCurrency).toBe('USD');
      expect(result.data.targetCurrency).toBe('BRL');
      expect(result.data.targetRate).toBe(5.45);
    }
  });

  it('should reject when baseCurrency and targetCurrency are identical', () => {
    const input = {
      baseCurrency: 'USD',
      targetCurrency: 'USD',
      targetRate: 1.0,
      notifyWhenAbove: true,
    };

    const result = watchItemSchema.safeParse(input);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('must be different');
    }
  });

  it('should reject invalid currency codes', () => {
    const input = {
      baseCurrency: 'US',
      targetCurrency: 'EURO',
      targetRate: 1.0,
      notifyWhenAbove: true,
    };

    const result = watchItemSchema.safeParse(input);
    expect(result.success).toBe(false);
  });

  it('should reject negative or zero targetRate', () => {
    const input = {
      baseCurrency: 'USD',
      targetCurrency: 'BRL',
      targetRate: -2.5,
      notifyWhenAbove: true,
    };

    const result = watchItemSchema.safeParse(input);
    expect(result.success).toBe(false);
  });
});

