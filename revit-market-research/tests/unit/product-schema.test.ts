import { expect, it } from 'vitest';

import { ProductSchema } from '../../src/models/product.js';

it('accepts the required product contract', () => {
  expect(ProductSchema.parse({
    name: 'Example Add-in',
    company: 'Example',
    url: 'https://example.com',
    description: 'Example public product.',
    features: ['Batch Naming'],
    category: 'BIM Productivity',
    target_user: 'BIM managers',
    pricing: 'unknown',
    revit_version: 'unknown',
    commercial_or_free: 'unknown',
    source: 'https://example.com',
  }).name).toBe('Example Add-in');
});

it('rejects fields outside the product output contract', () => {
  expect(() => ProductSchema.parse({
    name: 'Example Add-in',
    company: 'Example',
    url: 'https://example.com',
    description: 'Example public product.',
    features: ['Batch Naming'],
    category: 'BIM Productivity',
    target_user: 'BIM managers',
    pricing: 'unknown',
    revit_version: 'unknown',
    commercial_or_free: 'unknown',
    source: 'https://example.com',
    unapproved_field: 'must not be emitted',
  })).toThrow();
});

it('accepts public HTTP(S) URLs and rejects unsafe URL schemes or credentials', () => {
  const validProduct = {
    name: 'Example Add-in',
    company: 'Example',
    url: 'http://example.com/product',
    description: 'Example public product.',
    features: ['Batch Naming'],
    category: 'BIM Productivity',
    target_user: 'BIM managers',
    pricing: 'unknown',
    revit_version: 'unknown',
    commercial_or_free: 'unknown',
    source: 'https://example.com/source',
  };

  expect(ProductSchema.parse(validProduct)).toMatchObject({
    url: 'http://example.com/product',
    source: 'https://example.com/source',
  });

  for (const unsafeUrl of [
    'javascript:alert(1)',
    'data:text/plain,unsafe',
    'file:///private/input.json',
    'https://user:password@example.com',
  ]) {
    expect(ProductSchema.safeParse({ ...validProduct, url: unsafeUrl }).success).toBe(false);
    expect(ProductSchema.safeParse({ ...validProduct, source: unsafeUrl }).success).toBe(false);
  }
});
