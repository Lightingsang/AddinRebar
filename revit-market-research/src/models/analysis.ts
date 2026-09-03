import type { ProductCategory } from './product.js';

export interface CategoryAnalysis {
  category: ProductCategory;
  productCount: number;
  averageVisibilityScore: number;
}

export interface PricingAnalysis {
  commercial: number;
  free: number;
  freemium: number;
  unknown: number;
}

export interface MarketAnalysis {
  generatedAt: string;
  language: 'vi' | 'en';
  currency: 'USD';
  productCount: number;
  categories: CategoryAnalysis[];
  pricing: PricingAnalysis;
  summary: string;
}
