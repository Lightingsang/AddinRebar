import { z } from 'zod';

export const PRODUCT_CATEGORIES = [
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
] as const;

export const ProductCategorySchema = z.enum(PRODUCT_CATEGORIES);
export const CommercialModelSchema = z.enum(['commercial', 'free', 'freemium', 'unknown']);
export const HttpUrlSchema = z.string().url().refine((value) => {
  try {
    const url = new URL(value);
    return (url.protocol === 'http:' || url.protocol === 'https:')
      && !url.username
      && !url.password;
  } catch {
    return false;
  }
}, 'Only public HTTP(S) URLs without embedded credentials are allowed.');

export const ProductSchema = z.object({
  name: z.string().min(1),
  company: z.string().min(1),
  url: HttpUrlSchema,
  description: z.string(),
  features: z.array(z.string()),
  category: ProductCategorySchema,
  target_user: z.string(),
  pricing: z.string(),
  revit_version: z.string(),
  commercial_or_free: CommercialModelSchema,
  source: HttpUrlSchema,
}).strict();

export type ProductCategory = z.infer<typeof ProductCategorySchema>;
export type CommercialModel = z.infer<typeof CommercialModelSchema>;
export type Product = z.infer<typeof ProductSchema>;
