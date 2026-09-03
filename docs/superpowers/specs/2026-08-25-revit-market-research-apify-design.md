# Thiết kế Revit Market Research Apify Actor

**Ngày:** 2026-08-25  
**Trạng thái:** Đã được người dùng duyệt trong hội thoại  
**Vị trí triển khai:** `revit-market-research/` tại root repository

## 1. Mục tiêu

Xây dựng một Apify Actor bằng Node.js và TypeScript để nghiên cứu các Revit API Add-in chuyên nghiệp đang có trên thị trường. Tool phải tìm sản phẩm từ nguồn public, chuẩn hóa dữ liệu, phân tích tín hiệu thị trường và xuất:

- `products.json`
- `products.csv`
- `market_report.md`

MVP chạy live với tối đa 20 sản phẩm hợp lệ. Sau khi MVP được xác minh, cùng pipeline có thể chạy theo lịch và mở rộng quy mô bằng input, không cần thay đổi kiến trúc.

## 2. Phạm vi

### 2.1 Nguồn dữ liệu

- Google và Bing thông qua SERP Actors có sẵn trên Apify.
- Autodesk App Store public pages.
- Website sản phẩm và website vendor public.
- GitHub public repositories, releases và metadata công khai.
- Forum public để tìm pain points và tín hiệu thảo luận.

Không đăng nhập, không vượt paywall, không crawl dữ liệu private và không thu thập dữ liệu cá nhân.

### 2.2 Nhóm sản phẩm

- Revit Automation
- BIM Productivity
- Model QA/QC
- Parameter Management
- Family Management
- Sheet/View Automation
- Documentation
- Export/Import
- MEP/Structural/Architecture tools
- AI tools for Revit

### 2.3 Ngoài phạm vi MVP

- Dashboard web hoặc UI quản trị.
- Dự đoán doanh số hay số người dùng khi không có dữ liệu công bố.
- Bypass CAPTCHA, anti-bot, paywall hoặc access control.
- Tự động triển khai Actor hoặc tạo Apify Schedule.
- Dùng LLM bắt buộc trong extraction. MVP ưu tiên deterministic parsing và controlled taxonomy.

## 3. Phương án kiến trúc

Chọn hybrid orchestration:

1. Apify Client gọi SERP Actors cho Google và Bing.
2. Crawlee/Cheerio crawl các trang product/vendor và public source pages.
3. Playwright chỉ là fallback có giới hạn cho trang JavaScript-heavy thuộc allowlist.
4. Source adapters trả dữ liệu thô kèm bằng chứng.
5. Normalization hợp nhất bằng chứng thành product records.
6. Analysis tạo ranking, feature statistics, pricing analysis và market opportunities.
7. Reporting xuất Dataset và ba artifact bắt buộc.

Không dùng all-Playwright vì chi phí và độ giòn cao. Không chain hoàn toàn bằng third-party Actors vì schema, chi phí và độ tin cậy khó kiểm soát.

## 4. Cấu trúc project

```text
revit-market-research/
├── .actor/
│   ├── actor.json
│   ├── input_schema.json
│   ├── output_schema.json
│   └── dataset_schema.json
├── src/
│   ├── main.ts
│   ├── config/
│   ├── discovery/
│   ├── crawlers/
│   ├── sources/
│   ├── normalization/
│   ├── analysis/
│   ├── reporting/
│   ├── cache/
│   ├── models/
│   └── shared/
├── tests/
│   ├── fixtures/
│   ├── unit/
│   └── integration/
├── storage/
├── output/
├── .env.example
├── Dockerfile
├── package.json
├── tsconfig.json
└── README.md
```

`main.ts` chỉ điều phối pipeline. Parsing, scoring, storage và export phải nằm trong module riêng, có interface rõ và kiểm thử độc lập.

## 5. Input Actor

Input chính:

```ts
interface ActorInput {
  runMode: "sample" | "full";
  sampleSize: number;
  categories: ProductCategory[];
  searchProviders: Array<"google" | "bing">;
  serpActorIds: Partial<Record<"google" | "bing", string>>;
  searchQueriesPerCategory: number;
  maxRequests: number;
  maxConcurrency: number;
  cacheTtlHours: number;
  includeForums: boolean;
  includeGitHub: boolean;
  outputLanguage: "vi" | "en";
  targetCurrency: "USD";
}
```

Mặc định MVP:

- `runMode = "sample"`
- `sampleSize = 20`
- Google và Bing được bật.
- Output report bằng tiếng Việt.
- Request budget và concurrency có giới hạn an toàn.

SERP Actor IDs là input/configuration, không hardcode phụ thuộc một vendor duy nhất. Token lấy từ `APIFY_TOKEN` trong `.env` khi chạy local hoặc Apify secrets khi chạy cloud.

## 6. Mô hình dữ liệu

Output product giữ đúng các trường bắt buộc:

```ts
interface Product {
  name: string;
  company: string;
  url: string;
  description: string;
  features: string[];
  category: ProductCategory;
  target_user: string;
  pricing: string;
  revit_version: string;
  commercial_or_free: "commercial" | "free" | "freemium" | "unknown";
  source: string;
}
```

Evidence được giữ nội bộ và trong Apify Dataset metadata để audit:

```ts
interface ProductEvidence {
  sourceUrl: string;
  sourceType: "serp" | "autodesk" | "vendor" | "github" | "forum";
  fetchedAt: string;
  extractedFields: Partial<Product>;
  confidence: number;
}
```

Internal normalized record được phép có thêm `evidence`, `marketVisibilityScore`, `pricingNormalized`, `confidence` và `contentFingerprint`. Ba file output chỉ thêm field phân tích khi schema/documentation khai báo rõ.

## 7. Data flow

### 7.1 Query generation

Query builder kết hợp category, commercial intent và Revit terms. Ví dụ intent gồm `plugin`, `add-in`, `pricing`, `buy`, `professional`, `Autodesk App Store`, `review` và phiên bản Revit.

Query set phải deterministic, deduplicate và có giới hạn theo `searchQueriesPerCategory`.

### 7.2 SERP discovery

- Gọi Google/Bing SERP Actors qua injected client interface.
- Cache theo hash của provider, query và search options.
- Chỉ nhận HTTP(S) URLs hợp lệ.
- Canonicalize URL, loại tracking parameters và domain ngoài scope.
- Không truyền nội dung SERP hoặc crawled text vào code execution hay shell.

### 7.3 Source classification và crawling

URL classifier gán nguồn `autodesk`, `vendor`, `github` hoặc `forum`. CheerioCrawler là mặc định. PlaywrightCrawler chỉ dùng khi adapter cho biết trang cần JavaScript và domain nằm trong allowlist.

Mỗi adapter triển khai contract:

```ts
interface SourceAdapter {
  supports(url: URL): boolean;
  extract(context: CrawlContext): Promise<ProductEvidence[]>;
}
```

Adapter không ghi trực tiếp output và không tự quyết định ranking.

### 7.4 Normalization và deduplication

- Vendor hoặc Autodesk ưu tiên cho features, pricing và Revit versions.
- GitHub ưu tiên cho license, releases, open-source status và compatibility evidence.
- Forum dùng cho pain points và mentions; không ghi đè product facts.
- Hợp nhất theo canonical URL, normalized product/company name và known product aliases.
- Field không có đủ bằng chứng dùng `"unknown"`; không tạo dữ kiện suy đoán.
- Pricing giữ raw text và, khi parse được, tạo normalized amount/currency/billing period.

## 8. Phân tích thị trường

### 8.1 Market visibility score

Top Add-ins dùng public market visibility proxy, không tuyên bố là doanh số hoặc số người dùng:

| Tín hiệu | Trọng số |
|---|---:|
| SERP rank và số provider tìm thấy | 25% |
| Autodesk App Store hoặc nguồn authority | 20% |
| GitHub activity và public forum mentions | 15% |
| Commercial maturity: pricing, docs, support, version coverage | 20% |
| Số nguồn độc lập và độ mới evidence | 20% |

Score được chuẩn hóa 0–100. Report giải thích phương pháp và đưa evidence URLs cho từng mục Top 20.

### 8.2 Feature frequency

Feature normalizer ánh xạ synonyms về controlled taxonomy. Frequency đếm số product chứa feature sau deduplication. Top 20 ghi count, percentage và category distribution.

### 8.3 Pricing

- Phân nhóm free, freemium, one-time, monthly và annual subscription.
- Chỉ tính average khi parse được amount, currency và billing period.
- Subscription được annualize sang USD/năm.
- One-time price báo cáo riêng, không trộn với subscription.
- Report luôn ghi sample size, unknown count, ngày/tỷ giá cấu hình.

### 8.4 Market opportunities

```text
Opportunity score =
  30% pain-point frequency
+ 25% thiếu sản phẩm giải quyết trực tiếp
+ 20% khả thi bằng Revit API
+ 15% willingness-to-pay signals
+ 10% differentiation potential
```

Mỗi cơ hội gồm problem/workflow, target user, competitor coverage, MVP features, Revit API technical risk, pricing hypothesis, confidence và evidence URLs.

## 9. Reporting

### 9.1 `products.json`

UTF-8 JSON array, validated trước khi ghi. Mỗi record có các trường bắt buộc và không chứa secret hoặc raw HTML.

### 9.2 `products.csv`

UTF-8 CSV với header cố định. Arrays được serialize theo format document rõ; dấu phẩy, quote và newline được escape đúng chuẩn.

### 9.3 `market_report.md`

Report tiếng Việt gồm:

1. Executive summary.
2. Methodology, run timestamp và source coverage.
3. Top 20 Revit Add-ins theo market visibility.
4. Top 20 feature phổ biến.
5. Pricing analysis theo licensing model.
6. Workflows đang được tự động hóa.
7. Pain points còn tồn tại.
8. Top 10 market opportunities.
9. Đề xuất sản phẩm có tiềm năng thương mại.
10. Limitations, failures và evidence links.

Nếu run có dưới 20 sản phẩm hợp lệ, report phải công bố coverage thật; không bịa thêm record để đủ bảng.

Artifacts được ghi vào `output/` khi local và Apify Key-Value Store khi cloud. Normalized product records cũng được push vào Apify Dataset.

## 10. Retry, cache và phục hồi

- Tối đa 3 attempt với exponential backoff và jitter cho `429`, `5xx`, timeout và transient network errors.
- Tôn trọng `Retry-After` khi có.
- Không retry `401`, `403`, robots denial hoặc URL invalid.
- Circuit breaker theo domain khi lỗi liên tiếp vượt threshold.
- Lỗi một source không hủy toàn bộ run; report ghi partial coverage và failure summary.
- SERP TTL mặc định 24 giờ, product page 7 ngày, forum 3 ngày.
- Local dùng filesystem cache adapter; cloud dùng Apify Key-Value Store.
- Checkpoint sau discovery, crawl và normalization để Actor restart có thể tiếp tục an toàn.
- Content fingerprint quyết định record có cần reprocess trong incremental run hay không.

## 11. Logging và bảo mật

- Dùng `apify/log`, không dùng `console.log`.
- Log run ID, stage, counts, durations và sanitized URL/domain.
- Không log token, authorization headers, cookies hoặc full query có secret.
- External HTML/text là untrusted input; validate type/length, sanitize và không thực thi.
- `APIFY_TOKEN` chỉ từ `.env` hoặc Apify secret store.
- `.env`, `storage/`, cache và live outputs không được commit nếu chứa run data.
- Tôn trọng robots.txt, rate limits và Terms of Service.

## 12. Chạy định kỳ

Actor không tự tạo schedule. README hướng dẫn người vận hành tạo Apify Schedule theo tuần hoặc tháng và chọn preset `sample`/`full`.

Mỗi run lưu:

- Timestamp và sanitized input.
- Source coverage.
- New, changed và missing product counts so với snapshot trước.
- Failure summary.
- Output artifact links trên Apify.

## 13. Kiểm thử

### 13.1 Unit tests

- Query generation.
- URL canonicalization và classification.
- Deduplication và evidence precedence.
- Pricing parser và annualization.
- Feature taxonomy.
- Visibility/opportunity scoring.
- JSON/CSV/Markdown formatting.

### 13.2 Contract tests

Mỗi source adapter dùng sanitized HTML/JSON fixtures cố định. Test không phụ thuộc live website.

### 13.3 Integration tests

Pipeline dùng fake SERP client, fake crawler responses và local storage. Không gọi paid Actor, không cần token.

### 13.4 Live smoke test

- Chạy qua `apify run`.
- Đọc `APIFY_TOKEN` từ `.env` mà không in token.
- `runMode = "sample"`, `sampleSize = 20`.
- Có request/cost guardrail.
- Kiểm tra schema, số sản phẩm hợp lệ, source diversity và ba artifacts.

Nếu token không có, automated tests vẫn phải chạy đầy đủ; live smoke test được báo là skipped, không giả vờ pass.

## 14. Acceptance criteria MVP

1. Apify Actor TypeScript build thành công và chạy local bằng `apify run`.
2. Actor input/output/dataset schemas hợp lệ; `.actor/actor.json` có `generatedBy = "Codex with GPT-5"`.
3. Automated tests không gọi paid services và pass.
4. Live sample run có guardrail, thu tối đa 20 sản phẩm hợp lệ từ ít nhất ba loại nguồn nếu nguồn public khả dụng.
5. `products.json` và `products.csv` đúng schema, parse lại được.
6. `market_report.md` có đủ các section bắt buộc và ghi rõ methodology/limitations.
7. Retry, cache, partial failure và sanitized logging được kiểm thử.
8. Không có token, private data hoặc raw credential trong source, log và output.
9. README hướng dẫn local run, cloud run, input, outputs, schedule, chi phí và giới hạn pháp lý.

## 15. Trình tự mở rộng sau MVP

1. Tăng query/category coverage bằng input.
2. Bổ sung adapters khi có fixture và contract test.
3. Thêm change tracking giữa các market snapshots.
4. Hiệu chỉnh taxonomy và ranking từ dữ liệu thực.
5. Chỉ cân nhắc LLM-assisted extraction cho field khó sau khi có deterministic baseline, cost cap và audit trail.

## 16. Rủi ro và biện pháp

| Rủi ro | Biện pháp |
|---|---|
| SERP Actor thay đổi schema hoặc vendor | Adapter interface, schema validation, Actor ID configurable |
| Website thay selector | Source fixtures, fallback selectors, confidence score |
| CAPTCHA/anti-bot | Không bypass; giảm rate, cache, ghi coverage limitation |
| Giá không đồng nhất | Giữ raw price, tách billing model, report unknown/sample size |
| Popularity không có số dùng thật | Gọi đúng là market visibility proxy, công bố weights |
| Forum noisy hoặc chứa dữ liệu cá nhân | Chỉ tổng hợp pain-point text public, không xuất profile/PII |
| Cost tăng ngoài dự kiến | Sample mode, maxRequests, query cap, cache và run summary |

## 17. Quyết định đã chốt

- Package độc lập tại `revit-market-research/`.
- Node.js + TypeScript + Apify Client/Crawlee.
- Hybrid SERP Actors + source-specific Crawlee adapters.
- Chấp nhận paid SERP Actors cho live runs.
- Automated tests dùng fixtures/fakes; live sample tối đa 20 sản phẩm.
- Report mặc định bằng tiếng Việt.
- Market visibility proxy phải minh bạch, không trình bày như doanh số thật.

