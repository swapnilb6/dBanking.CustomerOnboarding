# dBanking.CustomerOnbaording

Solution: dBanking.CustomerManagement  (4 projects)
│
├─ compose/
│   └─ folderstructure.txt
│
├─ Customer Onboarding/
│   ├─ dBanking.CmsOnboarding.Core/
│   │   ├─ Dependencies/
│   │   ├─ DTOs/
│   │   ├─ Entities/
│   │   ├─ Mappers/
│   │   ├─ Messages/
│   │   ├─ RepositoryContracts/
│   │   ├─ ServiceContracts/
│   │   ├─ Services/
│   │   └─ DependencyInjection.cs
│   │
│   ├─ dBanking.CmsOnboarding.Infrastructure/
│   │   ├─ Dependencies/
│   │   ├─ Imports/
│   │   ├─ AppDbContext/
│   │   ├─ bin/
│   │   ├─ Configurations/
│   │   ├─ docker/
│   │   ├─ Migrations/
│   │   ├─ obj/
│   │   ├─ Repositories/
│   │   └─ DependencyInjection.cs
│   │
│   ├─ dBanking.CmsOnboarding.Tests/
│   │   ├─ Dependencies/
│   │   ├─ Consumers/
│   │   ├─ Controllers/
│   │   ├─ Services/
│   │   └─ TestUtils/
│   │
│   └─ dBanking.CustomerOnboarding.API/
│       ├─ Connected Services/
│       ├─ Dependencies/
│       ├─ Properties/
│       ├─ Consumers/
│       ├─ Controllers/
│       ├─ Middlewares/
│       ├─ appsettings.json
│       ├─ DependencyInjection.cs
│       ├─ Dockerfile
│       └─ Program.cs
│
├─ db-init/
│   ├─ 01-create-role.sql
│   └─ 02-create-db.sql
│
└─ Solution Items/
    ├─ .env.op1
    ├─ docker-compose.vm.yml
    └─ docker-compose.yml



Project Modules
dBanking.CmsOnboarding.Core
Purpose: Domain and application core (business rules, contracts, and use cases).
Key folders:

Entities/ – Domain models/aggregates representing core business concepts.
DTOs/ – Transfer models used across boundaries (API ↔ Services ↔ Infrastructure).
RepositoryContracts/ – Abstractions for persistence (ports).
ServiceContracts/ – Application service interfaces/use‑cases.
Services/ – Domain/application services implementing business workflows.
Mappers/ – Mapping helpers between Entities ⇄ DTOs (manual or mapper adapters).
Messages/ – Request/response envelopes, notifications, or integration messages.
DependencyInjection.cs – Core service registrations (usually extension methods).

dBanking.CmsOnboarding.Infrastructure
Purpose: Technical implementations and adapters for external concerns.
Key folders:

AppDbContext/ – EF Core DbContext, entity configurations, and context factories.
Repositories/ – Concrete implementations of RepositoryContracts (EF/SQL).
Configurations/ – EF Core entity type configurations or typed options.
Migrations/ – EF Core migrations for schema evolution.
Imports/ – Seeders/import routines or ETL helpers.
docker/ – Infra‑specific docker assets (if any).
DependencyInjection.cs – Infra service registrations (Db, repos, options, etc.).

dBanking.CustomerOnboarding.API
Purpose: HTTP endpoint layer hosting controllers and middleware.
Key folders/files:

Controllers/ – REST endpoints orchestrating application services.
Middlewares/ – Cross‑cutting concerns (exception handling, correlation, etc.).
Consumers/ – Message bus/queue consumers (if applicable).
DependencyInjection.cs – API‑level registrations (pipeline, auth, swagger, etc.).
appsettings.json – Application configuration.
Dockerfile / Program.cs – Container image and minimal‑hosting entry point.

dBanking.CmsOnboarding.Tests
Purpose: Automated tests (unit/component/integration).
Key folders:

Controllers/, Services/, Consumers/ – Test suites by vertical slice.
TestUtils/ – Builders, fixtures, stubs/mocks, and common assertions.
Dependencies/ – Test‑only DI setup, fakes, or in‑memory providers.


Support Assets

db-init/ – SQL bootstrap scripts:

01-create-role.sql – Database login/role provisioning.
02-create-db.sql – Schema/database creation or seed data.


Compose & Environment – Root‑level orchestration for local runs:

docker-compose.yml / docker-compose.vm.yml – Multi‑container setup (API, DB, etc.).
.env.op1 – Environment variables consumed by compose/services.


compose/folderstructure.txt – Snapshot of folder structure (for ops/docs).


How the layers fit together (at a glance)

Core defines contracts (ServiceContracts, RepositoryContracts) and domain logic.
Infrastructure implements those contracts (EF Core repositories, database).
API wires services, exposes HTTP endpoints, and hosts middleware.
Tests validate use‑cases, endpoints, and infra behaviors.


Tips & Conventions (optional but recommended)

Naming consistency: Prefer DependencyInjection.cs, DTOs, RepositoryContracts (no spaces), and fix the solution name typo CustomerManagment → CustomerManagement.
Assembly references:

API → Core, Infrastructure
Infrastructure → Core
Tests → API/Core/Infrastructure (as needed)


Configuration: Keep connection strings and secrets in environment variables; reference via appsettings.*.json with IOptions<T> and per‑environment overrides.