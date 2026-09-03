import { describe, expect, it } from 'vitest';

import { parseActorInput } from '../../src/config/input.js';
import { loadConfig } from '../../src/config/load-config.js';

describe('parseActorInput', () => {
  it('applies approved sample discovery defaults', () => {
    expect(parseActorInput({})).toMatchObject({
      runMode: 'sample',
      sampleSize: 20,
      maxRequests: 120,
      maxConcurrency: 10,
      searchProviders: ['google', 'bing'],
      outputLanguage: 'vi',
      targetCurrency: 'USD',
      serpActorIds: {
        google: 'scraper-engine/google-search-results-scraper',
        bing: 'crawlerbros/bing-search-scraper',
      },
      searchQueriesPerCategory: 2,
      cacheTtlHours: 24,
      includeForums: true,
      includeGitHub: true,
    });
  });

  it('rejects unsafe sample sizes', () => {
    expect(() => parseActorInput({ sampleSize: 1001 })).toThrow();
  });

  it('accepts the lower and upper numeric safety bounds', () => {
    expect(parseActorInput({
      sampleSize: 1,
      maxRequests: 1,
      maxConcurrency: 1,
      searchQueriesPerCategory: 1,
      cacheTtlHours: 1,
    })).toMatchObject({
      sampleSize: 1,
      maxRequests: 1,
      maxConcurrency: 1,
      searchQueriesPerCategory: 1,
      cacheTtlHours: 1,
    });
    expect(parseActorInput({
      sampleSize: 1000,
      maxRequests: 5000,
      maxConcurrency: 20,
      searchQueriesPerCategory: 10,
      cacheTtlHours: 720,
    })).toMatchObject({
      sampleSize: 1000,
      maxRequests: 5000,
      maxConcurrency: 20,
      searchQueriesPerCategory: 10,
      cacheTtlHours: 720,
    });
  });

  it('rejects values outside numeric safety bounds', () => {
    expect(() => parseActorInput({ sampleSize: 0 })).toThrow();
    expect(() => parseActorInput({ maxRequests: 5001 })).toThrow();
    expect(() => parseActorInput({ maxConcurrency: 21 })).toThrow();
    expect(() => parseActorInput({ searchQueriesPerCategory: 11 })).toThrow();
    expect(() => parseActorInput({ cacheTtlHours: 721 })).toThrow();
  });

  it('uses the required categories, providers, and nested discovery Actor IDs', () => {
    expect(parseActorInput({})).toMatchObject({
      searchProviders: ['google', 'bing'],
      categories: [
        'Revit Automation',
        'BIM Productivity',
        'Model QA/QC',
        'Parameter Management',
        'Family Management',
        'Sheet/View Automation',
        'Documentation',
        'Export/Import',
        'MEP/Structural/Architecture tools',
        'AI tools for Revit',
      ],
      serpActorIds: {
        google: 'scraper-engine/google-search-results-scraper',
        bing: 'crawlerbros/bing-search-scraper',
      },
    });
  });

  it('allows a partial nested Actor-ID override and rejects retired flat IDs', () => {
    expect(parseActorInput({ serpActorIds: { google: 'example/custom-google' } }).serpActorIds)
      .toEqual({ google: 'example/custom-google' });
    expect(() => parseActorInput({ googleActorId: 'retired/flat-id' })).toThrow();
  });

  it('requires a token whenever runtime configuration is built', () => {
    expect(() => loadConfig({}, {} as NodeJS.ProcessEnv)).toThrow();
    expect(() => loadConfig({ runMode: 'full' }, { APIFY_TOKEN: '   ' } as NodeJS.ProcessEnv))
      .toThrow();
    expect(loadConfig({ runMode: 'full' }, { APIFY_TOKEN: 'test-token' } as NodeJS.ProcessEnv)
      .apifyToken).toBe('test-token');
  });
});
