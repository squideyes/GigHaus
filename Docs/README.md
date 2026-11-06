# GigHaus — House Concert Platform

## What is GigHaus?

GigHaus is a platform for organizing **house concerts** — intimate live music events hosted in private spaces. It connects **artists**, **hosts**, and **attendees** and handles event creation, reservations, and suggested-donation-based contributions to support performing artists.

All money handling follows the guidance in [`GigHaus_Legal_and_Pricing_Notes.md`](./GigHaus_Legal_and_Pricing_Notes.md): payments are framed as **suggested donations / contributions to support the artist**, with clear disclosures that they are **not charitable donations and not tax-deductible**.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Orchestration | .NET Aspire 13.x (AppHost) |
| API | ASP.NET Core 10 Minimal APIs |
| Database | PostgreSQL (via Aspire + Dapper) |
| Migrations | DbUp (embedded SQL scripts) |
| Caching | Redis (via Aspire) |
| Messaging | RabbitMQ (via Aspire) |
| Data Access | Dapper (micro-ORM) + Npgsql |
| Admin UI | Blazor Web App (Server interactivity) |
| Mobile | .NET MAUI (end-user + check-in) |
| Email (Dev) | MailDev (SMTP + web UI via Docker) |
| Email (Prod) | MailGun SMTP |
| Tests | xUnit + NSubstitute |
| Containerization | Docker (managed by Aspire) |
| Future: Payments | Stripe Connect |

---

## Solution Structure

```text
GigHaus.slnx
├── /Aspire/
│   ├── GigHaus.AppHost/          # Aspire orchestrator (Postgres, Redis, RabbitMQ, MailDev)
│   └── GigHaus.ServiceDefaults/  # Shared Aspire defaults (telemetry, health, resilience)
├── /Core/
│   ├── GigHaus.Domain/           # Domain entities and enums (no dependencies)
│   ├── GigHaus.Infrastructure/   # Dapper repositories, email service, DI
│   ├── GigHaus.Mobile/           # .NET MAUI app (end-user events + check-in)
│   ├── GigHaus.Web/              # Blazor Web App (Admin UI)
│   └── GigHaus.WebApi/           # REST API (Minimal APIs)
├── /Tools/
│   └── GigHaus.Migrations/       # DbUp SQL migration runner
├── /Tests/
│   └── GigHaus.Tests/            # xUnit + NSubstitute tests
├── /Docs/
│   ├── GigHaus_Legal_and_Pricing_Notes.md
│   └── README.md
└── /Mascot/                      # Mascot artwork and animations
```

---

## What Has Been Done

### Phase 1a — Foundation (Domain + API + Tests)

- **Domain Model** — Artist, Host, Event, Reservation, PaymentRecord entities with full pricing policy (PricingMode, SuggestedDonationAmount, legal disclosure defaults)
- **API Endpoints** — CRUD via ASP.NET Core Minimal APIs for Artists, Hosts, Events, Reservations
- **Aspire Orchestration** — PostgreSQL (pgAdmin), Redis, RabbitMQ (management UI), MailDev

### Phase 1b — Dapper + Ecosystem Expansion

- **Dapper migration** — Replaced EF Core with Dapper micro-ORM and repository pattern
- **DbUp migrations** — SQL-based schema management via `GigHaus.Migrations` console app
- **Blazor Admin** — `GigHaus.Web` with dashboard (entity counts) and Artists list page
- **MAUI Mobile** — `GigHaus.Mobile` with Events browser and Check-In pages (requires MAUI workload)
- **Email service** — `IEmailService` + `SmtpEmailService` (MailDev for dev, MailGun SMTP for prod)
- **MailDev container** — SMTP on port 1025, web UI on port 1080 for email testing
- **Tests** — xUnit + NSubstitute mock-based repository tests

---

## Roadmap (Next Phases)

### Phase 2 — Reservations & Payments

- [ ] Full reservation workflow (create → confirm → check-in → cancel)
- [ ] Stripe Connect integration (connected accounts for artists)
- [ ] Online payment flow with suggested-donation UI
- [ ] Payment record creation with fee breakdown
- [ ] RabbitMQ event-driven notifications
- [ ] Email notifications via background worker

### Phase 3 — Advanced Features

- [ ] SignalR real-time updates (capacity changes, live check-in counts)
- [ ] Host & Artist dashboards
- [ ] Redis output caching
- [ ] Input validation (FluentValidation)
- [ ] API error handling middleware

### Phase 4 — Production Readiness

- [ ] Authentication & authorization (ASP.NET Identity / OAuth)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Monitoring & alerting (OpenTelemetry)
- [ ] Legal review checkpoint for Stripe Connect

---

## How to Run

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for Aspire-managed containers: Postgres, Redis, RabbitMQ)

### Start the application
```bash
cd Aspire/GigHaus.AppHost
dotnet run
```
Aspire will start PostgreSQL, Redis, RabbitMQ containers and the WebApi automatically.

### Run tests
```bash
dotnet test Tests/GigHaus.Tests
```

---

## Key Design Decisions

1. **"Suggested donation" not "ticket fee"** — all pricing language follows the legal guidance document.
2. **Dapper over EF Core** — micro-ORM for high-performance data access with explicit SQL.
3. **DbUp migrations** — SQL-based schema management; migration scripts are the source of truth.
4. **Aspire-first** — all infrastructure orchestrated through .NET Aspire for consistent local dev.
5. **MailDev for email testing** — real SMTP server with web UI; same email code path as production.
6. **Clean architecture** — Domain has zero dependencies; Infrastructure depends on Domain; API/Web depend on both.
