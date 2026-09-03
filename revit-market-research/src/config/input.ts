import { z } from 'zod';

import { PRODUCT_CATEGORIES } from '../models/product.js';
import { SEARCH_PROVIDERS } from '../models/evidence.js';

export const DEFAULT_GOOGLE_ACTOR_ID = 'scraper-engine/google-search-results-scraper';
export const DEFAULT_BING_ACTOR_ID = 'crawlerbros/bing-search-scraper';

export const ActorInputSchema = z.object({
  runMode: z.enum(['sample', 'full']).default('sample'),
  sampleSize: z.number().int().min(1).max(1000).default(20),
  maxRequests: z.number().int().min(1).max(5000).default(120),
  maxConcurrency: z.number().int().min(1).max(20).default(10),
  searchProviders: z.array(z.enum(SEARCH_PROVIDERS)).min(1).default(['google', 'bing']),
  categories: z.array(z.enum(PRODUCT_CATEGORIES)).min(1).default([...PRODUCT_CATEGORIES]),
  outputLanguage: z.enum(['vi', 'en']).default('vi'),
  targetCurrency: z.literal('USD').default('USD'),
  serpActorIds: z.object({
    google: z.string().min(1).optional(),
    bing: z.string().min(1).optional(),
  }).strict().default({
    google: DEFAULT_GOOGLE_ACTOR_ID,
    bing: DEFAULT_BING_ACTOR_ID,
  }),
  searchQueriesPerCategory: z.number().int().min(1).max(10).default(2),
  cacheTtlHours: z.number().int().min(1).max(720).default(24),
  includeForums: z.boolean().default(true),
  includeGitHub: z.boolean().default(true),
}).strict();

export type ActorInput = z.infer<typeof ActorInputSchema>;

export function parseActorInput(input: unknown): ActorInput {
  return ActorInputSchema.parse(input);
}
