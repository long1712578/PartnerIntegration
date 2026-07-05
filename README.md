# PartnerIntegration BFF

A **.NET 8 Backend-for-Frontend (BFF) microservice** that receives incoming partner transaction data, validates it, verifies the partner via an external API, and reliably queues the transaction for downstream systems to process.

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      API Layer (BFF)                         │
│  ┌──────────────────┐  ┌──────────────────────────────────┐  │
│  │ Partner           │  │ Mock Partner Controller          │  │
│  │ Transactions      │  │ (simulated external service)     │  │
│  │ Controller        │  └──────────────────────────────────┘  │
│  └────────┬─────────┘                                        │
│           │                                                  │
│  ┌────────▼─────────┐                                        │
│  │ Validation Filter │  ← FluentValidation                  │
│  └────────┬─────────┘                                        │
│           │                                                  │
├───────────┼──────────────────────────────────────────────────┤
│           │          Core Layer                              │
│  ┌────────▼─────────┐                                        │
│  │ TransactionService│  ← Orchestrates verify → publish     │
│  └──┬────────────┬──┘                                        │
│     │            │                                           │
├─────┼────────────┼───────────────────────────────────────────┤
│     │            │   Infrastructure Layer                    │
│  ┌──▼──────────┐ │                                           │
│  │ Partner     │ ┌▼─────────────────┐                        │
│  │ Verification│ │ Transaction      │                        │
│  │ Client      │ │ Message Publisher│                        │
│  │ (HttpClient)│ │ (RabbitMQ)       │                        │
│  └──┬──────────┘ └──┬──────────────┘                         │
│     │  Polly:        │                                       │
│     │  • Retry 3x    │                                       │
│     │  • Circuit     │                                       │
│     │    Breaker     │                                       │
│     │  • Timeout     │                                       │
└─────┼────────────────┼───────────────────────────────────────┘
      │                │
      ▼                ▼
  Mock Partner    RabbitMQ Queue
  Verification    (partner-transactions)
  API
```

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 8 |
| Validation | FluentValidation 11 |
| Resilience | Microsoft.Extensions.Http.Resilience (Polly) |
| Messaging | RabbitMQ (via RabbitMQ.Client 7.x) |
| Testing | xUnit + FluentAssertions + Moq |
| Containerization | Docker + Docker Compose |
| API Documentation | Swagger / OpenAPI |

## Design Decisions

### Clean Architecture (3-Layer)
- **Api** — Controllers, filters, authentication, exception handling. Thin layer that delegates to Core services.
- **Core** — Business logic, interfaces, models, validators. Zero infrastructure dependencies.
- **Infrastructure** — External service integrations (HTTP clients, RabbitMQ publisher). Implements Core interfaces.

### Controller-Based API
Chose controllers over Minimal APIs to demonstrate proper separation of concerns, testability, and standard MVC patterns expected in enterprise .NET applications.

### Service Layer (TransactionService)
Business logic is encapsulated in `TransactionService` rather than sitting in the controller. The service returns a `TransactionResult` object — avoiding exception-driven control flow for business outcomes.

### Resilience Strategy (Polly)
The Partner Verification API is unreliable by design (30% failure rate). The resilience pipeline includes:
- **Retry** (3 attempts, exponential backoff) — handles transient failures
- **Circuit Breaker** — prevents cascading failures when the API is consistently down
- **Timeout** (10s per attempt) — prevents requests from hanging indefinitely

The `PartnerVerificationClient` also catches exceptions after retry exhaustion, returning `false` instead of crashing the request.

### RabbitMQ Connection Management
A singleton `IConnection` is registered in DI. Individual channels are created per-publish operation (lightweight). This avoids the anti-pattern of creating a new TCP connection per request.

### FluentValidation
Used over DataAnnotations for richer, testable validation rules. All fields are required; amount must be > 0; currency is validated against a whitelist (USD, EUR, VND, JPY).

### API Key Authentication
A simple `X-Api-Key` header authentication demonstrates endpoint security. The mock partner endpoint is excluded (`[AllowAnonymous]`).

### Global Exception Handler
Uses .NET 8's `IExceptionHandler` interface (preferred over custom middleware) to format all unhandled exceptions as RFC 7807 ProblemDetails responses.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/products/docker-desktop/) & Docker Compose (for RabbitMQ)

## Getting Started

### Option 1: Docker Compose (Recommended)

Spins up both the API and RabbitMQ:

```bash
docker-compose up --build
```

- **API**: http://localhost:8090
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

### Option 2: Local Development

1. **Start RabbitMQ** (if not already running):
   ```bash
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. **Run the API**:
   ```bash
   cd PartnerIntegration.BFF.Api
   dotnet run
   ```

   The API will be available at http://localhost:5071

3. **Swagger UI**: http://localhost:5071/swagger

## API Usage

### POST /api/v1/partner/transactions

Accepts a partner transaction, validates it, verifies the partner, and queues it for processing.

**Headers:**
```
Content-Type: application/json
X-Api-Key: partner-api-key-2026
```

**Request Body:**
```json
{
  "partnerId": "P-1001",
  "transactionReference": "TXN-99823",
  "amount": 250.00,
  "currency": "USD",
  "timestamp": "2024-05-10T14:30:00Z"
}
```

**Responses:**

| Status | Description |
|--------|-------------|
| 202 Accepted | Transaction validated and queued |
| 400 Bad Request | Validation errors |
| 401 Unauthorized | Missing or invalid API key |
| 403 Forbidden | Partner verification failed |
| 500 Internal Server Error | Unexpected error |

### GET /health

Health check endpoint (no authentication required).

### GET /internal/mock-partner/{id}

Mock Partner Verification API (no authentication required). Simulates 30% timeout / 70% success.

## Running Tests

```bash
dotnet test --verbosity normal
```

Test coverage includes:
- **Validation tests** — all field rules, valid/invalid currencies, boundary cases
- **Service layer tests** — success, partner invalid, publish failure, short-circuit behavior
- **Controller tests** — HTTP status code mapping (202, 403)
- **Resilience tests** — retry recovery, all retries exhausted, immediate success

## Project Structure

```
PartnerIntegration/
├── PartnerIntegration.BFF.Api/          # API Layer
│   ├── Authentication/                  # API Key auth handler
│   ├── Controllers/                     # MVC controllers
│   ├── Filters/                         # FluentValidation action filter
│   ├── Middlewares/                      # IExceptionHandler
│   └── Program.cs                       # App bootstrap
├── PartnerIntegration.BFF.Core/         # Core/Domain Layer
│   ├── Exceptions/                      # Custom exceptions
│   ├── Interfaces/                      # Abstractions
│   ├── Models/                          # DTOs & result types
│   ├── Services/                        # Business logic
│   └── Validators/                      # FluentValidation rules
├── PartnerIntegration.BFF.Infrastructure/  # Infrastructure Layer
│   ├── Extensions/                      # DI registration
│   ├── HealthChecks/                    # RabbitMQ health check
│   ├── HttpClients/                     # Partner verification client
│   ├── Options/                         # Strongly-typed config
│   └── Publishers/                      # RabbitMQ publisher
├── PartnerIntegration.BFF.Tests/        # Test Project
│   ├── Controllers/                     # Controller unit tests
│   ├── Services/                        # Service unit tests
│   ├── Validators/                      # Validation unit tests
│   └── VerificationClients/             # HTTP client + resilience tests
├── docker-compose.yml                   # Docker Compose config
└── README.md
```
