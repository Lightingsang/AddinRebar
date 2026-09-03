export const SEARCH_PROVIDERS = ['google', 'bing'] as const;
export type SearchProvider = typeof SEARCH_PROVIDERS[number];

export const EVIDENCE_SOURCES = ['search-result', 'vendor-page', 'marketplace'] as const;
export type EvidenceSource = typeof EVIDENCE_SOURCES[number];

export interface DiscoveredUrl {
  url: string;
  provider: SearchProvider;
  query: string;
  title?: string;
  discoveredAt: string;
}

export interface ProductEvidence {
  productUrl: string;
  evidenceUrl: string;
  source: EvidenceSource;
  provider?: SearchProvider;
  excerpt?: string;
  collectedAt: string;
}

export interface RunFailure {
  stage: 'discovery' | 'fetch' | 'analysis' | 'export';
  message: string;
  provider?: SearchProvider;
  url?: string;
  retryable: boolean;
}
