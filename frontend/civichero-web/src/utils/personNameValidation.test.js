import { describe, expect, it } from 'vitest';
import { normalizePersonName, validatePersonName } from './personNameValidation.js';

describe('person-name validation', () => {
  it.each([
    'Abhimanyu Patil',
    'Vaishnavi Deore',
    'अभिमन्यु पाटील',
    'Élodie Martin',
  ])('accepts a valid alphabetic name: %s', (name) => {
    expect(validatePersonName(name)).toBe('');
  });

  it.each([
    'Abhimanyu123',
    'John_Doe',
    'Jane@Doe',
    'Test!',
    'A.B.',
  ])('rejects digits or special characters: %s', (name) => {
    expect(validatePersonName(name)).toContain('Digits and special characters');
  });

  it('normalizes leading, trailing and repeated spaces', () => {
    expect(normalizePersonName('  Abhimanyu   Patil  ')).toBe('Abhimanyu Patil');
  });
});
