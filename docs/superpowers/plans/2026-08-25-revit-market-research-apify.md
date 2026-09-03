# Revit Market Research Apify Actor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Build a production-oriented TypeScript Apify Actor that discovers, crawls, normalizes, analyzes, and reports public market data for professional Revit API add-ins, starting with a guarded 20-product sample run.

**Architecture:** Apify Client calls configurable Google and Bing SERP Actors. Crawlee/Cheerio visits canonical public URLs through source adapters; evidence is normalized into auditable products, scored transparently, and exported to Dataset plus JSON, CSV, and Vietnamese Markdown artifacts.

**Tech Stack:** Node.js 24, TypeScript ESM, Apify SDK/Client, Crawlee 3.14, Cheerio, Zod, csv-stringify, Vitest, dotenv, Docker.

**Spec:** docs/superpowers/specs/2026-08-25-revit-market-research-apify-design.md

## Global Constraints

- Create the independent package at revit-market-research/; do not couple it to HPRebar/ or the course website.
- Use public pages only; never log in, cross a paywall, bypass CAPTCHA/anti-bot controls, or collect private data.
- Set .actor/actor.json meta.generatedBy exactly to Codex with GPT-5.
- Read APIFY_TOKEN only from .env, process environment, or Apify secret storage; never print or persist it.
- Use Apify logging and never console.log in runtime code.
- Use Cheerio; MVP adds no Playwright dependency until an allowlisted adapter has a fixture-backed need.
- Set respectRobotsTxtFile: true, retryOnBlocked: false, maxRequestRetries: 3, and sameDomainDelaySecs: 2.
- Automated tests use fixtures/fakes and never call paid Actors. Only an explicit live smoke run may spend Apify credits.
- Keep implementation files below 200 lines where practical.
- Root workspace currently has no Git repository. Run verification but skip commits unless Git becomes available; report suggested messages.
- On Windows use npm.cmd because the npm.ps1 shim is blocked.

---

## File Map

Runtime/platform:
- package.json, package-lock.json, tsconfig.json, eslint.config.js, Dockerfile, .gitignore, .env.example.
- .actor/actor.json, input_schema.json, output_schema.json, dataset_schema.json.
- storage/key_value_stores/default/INPUT.json.

Models/config:
- src/models/product.ts: categories, product, normalized pricing, Zod schemas.
- src/models/evidence.ts: discovered URL, evidence, source and failure types.
- src/models/analysis.ts: score, summaries, opportunity and report contracts.
- src/config/input.ts: input validation/defaults.
- src/config/load-config.ts: input plus environment-only secrets.

Discovery:
- src/discovery/query-builder.ts.
- src/discovery/serp-client.ts.
- src/discovery/apify-serp-client.ts.
- src/discovery/serp-parsers.ts.
- src/discovery/url-normalizer.ts.
- src/discovery/source-classifier.ts.

Crawling/sources:
- src/sources/source-adapter.ts.
- src/sources/extraction-helpers.ts.
- src/sources/autodesk-adapter.ts.
- src/sources/github-adapter.ts.
- src/sources/forum-adapter.ts.
- src/sources/vendor-adapter.ts.
- src/crawlers/domain-circuit-breaker.ts.
- src/crawlers/product-crawler.ts.

Normalization/analysis:
- src/normalization/feature-taxonomy.ts.
- src/normalization/pricing-parser.ts.
- src/normalization/product-normalizer.ts.
- src/normalization/deduplicate-products.ts.
- src/analysis/visibility-score.ts.
- src/analysis/feature-analysis.ts.
- src/analysis/pricing-analysis.ts.
- src/analysis/opportunity-catalog.ts.
- src/analysis/opportunity-analysis.ts.
- src/analysis/market-analysis.ts.

Persistence/reporting:
- src/cache/cache-store.ts.
- src/pipeline/run-market-research.ts.
- src/reporting/json-report.ts.
- src/reporting/csv-report.ts.
- src/reporting/markdown-report.ts.
- src/reporting/artifact-writer.ts.
- src/main.ts.
- README.md.

---

### Task 1: Scaffold Actor, contracts, schemas, and input validation

**Files:** Create all runtime/platform files; models/product.ts, models/evidence.ts, models/analysis.ts, config/input.ts, config/load-config.ts. Test tests/unit/input.test.ts and product-schema.test.ts.

**Produces:**
- parseActorInput(input: unknown): ActorInput
- loadConfig(input: unknown, env: NodeJS.ProcessEnv): ActorConfig
- ProductSchema, ProductEvidence, DiscoveredUrl, MarketAnalysis

- [ ] **Step 1: Verify and install prerequisites**

~~~powershell
node --version
npm.cmd --version
npm.cmd install --global apify-cli
apify --help
~~~

Expected: Node 24.x, npm works through npm.cmd, Apify CLI help prints. Request network escalation when required.

- [ ] **Step 2: Scaffold and install reviewed packages**

~~~powershell
apify create revit-market-research -t ts_empty
Set-Location revit-market-research
npm.cmd install apify apify-client crawlee zod dotenv csv-stringify
npm.cmd install --save-dev typescript vitest @types/node eslint @eslint/js typescript-eslint
~~~

Expected: package-lock.json pins the dependency graph.

- [ ] **Step 3: Configure scripts and Actor metadata**

package.json scripts:

~~~json
{
  "build": "tsc -p tsconfig.json",
  "test": "vitest run",
  "test:watch": "vitest",
  "typecheck": "tsc -p tsconfig.json --noEmit",
  "lint": "eslint src tests",
  "smoke:live": "apify run"
}
~~~

actor.json links the three schemas and Dockerfile, sets usesStandbyMode false and generatedBy Codex with GPT-5.

- [ ] **Step 4: Write failing tests**

~~~ts
it('applies guarded sample defaults', () => {
  expect(parseActorInput({})).toMatchObject({
    runMode: 'sample',
    sampleSize: 20,
    maxRequests: 120,
    searchProviders: ['google', 'bing'],
    outputLanguage: 'vi',
  });
});

it('rejects unsafe sample sizes', () => {
  expect(() => parseActorInput({ sampleSize: 1001 })).toThrow();
});

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
~~~

- [ ] **Step 5: Verify RED**

Run: npm.cmd test -- tests/unit/input.test.ts tests/unit/product-schema.test.ts

Expected: FAIL because modules do not exist.

- [ ] **Step 6: Implement contracts/defaults**

Use exactly ten categories from the spec. Constrain sampleSize 1–1000, maxRequests 1–5000, maxConcurrency 1–20. Defaults:

~~~ts
serpActorIds: {
  google: 'scraper-engine/google-search-results-scraper',
  bing: 'crawlerbros/bing-search-scraper',
}
~~~

loadConfig throws APIFY_TOKEN is required for live SERP discovery only when constructing the real client; fixture tests need no token.

- [ ] **Step 7: Define schemas and safe INPUT**

Input schema mirrors validated fields. Dataset schema includes required product fields plus market_visibility_score, confidence, evidence_urls. Output schema links Dataset and KV records PRODUCTS_JSON, PRODUCTS_CSV, MARKET_REPORT. INPUT.json uses sampleSize 20 and maxRequests 120.

- [ ] **Step 8: Verify GREEN**

~~~powershell
npm.cmd run typecheck
npm.cmd test -- tests/unit/input.test.ts tests/unit/product-schema.test.ts
apify validate-schema .actor/input_schema.json
~~~

Expected: PASS. Suggested commit: feat: scaffold Revit market research actor

---

### Task 2: Add deterministic SERP discovery

**Files:** Create all discovery files. Test query-builder.test.ts, url-normalizer.test.ts, serp-parsers.test.ts. Fixtures tests/fixtures/serp/google.json and bing.json.

**Produces:**
- buildSearchQueries(categories, limitPerCategory): SearchQuery[]
- SerpClient.search(request): Promise<DiscoveredUrl[]>
- canonicalizeUrl(raw): string | null
- classifySource(url): SourceType

- [ ] **Step 1: Write failing tests**

~~~ts
it('builds stable queries', () => {
  expect(buildSearchQueries(['Model QA/QC'], 2).map((x) => x.query)).toEqual([
    'professional Revit add-in Model QA QC pricing',
    'site:apps.autodesk.com Revit Model QA QC add-in',
  ]);
});

it('removes tracking and fragments', () => {
  expect(canonicalizeUrl('https://Example.com/tool/?utm_source=x#pricing'))
    .toBe('https://example.com/tool');
});

it('normalizes nested Google results', () => {
  expect(parseGoogleItems(googleFixture, 'q')[0]).toMatchObject({
    url: 'https://example.com/tool',
    provider: 'google',
    rank: 1,
  });
});
~~~

- [ ] **Step 2: Verify RED**

Run the three discovery tests. Expected: missing modules.

- [ ] **Step 3: Implement query and URL safety**

Reject non-HTTP(S), embedded credentials, localhost, loopback, link-local/private IPs, and executable/binary URLs. Strip fragments/tracking params, default ports, duplicate slash and trailing slash.

- [ ] **Step 4: Implement provider parsers**

Google supports page-shaped organicResults and row-shaped url/link results. Bing supports url/link, position, title, snippet. Validate unknown rows with Zod passthrough objects; discard invalid URLs and count discards.

- [ ] **Step 5: Implement real Apify client**

The interface is:

~~~ts
export interface SerpClient {
  search(request: SerpSearchRequest): Promise<DiscoveredUrl[]>;
}
~~~

ApifySerpClient uses new ApifyClient({ token }), calls Google once with newline-separated queries, Bing once with query array, checks SUCCEEDED/defaultDatasetId, and lists the dataset. Cap rows before crawling.

- [ ] **Step 6: Verify GREEN**

Run discovery tests and typecheck. Expected: PASS. Suggested commit: feat: add guarded SERP discovery

---

### Task 3: Crawl and extract public source evidence

**Files:** Create all source/crawler files. Test domain-circuit-breaker.test.ts and contract/source-adapters.test.ts. Add sanitized HTML fixtures for Autodesk, vendor, GitHub, and forum.

**Produces:**
- SourceAdapter.supports(url, sourceType): boolean
- SourceAdapter.extract(context): Promise<ProductEvidence[]>
- crawlProducts(urls, options): Promise<CrawlResult>

- [ ] **Step 1: Create sanitized fixtures**

Each product fixture contains public title, canonical URL, meta description, company, features, price, and version. Forum fixture has aggregated problem text without usernames/emails.

- [ ] **Step 2: Write failing tests**

~~~ts
it('emits auditable vendor evidence', async () => {
  const evidence = await extractFixture(vendorAdapter, vendorFixture);
  expect(evidence[0]).toMatchObject({
    extractedFields: { name: 'Vendor Sample Tool' },
    confidence: expect.any(Number),
  });
  expect(evidence[0].sourceUrl).toMatch(/^https:\/\//);
});

it('opens a domain after three failures', () => {
  const breaker = new DomainCircuitBreaker(3, 60_000);
  breaker.recordFailure('example.com');
  breaker.recordFailure('example.com');
  breaker.recordFailure('example.com');
  expect(breaker.canRequest('example.com', 0)).toBe(false);
});
~~~

- [ ] **Step 3: Verify RED**

Run contract and circuit-breaker tests. Expected: missing modules.

- [ ] **Step 4: Implement bounded extraction helpers**

cleanText collapses whitespace/control characters and truncates. Parse JSON-LD using JSON.parse only, never eval. Cap description at 2,000 characters, features at 40, each feature at 160.

- [ ] **Step 5: Implement adapters**

Autodesk prefers JSON-LD and labeled product/publisher/version/price selectors. GitHub prefers OpenGraph/topics/release/license and marks free only with public license evidence. Forum returns pain-point evidence and does not override facts. Vendor prefers Product/SoftwareApplication JSON-LD then meta/headings/labeled price/version blocks. All return confidence 0–1 and source URL.

- [ ] **Step 6: Implement Crawlee**

~~~ts
new CheerioCrawler({
  maxConcurrency: config.maxConcurrency,
  maxRequestsPerCrawl: config.maxRequests,
  maxRequestRetries: 3,
  respectRobotsTxtFile: true,
  retryOnBlocked: false,
  sameDomainDelaySecs: 2,
  requestHandlerTimeoutSecs: 45,
  requestHandler,
  failedRequestHandler,
});
~~~

Pre-navigation checks the circuit breaker; when open it sets request.noRetry and throws DomainCircuitOpenError. Failures store sanitized domain/status/reason. Success resets the domain.

- [ ] **Step 7: Verify GREEN**

Run contract tests, breaker tests, and typecheck. Expected: PASS. Suggested commit: feat: crawl public Revit product sources

---

### Task 4: Normalize features, prices, evidence, and duplicates

**Files:** Create all normalization files. Test feature-taxonomy, pricing-parser, product-normalizer, deduplicate-products.

**Produces:**
- normalizeFeature(raw): string | null
- parsePricing(raw, rates): NormalizedPricing
- normalizeProduct(evidence): NormalizedProduct | null
- deduplicateProducts(products): NormalizedProduct[]

- [ ] **Step 1: Write failing tests**

~~~ts
it.each(['batch rename', 'bulk naming', 'renumber sheets'])(
  'maps %s to Batch Naming',
  (raw) => expect(normalizeFeature(raw)).toBe('Batch Naming'),
);

it('annualizes monthly USD', () => {
  expect(parsePricing('$10/month', { USD: 1 })).toMatchObject({
    model: 'monthly',
    currency: 'USD',
    annualUsd: 120,
  });
  expect(parsePricing('$199 one-time', { USD: 1 }).annualUsd).toBeNull();
});

it('prefers vendor pricing over forum text', () => {
  expect(normalizeProduct([forumEvidence, vendorEvidence])?.pricing)
    .toBe(vendorEvidence.extractedFields.pricing);
});
~~~

- [ ] **Step 2: Verify RED**

Run four normalization test files. Expected: missing modules.

- [ ] **Step 3: Implement taxonomy**

Aliases cover Batch Naming, Sheet Creation, View Creation, Parameter Editing, Family Browsing, Model Checking, Clash Detection, Standards Enforcement, Batch Export, Data Import, Quantity Takeoff, MEP Routing, Structural Detailing, Documentation Generation, AI Assistance, Repetitive Task Automation. Discard marketing sentences.

- [ ] **Step 4: Implement pricing**

Support USD/EUR/GBP symbols/codes and month/mo/year/yr/annual/one-time/perpetual/free/trial. Store raw, model, amount, currency, annualUsd, confidence. Exchange rates are input, never silently fetched.

- [ ] **Step 5: Merge/deduplicate**

Authority: vendor/autodesk, GitHub, SERP, forum. Reject empty name or invalid URL. Identity uses normalized company/name plus canonical aliases. Union features/evidence; retain highest-confidence non-unknown fields.

- [ ] **Step 6: Verify GREEN**

Run normalization tests and typecheck. Expected: PASS. Suggested commit: feat: normalize Revit add-in evidence

---

### Task 5: Analyze market signals and opportunities

**Files:** Create all analysis files. Test visibility-score, pricing/market-analysis, opportunity-analysis.

**Produces:**
- calculateVisibilityScore(product): VisibilityScore
- analyzeMarket(products, context): MarketAnalysis

- [ ] **Step 1: Write failing score tests**

~~~ts
it('applies published visibility weights', () => {
  expect(calculateVisibilityScore(fullSignalProduct).components).toEqual({
    searchVisibility: 25,
    authorityPresence: 20,
    communitySignals: 15,
    commercialMaturity: 20,
    evidenceQuality: 20,
  });
});

it('keeps price models separate', () => {
  const result = analyzePricing([monthly10, annual240, oneTime199]);
  expect(result.subscriptionAnnualUsdAverage).toBe(180);
  expect(result.oneTimeUsdAverage).toBe(199);
});
~~~

- [ ] **Step 2: Write failing opportunity formula test**

~~~ts
expect(scoreOpportunity(signal)).toBe(
  0.30 * signal.painFrequency
  + 0.25 * signal.competitionGap
  + 0.20 * signal.apiFeasibility
  + 0.15 * signal.willingnessToPay
  + 0.10 * signal.differentiation
);
~~~

- [ ] **Step 3: Verify RED**

Run analysis tests. Expected: missing modules.

- [ ] **Step 4: Implement deterministic analysis**

Cap visibility components at weights 25/20/15/20/20. Count features per product. Report parsed/unknown price counts. Stable ties: score desc, confidence desc, name asc.

- [ ] **Step 5: Implement finite opportunity catalog**

Include model health triage, parameter governance, family quality governance, cross-version standards migration, sheet/view QA, documentation consistency, batch IFC/DWG export QA, MEP issue resolution, structural detailing, AI model query, accessibility checks, and change-impact reporting. Each defines users, MVP, feasibility, risks, competitor aliases, pain keywords, price hypothesis.

Only cite matched evidence URLs. No match means zero demand/payment signals and low confidence.

- [ ] **Step 6: Verify GREEN**

Run analysis tests and typecheck. Expected: PASS. Suggested commit: feat: analyze Revit add-in market signals

---

### Task 6: Add TTL cache, checkpoints, and pipeline

**Files:** Create cache/cache-store.ts and pipeline/run-market-research.ts. Test cache-store.test.ts and integration/pipeline.test.ts. Add 24 candidate fixtures.

**Produces:**
- CacheStore.get/set
- runMarketResearch(dependencies, config): Promise<PipelineResult>

- [ ] **Step 1: Write failing cache test**

~~~ts
await cache.set('key', { ok: true }, 1000, 0);
expect(await cache.get('key', 999)).toEqual({ ok: true });
expect(await cache.get('key', 1001)).toBeNull();
~~~

- [ ] **Step 2: Write failing 20-product integration test**

~~~ts
expect(result.products).toHaveLength(20);
expect(new Set(result.products.map((x) => x.url)).size).toBe(20);
expect(result.analysis.topProducts).toHaveLength(20);
expect(result.runSummary.sourceCoverage.length).toBeGreaterThanOrEqual(3);
~~~

Fake SERP returns cross-provider duplicates; fake crawler returns 24 valid candidates and failures.

- [ ] **Step 3: Verify RED**

Run cache and pipeline tests. Expected: missing modules.

- [ ] **Step 4: Implement cache**

~~~ts
export interface KeyValueBackend {
  getValue<T>(key: string): Promise<T | null>;
  setValue<T>(key: string, value: T): Promise<void>;
}
~~~

Store value, createdAt, expiresAt, contentFingerprint. Keys are SHA-256 of sanitized query/URL; never token/header data.

- [ ] **Step 5: Implement stages/checkpoints**

Stages: input, queries, cached SERP, URL dedupe, crawl/cache, evidence normalization, product dedupe, sample limit, analysis. Checkpoints CHECKPOINT_DISCOVERY, CHECKPOINT_EVIDENCE, CHECKPOINT_PRODUCTS include schemaVersion/inputFingerprint. Ignore mismatched fingerprints. Partial failures continue unless no valid product remains.

- [ ] **Step 6: Verify GREEN**

Run cache/integration tests, full suite, and typecheck. Expected: exactly 20 unique fixture products; PASS. Suggested commit: feat: orchestrate incremental market research

---

### Task 7: Export artifacts and wire Actor lifecycle

**Files:** Create reporting files and src/main.ts. Test reporting.test.ts and integration/actor-output.test.ts.

**Produces:**
- renderProductsJson, renderProductsCsv, renderMarketReport
- KV keys PRODUCTS_JSON, PRODUCTS_CSV, MARKET_REPORT, RUN_SUMMARY

- [ ] **Step 1: Write failing tests**

~~~ts
expect(JSON.parse(renderProductsJson([product]))[0]).toMatchObject(product);
expect(renderProductsCsv([{ ...product, description: 'A, "quoted"\nline' }]))
  .toContain('"A, ""quoted""\nline"');

for (const heading of [
  'Top 20 Revit Add-ins',
  'Top 20 tính năng',
  'Top 10 cơ hội',
  'Giới hạn',
]) {
  expect(renderMarketReport(analysisFixture)).toContain(heading);
}
~~~

- [ ] **Step 2: Verify RED**

Run reporting/output tests. Expected: missing modules.

- [ ] **Step 3: Implement serializers/report**

JSON: two spaces and final newline. CSV: csv-stringify/sync, fixed columns, features separated by pipe, UTF-8 BOM. Markdown: actual list length, pricing sample sizes, formulas, workflows, pain points, failures, limitations, evidence links.

- [ ] **Step 4: Implement writer**

Write content types to default KV keys. When APIFY_IS_AT_HOME is not 1, mirror exact content to output/products.json, products.csv, market_report.md with mkdir recursive/writeFile.

- [ ] **Step 5: Implement main**

Actor.init; read input once; construct storage/client; run pipeline; await Actor.pushData(products); write artifacts; sanitized counts through Apify logger; exception logs/rethrows; Actor.exit in finally.

- [ ] **Step 6: Verify GREEN**

~~~powershell
npm.cmd test
npm.cmd run lint
npm.cmd run typecheck
npm.cmd run build
~~~

Expected: PASS. Suggested commit: feat: export Revit market research reports

---

### Task 8: Document, audit, and run guarded sample

**Files:** Create README.md and tests/live/live-smoke.test.ts; finalize package scripts/schemas.

- [ ] **Step 1: Write Actor README**

Use Apify-required H2/H3 structure: purpose, benefits, extracted fields, run tutorial, cost guardrails, input, output, scheduling, API example, ethics/legal disclaimer, troubleshooting/support. Explain market visibility is not sales/users.

- [ ] **Step 2: Add opt-in live smoke**

Only run when RUN_LIVE_SMOKE=1 and APIFY_TOKEN exists; otherwise skip. Use sampleSize 20, maxRequests 120, two queries/category max; validate artifacts without printing token.

- [ ] **Step 3: Run security/quality audit**

~~~powershell
rg -n "console\.log|APIFY_TOKEN\s*=|Bearer\s+[A-Za-z0-9]" src tests .actor README.md
npm.cmd audit --audit-level=high
npm.cmd test
npm.cmd run lint
npm.cmd run typecheck
npm.cmd run build
apify validate-schema .actor/input_schema.json
apify validate-schema .actor/dataset_schema.json
~~~

Expected: no credential/runtime console matches, no high/critical audit issues, all checks PASS.

- [ ] **Step 4: Verify fixture sample**

Run integration pipeline/output tests. Expected: exactly 20 deduplicated products and parseable JSON/CSV/Markdown without paid calls.

- [ ] **Step 5: Run live smoke only with token**

~~~powershell
$env:RUN_LIVE_SMOKE='1'
npm.cmd test -- tests/live/live-smoke.test.ts
apify run
~~~

Expected: up to 20 valid products and honest source/failure coverage. If token absent, report SKIPPED; do not fabricate.

- [ ] **Step 6: Inspect artifacts**

Parse JSON, re-import CSV, scan headings/links. Confirm no credentials, private data, raw HTML, or unsupported popularity claims. Record counts for products, sources, unknown prices, failures, and opportunities.

- [ ] **Step 7: Final suggested commit**

Suggested message: test: verify 20-product Revit market sample

---

## Final Verification Gate

From revit-market-research/ run:

~~~powershell
npm.cmd test
npm.cmd run lint
npm.cmd run typecheck
npm.cmd run build
apify validate-schema .actor/input_schema.json
apify validate-schema .actor/dataset_schema.json
npm.cmd audit --audit-level=high
~~~

Completion requires:
- Automated tests pass without paid calls.
- Fixture integration produces exactly 20 unique valid products.
- Actor schemas validate and generatedBy is correct.
- JSON/CSV/Markdown parse and contain required sections.
- Runtime logs use Apify logging and contain no secrets.
- Live smoke is verified with real evidence or explicitly skipped for missing token.
- Unavailable Git commits are reported, never claimed.

