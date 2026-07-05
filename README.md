# PartnerIntegration BFF

> **A production-ready .NET 8 Backend-for-Frontend microservice** demonstrating Clean Architecture, resilient external service integration, asynchronous message queuing, and comprehensive test coverage — built as a technical assessment submission.

---

## ✅ Test Results — 21 Tests, 0 Failures

```
Test run finished: 21 Tests (21 Passed, 0 Failed, 0 Skipped) run in 264 ms
```

| Suite | Tests | Result |
|---|---|---|
| `PartnerIntegration.BFF.Tests.Controllers` | 3 | ✅ All Passed |
| `PartnerIntegration.BFF.Tests.Services` | 3 | ✅ All Passed |
| `PartnerIntegration.BFF.Tests.Validators` | 9 | ✅ All Passed |
| `PartnerIntegration.BFF.Tests.VerificationClients` | 3 | ✅ All Passed |
| **Build** | — | ✅ 0 Warnings, 0 Errors |

---

## 🏗️ Architecture — Clean Architecture (3 Layers)

```
┌────────────────────────────────────────────────────────────────────┐
│                         API LAYER                                  │
│                                                                    │
│  POST /api/v1/partner/transactions   GET /internal/mock-partner    │
│  ┌───────────────────────────────┐   ┌──────────────────────────┐  │
│  │  PartnerTransactionsController│   │  MockPartnerController   │  │
│  │  [Authorize][ApiController]   │   │  [AllowAnonymous]        │  │
│  └──────────────┬────────────────┘   └─────────────┬────────────┘  │
│                 │                                   │              │
│  ┌──────────────▼────────────────┐                 │ throws        │
│  │  ValidationActionFilter       │        TimeoutException 30%    │
│  │  (FluentValidation via DI)    │                 │              │
│  └──────────────┬────────────────┘   ┌─────────────▼────────────┐  │
│                 │                    │  GlobalExceptionHandler   │  │
│                 │                    │  → HTTP 504 ProblemDetails│  │
│                 │                    └──────────────────────────┘  │
├─────────────────┼──────────────────────────────────────────────────┤
│                 │              CORE LAYER                          │
│  ┌──────────────▼────────────────┐                                 │
│  │       TransactionService      │  ← Result Pattern (no throws)  │
│  │  1. Verify partner            │                                 │
│  │  2. Publish to queue          │                                 │
│  └────────┬────────────┬─────────┘                                 │
│           │            │                                           │
├───────────┼────────────┼───────────────────────────────────────────┤
│           │            │       INFRASTRUCTURE LAYER                │
│  ┌────────▼─────────┐  ┌▼────────────────────────┐                 │
│  │PartnerVerification│  │TransactionMessagePublisher│               │
│  │Client (HttpClient)│  │(RabbitMQ — singleton     │               │
│  │                   │  │ IConnection + per-publish │               │
│  │ Polly Pipeline:   │  │ channel)                  │               │
│  │  • Retry × 3      │  └──────────────┬────────────┘               │
│  │  • Circuit Breaker│                 │                           │
│  │  • Timeout 10s    │                 │                           │
└──┼───────────────────┼─────────────────┼───────────────────────────┘
   │                   │                 │
   ▼                   │                 ▼
Mock Partner API       │          RabbitMQ Queue
(same process,         │          "partner-transactions"
 30% timeout)          │          delivery_mode: 2 (persistent)
                       │          content_type: application/json
                       └──────────────────────────────────────────►
```

---

## 🔬 Live Demo — Verified End-to-End

### 1 · Swagger UI — Authenticated via `X-Api-Key`

The API is secured with API Key authentication. Swagger UI exposes an **Authorize** button where the key can be entered once for all requests.

```http
POST https://localhost:7199/api/v1/partner/transactions
X-Api-Key: partner-api-key-2026
Content-Type: application/json

{
  "partnerId": "p-600",
  "transactionReference": "pr-100",
  "amount": 111110,
  "currency": "USD",
  "timestamp": "2026-07-05T08:52:54.877Z"
}
```

**Response — HTTP 202 Accepted:**
```json
{
  "message": "Transaction accepted and queued for processing.",
  "transactionReference": "pr-100",
  "acceptedAt": "2026-07-05T09:01:56.9085473+00:00"
}
```

---

### 2 · Resilience in Action — Mock Partner Verification

The mock partner API randomly fails **30% of the time** with a `TimeoutException`. The Polly resilience pipeline silently retries (up to 3×) before giving up — the calling request **never crashes**.

**30% case — Gateway Timeout (Polly retries then returns gracefully):**
```json
{
  "title": "Gateway Timeout",
  "status": 504,
  "detail": "An upstream service did not respond in time.",
  "instance": "/internal/mock-partner/p-600",
  "traceId": "0HNM0EFS0C33T:00000001",
  "timestamp": "2026-07-05T09:01:17.6312711+00:00"
}
```

**70% case — Partner Verified Successfully:**
```json
{
  "partnerId": "p-600",
  "status": "Active"
}
```

---

### 3 · RabbitMQ — Messages Queued & Persisted

Every accepted transaction is published to the `partner-transactions` queue with:
- `delivery_mode: 2` — **durable / persistent** (survives broker restart)
- `content_type: application/json`

Multiple transactions visible in RabbitMQ Management UI, confirming end-to-end message delivery.

---

## ⚙️ Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 8.0 |
| **Web Framework** | ASP.NET Core MVC (Controller-based) | 8.0 |
| **Validation** | FluentValidation | 11.11 |
| **Resilience** | Polly via `Microsoft.Extensions.Http.Resilience` | 8.10 |
| **Messaging** | RabbitMQ.Client | 7.0 |
| **API Docs** | Swashbuckle / Swagger UI | 6.6 |
| **Unit Testing** | xUnit + Moq + FluentAssertions | Latest |
| **Containerization** | Docker + Docker Compose | — |

---

## 🎯 Key Design Decisions

### Clean Architecture
Three explicit layers with strict dependency rules:
- **Api** → depends on Core + Infrastructure (composition root)
- **Core** → zero infrastructure dependencies (pure business logic)
- **Infrastructure** → implements Core interfaces (details live here)

### Controller-Based API over Minimal APIs
Chosen for better testability, clear separation of concerns, and alignment with enterprise .NET conventions — controllers are thin, all logic lives in the service layer.

### Result Pattern (no exception-driven flow)
`TransactionService` returns a `TransactionResult` discriminated union instead of throwing exceptions for business failures. The controller maps results to HTTP responses:

```csharp
return result.ErrorType switch
{
    TransactionErrorType.PartnerNotVerified => Problem(statusCode: 403, ...),
    _ => Problem(statusCode: 500)
};
```

### Singleton RabbitMQ Connection
A single `IConnection` is registered as a singleton — channels are lightweight and created per-publish. This avoids the critical anti-pattern of creating a new TCP connection per HTTP request.

```csharp
services.AddSingleton<IConnection>(sp =>
    factory.CreateConnectionAsync().GetAwaiter().GetResult());

services.AddScoped<ITransactionMessagePublisher, TransactionMessagePublisher>();
```

### Polly Resilience Pipeline
Three layers of defense against an unreliable partner API:

```
Request
   │
   ├─[Timeout 10s]──────────────────► per-attempt deadline
   │
   ├─[Retry × 3, exponential backoff]► handles 5xx + TimeoutException
   │
   └─[Circuit Breaker]────────────────► opens after 50% failure rate
                                        (min 5 requests / 30s window)
                                        breaks for 15s
```

After all retries are exhausted, `PartnerVerificationClient` catches remaining exceptions and returns `false` — the request never crashes.

### .NET 8 `IExceptionHandler`
All unhandled exceptions are caught and formatted as [RFC 7807 ProblemDetails](https://www.rfc-editor.org/rfc/rfc7807) responses with `traceId` and `timestamp` extensions — no generic 500 HTML pages escape.

### Options Pattern (strongly-typed config)
No `configuration["magic:strings"]` anywhere. All config is bound to validated options classes at startup:

```csharp
services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .Validate(o => !string.IsNullOrWhiteSpace(o.Uri), "RabbitMQ:Uri is required.")
    .ValidateOnStart(); // fail fast at startup, not at runtime
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Option 1 — Docker Compose (Recommended)

```bash
docker-compose up --build
```

| Service | URL |
|---------|-----|
| API | http://localhost:8090 |
| Swagger UI | http://localhost:8090/swagger |
| RabbitMQ Management | http://localhost:15672 (guest / guest) |

### Option 2 — Local Run

```bash
# 1. Start RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# 2. Run the API
cd PartnerIntegration.BFF.Api
dotnet run
```

Swagger UI → https://localhost:7199/swagger

**Authenticate in Swagger:**
1. Click **Authorize 🔒**
2. Enter `partner-api-key-2026`
3. Click **Authorize** → all subsequent requests include the API key automatically

---

## 📡 API Reference

### `POST /api/v1/partner/transactions`
> Requires `X-Api-Key` header

**Request:**
```json
{
  "partnerId": "P-1001",
  "transactionReference": "TXN-99823",
  "amount": 250.00,
  "currency": "USD",
  "timestamp": "2024-05-10T14:30:00Z"
}
```

**Validation rules:**
| Field | Rule |
|-------|------|
| `partnerId` | Required, non-empty |
| `transactionReference` | Required, non-empty |
| `amount` | Required, must be `> 0` |
| `currency` | Required, must be one of: `USD`, `EUR`, `VND`, `JPY` |
| `timestamp` | Required |

**Responses:**

| Status | When |
|--------|------|
| `202 Accepted` | Partner verified, transaction queued |
| `400 Bad Request` | Validation failure (RFC 7807 `ValidationProblemDetails`) |
| `401 Unauthorized` | Missing or invalid API key |
| `403 Forbidden` | Partner verification failed after retries |
| `504 Gateway Timeout` | Partner API timed out (propagated from mock) |
| `500 Internal Server Error` | Unexpected error |

### `GET /health`
No authentication required. Returns RabbitMQ connectivity status.

### `GET /internal/mock-partner/{id}`
Simulates an external partner verification service.
- **70%** → `200 OK` `{ "partnerId": "...", "status": "Active" }`
- **30%** → `504 Gateway Timeout` (simulated `TimeoutException`)

---

## 🧪 Test Coverage

```bash
dotnet test --verbosity normal
# Test Run Successful. Total: 21 | Passed: 21 | Duration: ~264ms
```

| Test Suite | Scenarios Covered |
|------------|------------------|
| **Validators** (9 tests) | Valid request, empty `PartnerId`, empty `TransactionReference`, empty `Currency`, invalid currency, `amount = 0`, `amount < 0`, all 4 valid currencies (`Theory`), multiple simultaneous errors |
| **TransactionService** (3 tests) | Valid partner → accept + publish; Invalid partner → 403, publish never called; Publisher throws → exception propagates |
| **PartnerTransactionsController** (3 tests) | Success → 202 + correct body; Partner not verified → 403; `AcceptedAt` timestamp within request window |
| **PartnerVerificationClient** (3 tests) | Fails twice then succeeds (retry recovery); All 4 attempts fail → returns `false` gracefully; First attempt succeeds → exactly 1 HTTP call |

---

## 📁 Project Structure

```
PartnerIntegration/
│
├── PartnerIntegration.BFF.Api/              # 🌐 API Layer
│   ├── Authentication/
│   │   └── ApiKeyAuthenticationHandler.cs   # X-Api-Key scheme
│   ├── Controllers/
│   │   ├── PartnerTransactionsController.cs # POST /api/v1/partner/transactions
│   │   └── MockPartnerController.cs         # GET /internal/mock-partner/{id}
│   ├── Filters/
│   │   └── ValidationActionFilter.cs        # Global FluentValidation filter
│   ├── Middlewares/
│   │   └── GlobalExceptionHandler.cs        # IExceptionHandler → ProblemDetails
│   ├── appsettings.json
│   ├── Dockerfile
│   └── Program.cs                           # Composition root
│
├── PartnerIntegration.BFF.Core/             # 💡 Core / Domain Layer
│   ├── Exceptions/
│   │   └── AppException.cs
│   ├── Extensions/
│   │   └── CoreServiceCollectionExtensions.cs
│   ├── Interfaces/
│   │   ├── IPartnerVerificationClient.cs
│   │   └── ITransactionMessagePublisher.cs
│   ├── Models/
│   │   ├── PartnerTransactionRequest.cs
│   │   ├── TransactionAcceptedResponse.cs
│   │   └── TransactionResult.cs             # Result pattern
│   ├── Services/
│   │   ├── ITransactionService.cs
│   │   └── TransactionService.cs            # Orchestrate verify → publish
│   └── Validators/
│       └── PartnerTransactionRequestValidator.cs
│
├── PartnerIntegration.BFF.Infrastructure/   # 🔧 Infrastructure Layer
│   ├── Extensions/
│   │   └── InfrastructureServiceCollectionExtensions.cs
│   ├── HealthChecks/
│   │   └── RabbitMqHealthCheck.cs
│   ├── HttpClients/
│   │   └── PartnerVerificationClient.cs     # Polly retry + circuit breaker
│   ├── Options/
│   │   ├── PartnerApiOptions.cs
│   │   └── RabbitMqOptions.cs
│   └── Publishers/
│       └── TransactionMessagePublisher.cs   # Singleton connection, scoped channel
│
├── PartnerIntegration.BFF.Tests/            # 🧪 Test Project
│   ├── Controllers/
│   │   └── PartnerTransactionsControllerTests.cs
│   ├── Services/
│   │   └── TransactionServiceTests.cs
│   ├── Validators/
│   │   └── PartnerTransactionRequestValidatorTests.cs
│   └── VerificationClients/
│       └── PartnerVerificationClientTests.cs
│
├── docker-compose.yml
├── docker-compose.override.yml
└── README.md
```

---

## 📄 License

Built as a technical assessment. All code is original.
