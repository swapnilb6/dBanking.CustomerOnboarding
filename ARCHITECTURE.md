# dBanking.CustomerOnbaording — Architecture Document

Last updated: 2026-01-16

## Purpose
This document describes the architecture of the dBanking.CustomerOnbaording repository — the components, runtime interactions, deployment considerations, integration points, and improvement suggestions. It is intended to help maintainers and new contributors understand the structure and behavior of the system.

## High-level Overview
dBanking.CustomerOnbaording is an ASP.NET Core-based service responsible for customer onboarding and KYC (Know Your Customer) case handling. It follows a layered architecture:

- Presentation / API layer: dBanking.CustomerOnbaording.API — Web API endpoints, authentication, Swagger, middleware.
- Core / Domain layer: dBanking.Core — Domain entities, DTOs, service contracts, application services (KycCaseService, CustomerService, AuditService).
- Infrastructure layer: dBanking.Infrastructure — EF Core DbContext(s), repository implementations (CustomerRepository, KycCaseRepository, AuditRepository), external integrations.
- Messaging: MassTransit abstraction used to publish/consume messages (RabbitMQ or other brokers).
- Persistence: PostgreSQL (AppPostgresDbContext).
- External Identity: Azure AD / Microsoft Identity Web (JWT Bearer).
- Observability: Console logging is configured; auditing is supported via AuditService.

Key libraries and frameworks:
- .NET 8 (ASP.NET Core)
- MassTransit (message bus abstraction)
- AutoMapper
- EF Core (Npgsql / PostgreSQL)
- FluentValidation
- Microsoft.Identity.Web (Azure AD auth for JWT)
- Swagger/OpenAPI
- Docker (Dockerfile included)

## Repository & Folder Mapping
- dBanking.CustomerOnbaording.API
  - Program.cs — DI registration, middleware, MassTransit config, EF Core registration.
  - Controllers/ — API endpoints (e.g., DiagController).
  - Middlewares/ — custom middleware such as correlation id or auditing middleware.
  - Consumers/ — MassTransit consumers (if present).
  - Dockerfile — build and publish steps (multi-stage).
- dBanking.Core
  - Entities/ — domain models (Customer, KycCase, AuditRecord).
  - DTOS/ — data transfer objects (AuditEntryDto, etc).
  - ServiceContracts/ — interfaces (IAuditService, IKycCaseService, ICustomerService).
  - Services/ — implementation classes (KycCaseService, CustomerService, AuditService).
  - Repository Contracts/ — interfaces for repositories (ICustomerRepository, IKycCaseRepository, IAuditRepository).
  - MappingProfiles/ — AutoMapper profiles (CustomerMappingProfile, KycMappingProfile).
- dBanking.Infrastructure
  - DbContext/ — AppPostgresDbContext for PostgreSQL.
  - Repositories/ — concrete repository implementations (CustomerRepository, KycCaseRepository, AuditRepository).
  - dependancyInjection.cs — extension to register infrastructure services.
- dBanking.Tests
  - Unit / integration tests (e.g., KycCaseServiceTests using MassTransit test harness).

## Components & Responsibilities

- API (dBanking.CustomerOnbaording.API)
  - Handles incoming HTTP requests.
  - Registers services and middleware: authentication (Azure AD), authorization policies, Swagger, AutoMapper, FluentValidation.
  - Configures MassTransit and message endpoints.
  - Exposes endpoints for customer onboarding operations and diagnostics.

- Core Services (dBanking.Core.Services)
  - KycCaseService: Orchestrates KYC flow, interacts with repositories, publishes events (via MassTransit), records audit entries.
  - CustomerService: Business logic around customer creation/updating.
  - AuditService: Records audit logs through IAuditRepository.

- Repositories (dBanking.Infrastructure.Repositories)
  - Abstract persistence behind repository interfaces.
  - Use EF Core DbContext (AppPostgresDbContext) connecting to PostgreSQL.
  - Responsible for CRUD operations and queries (e.g., GetByEmailOrPhoneAsync).

- Messaging Layer (MassTransit)
  - IPublishEndpoint used to publish domain events (e.g., KycStatusChanged).
  - Consumers (if implemented) receive messages and react (e.g., update statuses, notify other systems).

- Database
  - PostgreSQL stores Customers, KycCases, AuditRecords.
  - AppPostgresDbContext configured in Program.cs.

- Identity & Security
  - Azure AD (Microsoft Identity Web) used for JWT-based authentication.
  - Authorization policies defined using scopes like "App.Read" and "App.Write".

## Typical Data Flows / Scenarios

1) Customer Registration (synchronous)
- Client -> POST /customers -> Controller validates input -> CustomerService checks repository -> ICustomerRepository persists new Customer -> Service returns created entity -> Controller returns HTTP 201.

2) KYC Case Creation and Event Publishing (hybrid)
- Client -> POST /kyc/cases -> KycCaseService validates and persists KycCase via IKycCaseRepository -> Service maps KycCase to a KycCreated or KycStatusChanged message -> IPublishEndpoint.Publish(message) -> downstream services consume the event.

3) Audit Recording
- Important actions call IAuditService.RecordAsync(AuditEntryDto) -> AuditService serializes an AuditRecord -> IAuditRepository.AddAsync persists record to DB.

4) Asynchronous Consumer Processing
- External system or internal consumer reads messages (MassTransit consumer) -> processes message -> updates DB via repositories or triggers additional events.

## Sequence Diagram (high-level)
(See the PlantUML file included as `architecture.puml` for a visual sequence diagram.)

Steps:
1. HTTP request arrives to API.
2. API controller delegates to appropriate Core service.
3. Core service performs domain logic, persists via repository.
4. Service publishes events via MassTransit (IPublishEndpoint).
5. Consumers (possibly in same service or separate microservices) receive event, perform work, and may publish further events.
6. AuditService logs important events to database.

## Deployment & Runtime
- The API is containerized (Dockerfile included). The typical deployment stack:
  - dBanking.CustomerOnbaording.API running in a container (Kubernetes deployment or Docker Compose).
  - PostgreSQL (managed or container).
  - RabbitMQ (or other broker) for MassTransit.
  - Azure AD for authentication (no container needed).
- Recommended runtime resources:
  - Health checks and readiness probes for the API.
  - MassTransit host set to WaitUntilStarted (configured in Program.cs).
  - Persistent storage for Postgres.

Suggested Docker Compose (example snippet):
```yaml
version: "3.8"
services:
  api:
    build: ./dBanking.CustomerOnbaording.API
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__PostgresAzureDb=Host=postgres;Database=db;Username=postgres;Password=postgres
      - RabbitMQ__Host=rabbitmq
  postgres:
    image: postgres:15
    environment:
      - POSTGRES_PASSWORD=postgres
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "15672:15672"
```

## Observability & Testing
- Logging: Console logger is added with LogLevel.Debug in Program.cs.
- Tests: Unit tests and MassTransit test harness exist in dBanking.Tests using dependency injection and mocked repositories.
- Recommend adding:
  - Structured logging (Serilog) with a sink (e.g., Seq, ELK).
  - Distributed tracing (OpenTelemetry) to capture correlation between HTTP requests and MassTransit messages.
  - Metrics export (Prometheus) if required.

## Security Considerations
- Use secure storage for connection strings and secrets (Azure Key Vault, environment variables).
- Review JWT scope-based policies and ensure endpoints are protected appropriately.
- Sanitize any auditable payloads to avoid logging sensitive PII.

## Recommendations & Improvements (short-term)
- Add CI pipeline that builds, runs unit tests, and lints code.
- Add Docker Compose sample in repo for local development (Postgres + RabbitMQ + API).
- Add integration tests covering MassTransit flows (consumers + producers).
- Introduce structured logs and correlation propagation across MassTransit messages.
- Harden database migrations (use EF Migrations in CI and apply in deployment).
- Add health endpoints and readiness/liveness probes.
