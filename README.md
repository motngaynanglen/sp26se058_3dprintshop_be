<div align="center">

# 3D Print Shop — Backend API

**An intelligent 3D printing e-commerce platform with AI-powered 2D-to-3D model generation**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MySQL](https://img.shields.io/badge/MySQL-8.x-4479A1?logo=mysql&logoColor=white)](https://www.mysql.com/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)](https://github.com/jasontaylordev/CleanArchitecture)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

</div>

---

## Overview

3D Print Shop is the backend service for a full-cycle 3D printing platform. Customers submit 2D images or design ideas; the system uses an external AI service to generate printable 3D models (glTF), then manages the entire workflow from design collaboration through production, payment, and shipment.

This project is a **capstone submission (SP26SE058)** built on the [Clean Architecture Solution Template](https://github.com/jasontaylordev/CleanArchitecture) v8.0.6.

---

## Key Features

| Area | Capability |
|---|---|
| **AI Generation** | Converts a Base64 image to a glTF 3D model via external AI service |
| **Design Workflow** | Versioned design works with branching (ORIGINAL / REVISION / BRANCH / CLONE), staff assignment, and real-time chat |
| **Order Management** | Full lifecycle: PENDING → confirmed → in-production → delivered, with auto-expiry background job |
| **Pricing Engine** | Weight-based print cost calculation with markup and time factors |
| **Payments** | PayOS integration with deposit tracking and manual payment by staff |
| **File Storage** | Backblaze B2 (primary) and AWS S3 compatible storage |
| **Notifications** | Email via MailKit / SMTP |
| **Real-time** | SignalR hub for design-work chat between customers and staff |
| **Role-based Access** | Five roles: ADMIN, MANAGER, STAFF, CUSTOMER, GUEST |
| **Observability** | Health check endpoint at `/health`, structured logging |

---

## Architecture

The solution follows **Clean Architecture**, separated into four projects:

```
src/
├── Domain/             # Entities, enums, value objects, domain events — no dependencies
├── Application/        # Use cases (CQRS with MediatR), interfaces, validators
├── Infrastructure/     # EF Core, external services (AI, PayOS, B2, email, JWT)
└── Web/                # ASP.NET Core API — endpoints, SignalR hub, middleware

tests/
├── Domain.UnitTests/
├── Application.UnitTests/
├── Infrastructure.IntegrationTests/
└── Application.FunctionalTests/
```

### Request flow

```
HTTP Request
    └─▶ Web (Minimal API Endpoint)
            └─▶ MediatR (Command / Query)
                    └─▶ Application (Handler + Validator)
                            └─▶ Domain (Business logic)
                            └─▶ Infrastructure (DB, external APIs)
                    └─▶ HTTP Response
```

---

## Tech Stack

| Category | Technology |
|---|---|
| Runtime | .NET 8 / C# 12 |
| Web framework | ASP.NET Core 8 (Minimal APIs) |
| Database | MySQL 8 via Pomelo EF Core 8 |
| ORM | Entity Framework Core 8 |
| CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| Mapping | AutoMapper 15 |
| Authentication | JWT Bearer |
| Password hashing | BCrypt.Net-Next |
| API docs | NSwag 14 (OpenAPI / Swagger UI at `/api`) |
| Real-time | ASP.NET Core SignalR |
| Payment | PayOS SDK 2 |
| File storage | Backblaze B2 (S3-compatible via AWSSDK.S3) |
| Email | MailKit 4 |
| Testing | NUnit 3, FluentAssertions, Moq, Respawn, Testcontainers |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- MySQL 8.x (local or remote)
- Visual Studio 2022 (v17.8+) or any IDE with .NET 8 support

### 1. Clone the repository

```bash
git clone https://github.com/motngaynanglen/sp26se058_3dprintshop_be.git
cd sp26se058_3dprintshop_be
```

### 2. Configure settings

Create `src/Web/appsettings.json` based on the template below (this file is gitignored — never commit real secrets):

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<host>;Port=3306;Database=<db>;User=<user>;Password=<password>;"
  },
  "JwtSettings": {
    "Secret": "<min-32-char-random-secret>",
    "Issuer": "3DPrintShop_Backend",
    "Audience": "3DPrintShop_Clients"
  },
  "AI": {
    "GenerateUrl": "<ai-service-endpoint>"
  },
  "PayOS": {
    "ClientId": "<payos-client-id>",
    "ApiKey": "<payos-api-key>",
    "ChecksumKey": "<payos-checksum-key>",
    "ReturnUrl": "<return-url>",
    "CancelUrl": "<cancel-url>"
  },
  "BackblazeB2": {
    "KeyId": "<b2-key-id>",
    "ApplicationKey": "<b2-application-key>",
    "BucketName": "<bucket-name>",
    "BucketId": "<bucket-id>"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "FromEmail": "<your-email>",
    "AppPassword": "<gmail-app-password>",
    "DisplayName": "3D Print Shop Support"
  },
  "DevAccount": {
    "Username": "admin_dev",
    "Password": "<seed-admin-password>",
    "Fullname": "Dev Administrator"
  }
}
```

### 3. Apply database migrations

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

### 4. Run the API

```bash
dotnet watch run --project src/Web
```

Swagger UI is available at `http://localhost:<port>/api`.

---

## API Reference

The full OpenAPI specification is served at `/api/specification.json` when the server is running.

### Endpoint groups

| Group | Description |
|---|---|
| Auth | Register, login, refresh token |
| Account | Profile management |
| Design Work | Create and manage design jobs |
| Design Versions | Version history per design |
| Design Templates | Reusable design templates |
| Model Generate | Trigger AI 2D → 3D generation |
| Orders | Order lifecycle management |
| Materials | Print material catalog and pricing |
| Service Options | Configurable print service add-ons |
| Transactions | Payment records |
| Shipments | Shipping and delivery tracking |
| Inventory | Stock movement records |
| Dashboard | Aggregated metrics for managers |
| Feedback | Customer reviews |
| Tags | Concept tags and design tags |

### Real-time hub

| Hub | Route | Description |
|---|---|---|
| Design Work Chat | `/hubs/design-work-chat` | Real-time messaging between customer and staff on a design job |

---

## Testing

```bash
# Run all tests
dotnet test

# Run a specific project
dotnet test tests/Application.UnitTests

# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

The test suite contains **400+ unit tests** across four projects using NUnit, FluentAssertions, and Moq. Integration tests use Testcontainers and Respawn for database isolation.

---

## Project Structure (detailed)

```
src/Domain/
├── Entities/       # Order, DesignWork, Material, Shipment, Invoice, ...
├── Enums/          # PriorityLevel, ...
├── Events/         # Domain events
├── Constants/      # Roles (ADMIN, MANAGER, STAFF, CUSTOMER, GUEST)
└── ValueObjects/

src/Application/
├── Orders/         # Commands & queries for order lifecycle
├── DesignWorks/    # Design job CQRS handlers
├── ModelGeneratorAI/
├── Transactions/
├── Materials/
└── Common/         # Interfaces, pipeline behaviours, exceptions

src/Infrastructure/
├── Data/           # ApplicationDbContext, migrations, seeding
├── Identity/       # JWT token generation
└── Service/        # AIService, PayOsService, BackblazeB2Service,
                    # EmailService, PricingEngine,
                    # ExpiredPendingOrderHostedService, ...

src/Web/
├── Endpoints/      # 21 minimal-API endpoint files
├── Hubs/           # DesignWorkChatHub (SignalR)
└── Services/       # Web-layer services
```

---

## Contributing

1. Fork the repository and create a feature branch from `dev`.
2. Follow the existing CQRS pattern — add Commands/Queries under the relevant `Application` folder.
3. Write unit tests for new handlers.
4. Open a pull request targeting `dev`; `main` receives only stable merges.

Please do **not** commit `appsettings.json` or `appsettings.Development.json` — they are gitignored by design.

---

## License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">
  <sub>Capstone Project SP26SE058 — FPT University, Spring 2026</sub>
</div>
