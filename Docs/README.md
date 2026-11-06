# GigHaus

GigHaus is a free platform that makes **house concerts**, live music in private homes, dead simple for the people who host them. GigHaus is for hosts alone. They get a page for each show, a guest list they control and check-in at the door.

GigHaus doesn't charge anyone, and takes no cut of what guests give. Guests contribute at the door, and that money supports the artist.

## Documentation

| Document | What's in it |
|---|---|
| [Concepts.md](Concepts.md) | The people involved, and how hosts, shows, reservations, leads, money and privacy work |
| [Features.md](Features.md) | Every page and feature by audience, what isn't built, and the launch checklist |
| [GigHaus_Legal_and_Pricing_Notes.md](GigHaus_Legal_and_Pricing_Notes.md) | Legal and pricing guidance that shapes the wording on the site |

## The people in GigHaus

| Name in the product | Who they are | Can sign in? |
|---|---|---|
| **Host** | Opens their home as a venue. The people you sell to. | Yes, to the host portal |
| **Artist** | Performs at a show. Added to a show by a host. | No |
| **Guest** (Attendee in the code) | Reserves a spot and comes to the show. | No |
| **Admin** | You, the one person who runs GigHaus. There are no staff. | Yes, to the admin area |

## Technology

| Layer | Technology |
|---|---|
| Language and runtime | C# 14 on .NET 10 |
| Orchestration | .NET Aspire 13.6 |
| Website | Blazor Web App: static pages for the public site, interactive server pages for the portal and admin |
| API | ASP.NET Core Minimal APIs |
| Database | PostgreSQL through Dapper and Npgsql |
| Migrations | DbUp with plain SQL scripts |
| Sign-in | ASP.NET Core Identity over a Dapper user store |
| Caching and messaging | Redis and RabbitMQ, used by the API |
| Email | MailDev in development, any SMTP service in production |
| Tests | xUnit, Shouldly, NSubstitute and Testcontainers |
| Mobile | .NET MAUI (an early skeleton, see [Features.md](Features.md#not-built-yet)) |

## Solution layout

```text
GigHaus.slnx
├── Aspire/
│   ├── GigHaus.AppHost/            Starts everything locally
│   └── GigHaus.ServiceDefaults/    Shared telemetry, health checks and resilience
├── Core/
│   ├── GigHaus.Domain/             Entities and enums, with no dependencies
│   ├── GigHaus.Infrastructure/     Repositories, sign-in store, services, email, messaging
│   ├── GigHaus.Mobile/             .NET MAUI app
│   ├── GigHaus.Web/                Marketing site, host portal and admin
│   └── GigHaus.WebApi/             REST API
├── Tools/
│   └── GigHaus.Migrations/         DbUp migration runner and SQL scripts
├── Tests/
│   ├── GigHaus.IntegrationTests/   Runs against a real Postgres in Docker
│   └── GigHaus.Tests/              Fast unit tests
├── Docs/                           This documentation
└── Mascot/                         Source mascot artwork and animations
```

`GigHaus.Web` talks to the database through `GigHaus.Infrastructure`. It doesn't call the API.

Inside `GigHaus.Web/Components/Pages`, the folder decides who can reach a page:

| Folder | Who can reach it | Rendering |
|---|---|---|
| `Public/` | Everyone | Static |
| `Account/` | Everyone, except `ChangePassword` | Static |
| `Portal/` | Hosts only, enforced in the folder's `_Imports.razor` | Static or interactive |
| `Admin/` | The admin only, enforced in the folder's `_Imports.razor` | Static or interactive |

## Running it locally

You need the .NET 10 SDK and Docker Desktop.

The admin account is created at startup from an Aspire parameter. Set its password once per machine:

```bash
dotnet user-secrets set Parameters:admin-password "choose-a-long-password" --project Aspire/GigHaus.AppHost
```

Then start everything:

```bash
dotnet run --project Aspire/GigHaus.AppHost
```

The Aspire dashboard opens with links to every service. Open the `web-admin` link for the website, and sign in as `admin@gighaus.local` with the password you set. MailDev catches all email in development.

The database has no volume, so it starts empty each time.

## Running the tests

```bash
dotnet test Tests/GigHaus.Tests
```

The integration tests need Docker running:

```bash
dotnet test Tests/GigHaus.IntegrationTests
```

CI runs both on every push and pull request to `main`, along with a build that treats warnings as errors. `Directory.Build.props` applies the same rule locally, so a newly published NuGet vulnerability fails the build until the package is updated.

## Design decisions

1. **Free, with no platform fee.** The platform fee in `StripePaymentService` is zero.
2. **"Suggested donation", never "ticket".** All money wording follows the [legal notes](GigHaus_Legal_and_Pricing_Notes.md).
3. **The street address is private.** It's never public. See [Concepts.md](Concepts.md#privacy).
4. **Dapper, not EF Core.** Identity runs on a small custom store, so there's no EF Core dependency.
5. **Migrations are plain SQL.** `Tools/GigHaus.Migrations/Scripts` is the source of truth for the schema.
6. **Business rules live in services.** `HostPortalService` checks ownership, so a page can't skip it by accident.
